using System;
using System.Collections.Generic;
using System.IO;

namespace GridPlateInspector
{
    public class MatchResult
    {
        public string FileName { get; set; }
        public double Score { get; set; }
        public double LWPenalty { get; set; }
        public double PosCost { get; set; }
        public double SizeCost { get; set; }
        public double ShapeCost { get; set; }
        public int MatchedCount { get; set; }
        public int UnmatchedCount { get; set; }
        public bool IsValid { get { return Score < 1e8; } }
    }

    public class Matcher
    {
        // --- 可调权重 ---
        public double W_POS   = 0.50;  // 归一化坐标权重
        public double W_SIZE  = 0.25;  // 椭圆轴尺寸权重
        public double W_ANIS  = 0.15;  // 各向异性权重
        public double W_CIRC  = 0.10;  // 圆度权重

        // --- 容差 ---
        public double E_L     = 2.0;    // L 绝对容差 (mm)
        public double E_L_PCT = 0.005;  // L 相对容差
        public double E_W     = 2.0;    // W 绝对容差 (mm)
        public double E_W_PCT = 0.005;  // W 相对容差
        public double E_POS   = 0.025;  // 位置容差（归一化单位）
        public double E_SIZE  = 0.15;   // 尺寸相对容差
        public double E_ANIS  = 0.20;   // 各向异性相对容差
        public double E_CIRC  = 0.15;   // 圆度绝对容差

        // --- 未配对罚分 ---
        public double P_UNMATCHED = 0.30;

        // --- 匹配阈值 ---
        public double T_MATCH = 0.50;

        /// <summary>
        /// 从文件夹加载所有 CAD JSON 编码
        /// </summary>
        public List<FeatureEncoding> LoadCADLibrary(string folderPath)
        {
            List<FeatureEncoding> lib = new List<FeatureEncoding>();
            foreach (string file in Directory.GetFiles(folderPath, "*.json"))
            {
                try
                {
                    string json = File.ReadAllText(file, System.Text.Encoding.UTF8);
                    FeatureEncoding enc = FeatureEncoding.FromJson(json);
                    if (enc != null && enc.features != null && enc.L > 0 && enc.W > 0)
                    {
                        enc.FilePath = file;
                        lib.Add(enc);
                    }
                }
                catch { }
            }
            return lib;
        }

        /// <summary>
        /// 四级匹配流水线，返回按分数升序排列的结果。
        /// 自动尝试4种方向变体以消除 SmallestRectangle2 的轴方向歧义。
        /// </summary>
        public List<MatchResult> Match(FeatureEncoding image, List<FeatureEncoding> cadLib)
        {
            var orientations = GenerateOrientations(image);
            List<MatchResult> results = new List<MatchResult>();

            foreach (var cad in cadLib)
            {
                MatchResult best = null;
                foreach (var imgVariant in orientations)
                {
                    MatchResult r = MatchOne(imgVariant, cad);
                    r.FileName = cad.FilePath;
                    if (best == null || r.Score < best.Score)
                        best = r;
                }
                results.Add(best);
            }

            results.Sort((a, b) => a.Score.CompareTo(b.Score));
            return results;
        }

        /// <summary>
        /// 生成图像编码的4种方向变体，应对 SmallestRectangle2 Phi 的 180° 歧义
        /// 和矩形轴正方向的歧义：(cx,cy), (cx,-cy), (-cx,cy), (-cx,-cy)
        /// </summary>
        private List<FeatureEncoding> GenerateOrientations(FeatureEncoding image)
        {
            var result = new List<FeatureEncoding>();
            int[][] signs = { new[] { 1, 1 }, new[] { 1, -1 }, new[] { -1, 1 }, new[] { -1, -1 } };

            foreach (var s in signs)
            {
                var variant = new FeatureEncoding();
                variant.L = image.L;
                variant.W = image.W;
                foreach (var f in image.features)
                {
                    variant.features.Add(new FeaturePoint
                    {
                        cx = f.cx * s[0],
                        cy = f.cy * s[1],
                        area = f.area,
                        circularity = f.circularity,
                        anisometry = f.anisometry,
                        bulkiness = f.bulkiness,
                        ra = f.ra,
                        rb = f.rb
                    });
                }
                result.Add(variant);
            }
            return result;
        }

        /// <summary>
        /// 单张 CAD 对图像编码的四级匹配
        /// </summary>
        public MatchResult MatchOne(FeatureEncoding image, FeatureEncoding cad)
        {
            MatchResult result = new MatchResult();

            // --- Stage 1: L, W 双向独立过滤 ---
            double dL = Math.Abs(image.L - cad.L);
            double dW = Math.Abs(image.W - cad.W);
            double tolL = Math.Max(E_L, cad.L * E_L_PCT);
            double tolW = Math.Max(E_W, cad.W * E_W_PCT);

            if (dL > tolL || dW > tolW)
            {
                result.Score = double.MaxValue;
                result.LWPenalty = Math.Max(dL / tolL, dW / tolW);
                return result;
            }
            result.LWPenalty = (dL / tolL + dW / tolW) * 0.05; // L,W 贡献很小

            // --- Stage 2: 特征数量 ---
            int nImg = image.features.Count;
            int nCad = cad.features.Count;
            if (Math.Abs(nImg - nCad) > 1)
            {
                result.Score = double.MaxValue;
                return result;
            }

            // --- Stage 3: 匈牙利最优指派 ---
            int n = Math.Max(nImg, nCad);
            double[,] costMatrix = new double[n, n];

            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    if (i < nImg && j < nCad)
                        costMatrix[i, j] = FeatureCost(image.features[i], cad.features[j]);
                    else
                        costMatrix[i, j] = P_UNMATCHED; // 虚拟配对（未匹配罚分）
                }
            }

            double hungarianCost;
            int[] assignment;
            Hungarian(costMatrix, out hungarianCost, out assignment);

            // 收集配对详情
            double totalPos = 0, totalSize = 0, totalShape = 0;
            int matched = 0, unmatched = 0;

            for (int i = 0; i < nImg; i++)
            {
                int j = assignment[i];
                if (j >= 0 && j < nCad)
                {
                    var imgFeat = image.features[i];
                    var cadFeat = cad.features[j];
                    totalPos += PosCost(imgFeat, cadFeat);
                    totalSize += SizeCost(imgFeat, cadFeat);
                    totalShape += ShapeCost(imgFeat, cadFeat);
                    matched++;
                }
                else
                {
                    unmatched++;
                }
            }
            unmatched += Math.Max(0, nCad - nImg);

            // --- Stage 4: 加权总评分 ---
            double maxN = Math.Max(nImg, nCad);
            if (maxN == 0) maxN = 1;
            result.PosCost = totalPos / maxN;
            result.SizeCost = totalSize / maxN;
            result.ShapeCost = totalShape / maxN;
            result.MatchedCount = matched;
            result.UnmatchedCount = unmatched;
            result.Score = result.LWPenalty
                         + result.PosCost
                         + result.SizeCost
                         + result.ShapeCost
                         + P_UNMATCHED * unmatched / maxN;

            return result;
        }

        /// <summary>
        /// 两个特征间的加权代价
        /// </summary>
        private double FeatureCost(FeaturePoint f1, FeaturePoint f2)
        {
            return PosCost(f1, f2) + SizeCost(f1, f2) + ShapeCost(f1, f2);
        }

        private double PosCost(FeaturePoint f1, FeaturePoint f2)
        {
            double d = Math.Sqrt((f1.cx - f2.cx) * (f1.cx - f2.cx)
                               + (f1.cy - f2.cy) * (f1.cy - f2.cy));
            return W_POS * Math.Min(d / E_POS, 1.0);
        }

        private double SizeCost(FeaturePoint f1, FeaturePoint f2)
        {
            double maxRa = Math.Max(Math.Abs(f1.ra), Math.Abs(f2.ra));
            double maxRb = Math.Max(Math.Abs(f1.rb), Math.Abs(f2.rb));
            double maxArea = Math.Max(Math.Abs(f1.area), Math.Abs(f2.area));
            if (maxRa < 0.001) maxRa = 1;
            if (maxRb < 0.001) maxRb = 1;
            if (maxArea < 0.001) maxArea = 1;

            double dRa = Math.Abs(f1.ra - f2.ra) / maxRa;
            double dRb = Math.Abs(f1.rb - f2.rb) / maxRb;
            double dA  = Math.Abs(f1.area - f2.area) / maxArea;
            return W_SIZE * Math.Min((dRa + dRb + dA) / 3.0 / E_SIZE, 1.0);
        }

        private double ShapeCost(FeaturePoint f1, FeaturePoint f2)
        {
            double maxAnis = Math.Max(Math.Abs(f1.anisometry), Math.Abs(f2.anisometry));
            if (maxAnis < 0.1) maxAnis = 0.1;
            double dAnis = Math.Abs(f1.anisometry - f2.anisometry) / maxAnis;
            double dCirc = Math.Abs(f1.circularity - f2.circularity);
            return W_ANIS * Math.Min(dAnis / E_ANIS, 1.0)
                 + W_CIRC * Math.Min(dCirc / E_CIRC, 1.0);
        }

        /// <summary>
        /// 匈牙利算法 — O(n³) 最小代价指派
        /// cost 是 n×n 方阵，assignment[i] = 分配给行 i 的列 j（-1 表示未匹配）
        /// </summary>
        private static void Hungarian(double[,] cost, out double totalCost, out int[] assignment)
        {
            int n = cost.GetLength(0);
            double[] u = new double[n + 1];
            double[] v = new double[n + 1];
            int[] p = new int[n + 1];
            int[] way = new int[n + 1];

            for (int i = 1; i <= n; i++)
            {
                p[0] = i;
                int j0 = 0;
                double[] minv = new double[n + 1];
                bool[] used = new bool[n + 1];
                for (int j = 0; j <= n; j++) minv[j] = 1e30;

                do
                {
                    used[j0] = true;
                    int i0 = p[j0];
                    double delta = 1e30;
                    int j1 = 0;
                    for (int j = 1; j <= n; j++)
                    {
                        if (!used[j])
                        {
                            double cur = cost[i0 - 1, j - 1] - u[i0] - v[j];
                            if (cur < minv[j])
                            {
                                minv[j] = cur;
                                way[j] = j0;
                            }
                            if (minv[j] < delta)
                            {
                                delta = minv[j];
                                j1 = j;
                            }
                        }
                    }
                    for (int j = 0; j <= n; j++)
                    {
                        if (used[j])
                        {
                            u[p[j]] += delta;
                            v[j] -= delta;
                        }
                        else
                        {
                            minv[j] -= delta;
                        }
                    }
                    j0 = j1;
                } while (p[j0] != 0);

                do
                {
                    int j1 = way[j0];
                    p[j0] = p[j1];
                    j0 = j1;
                } while (j0 != 0);
            }

            assignment = new int[n];
            for (int i = 0; i < n; i++) assignment[i] = -1;
            for (int j = 1; j <= n; j++)
            {
                if (p[j] > 0)
                {
                    assignment[p[j] - 1] = j - 1;
                }
            }

            totalCost = -v[0];
        }
    }
}
