using System;
using System.Collections.Generic;
using ACadSharp;
using ACadSharp.Entities;
using ACadSharp.IO;
using CSMath;

namespace GridPlateInspector
{
    public static class DwgExtractor
    {
        const int SAMPLE_COUNT = 200;

        public static FeatureEncoding Extract(string dwgPath)
        {
            CadDocument doc;
            using (var reader = new DwgReader(dwgPath))
                doc = reader.Read();

            var closed = new List<Entity>();
            foreach (Entity ent in doc.Entities)
            {
                if (IsClosedEntity(ent))
                    closed.Add(ent);
            }

            if (closed.Count == 0)
                throw new Exception("DWG中没有找到闭合实体");

            // 找板外框（面积最大的闭合实体）
            Entity boardEnt = null;
            double maxArea = 0;
            foreach (var ent in closed)
            {
                double a = ComputeArea(ent);
                if (a > maxArea) { maxArea = a; boardEnt = ent; }
            }

            if (boardEnt == null)
                throw new Exception("无法识别板外框");

            // 板的定向包围盒
            var boardSamples = GetBoundarySamplePoints(boardEnt, SAMPLE_COUNT);
            ComputeOBB(boardSamples, out double bx, out double by, out double bPhi, out double bL, out double bW);

            // 提取孔特征
            var encoding = new FeatureEncoding();
            encoding.L = bL;
            encoding.W = bW;

            foreach (var ent in closed)
            {
                if (ent == boardEnt) continue;

                double area = ComputeArea(ent);
                double perim = ComputePerimeter(ent);
                double circ = (perim > 0.001) ? 4 * Math.PI * area / (perim * perim) : 0;
                if (circ > 1.0) circ = 1.0;

                (double cx, double cy) = ComputeCentroid(ent);

                var samples = GetBoundarySamplePoints(ent, SAMPLE_COUNT);
                ComputeEllipticAxes(samples, cx, cy, out double ra, out double rb);
                double anis = (rb > 0.001) ? ra / rb : 1.0;
                double bulk = (ra * rb > 0.001) ? area / (Math.PI * ra * rb) : 1.0;

                // 归一化坐标（匹配 FormBlob.cs 公式）
                double dx = cx - bx;
                double dy = cy - by;
                double ncx = (dx * Math.Cos(bPhi) + dy * Math.Sin(bPhi)) / bL;
                double ncy = (-dx * Math.Sin(bPhi) + dy * Math.Cos(bPhi)) / bW;

                encoding.features.Add(new FeaturePoint
                {
                    cx = ncx, cy = ncy,
                    area = area, circularity = circ,
                    anisometry = anis, bulkiness = bulk,
                    ra = ra, rb = rb
                });
            }

            return encoding;
        }

        // ======================== 实体分类 ========================

        static bool IsClosedEntity(Entity ent)
        {
            if (ent is Circle) return true;
            if (ent is Ellipse) return true;
            if (ent is LwPolyline lw && lw.IsClosed) return true;
            if (ent is Polyline2D p2 && p2.IsClosed) return true;
            return false;
        }

        // ======================== 面积 ========================

        static double ComputeArea(Entity ent)
        {
            if (ent is Circle c)
                return Math.PI * c.Radius * c.Radius;

            if (ent is Ellipse e)
                return Math.PI * e.MajorAxis * e.MinorAxis;

            if (ent is LwPolyline lw)
                return LwPolylineArea(lw);

            if (ent is Polyline2D p2)
                return Polyline2DArea(p2);

            return 0;
        }

        static double LwPolylineArea(LwPolyline lw)
        {
            var verts = new List<LwPolyline.Vertex>(lw.Vertices);
            int n = verts.Count;
            if (n < 3) return 0;

            double area = 0;
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                double xi = verts[i].Location.X;
                double yi = verts[i].Location.Y;
                double xj = verts[j].Location.X;
                double yj = verts[j].Location.Y;
                area += xi * yj - xj * yi;
            }
            area *= 0.5;

            // 弧段修正
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                double bulge = verts[i].Bulge;
                if (Math.Abs(bulge) < 1e-10) continue;

                (double arcCx, double arcCy, double arcR, double theta, bool cw) =
                    BulgeToArc(verts[i].Location.X, verts[i].Location.Y,
                               verts[j].Location.X, verts[j].Location.Y, bulge);

                double sectorArea = arcR * arcR * theta / 2;
                double triArea = arcR * arcR * Math.Sin(theta) / 2;
                double segArea = sectorArea - triArea; // positive for CCW
                if (bulge > 0) area += segArea;
                else area -= segArea;
            }

            return Math.Abs(area);
        }

        static double Polyline2DArea(Polyline2D poly)
        {
            var verts = new List<Vertex2D>();
            verts.AddRange(poly.Vertices);
            int n = verts.Count;
            if (n < 3) return 0;

            double area = 0;
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                area += verts[i].Location.X * verts[j].Location.Y
                      - verts[j].Location.X * verts[i].Location.Y;
            }
            return Math.Abs(area * 0.5);
        }

        // ======================== 周长 ========================

        static double ComputePerimeter(Entity ent)
        {
            if (ent is Circle c)
                return 2 * Math.PI * c.Radius;

            if (ent is Ellipse e)
            {
                double a = e.MajorAxis;
                double b = e.MinorAxis;
                return Math.PI * (3 * (a + b) - Math.Sqrt((3 * a + b) * (a + 3 * b)));
            }

            if (ent is LwPolyline lw)
                return LwPolylinePerimeter(lw);

            if (ent is Polyline2D p2)
                return Polyline2DPerimeter(p2);

            return 0;
        }

        static double LwPolylinePerimeter(LwPolyline lw)
        {
            var verts = new List<LwPolyline.Vertex>(lw.Vertices);
            int n = verts.Count;
            if (n < 2) return 0;

            double perim = 0;
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                double x1 = verts[i].Location.X, y1 = verts[i].Location.Y;
                double x2 = verts[j].Location.X, y2 = verts[j].Location.Y;
                double bulge = verts[i].Bulge;

                if (Math.Abs(bulge) < 1e-10)
                {
                    perim += Math.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));
                }
                else
                {
                    (_, _, double arcR, double theta, _) =
                        BulgeToArc(x1, y1, x2, y2, bulge);
                    perim += arcR * theta;
                }
            }
            return perim;
        }

        static double Polyline2DPerimeter(Polyline2D poly)
        {
            var verts = new List<Vertex2D>();
            verts.AddRange(poly.Vertices);
            int n = verts.Count;
            if (n < 2) return 0;

            double perim = 0;
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                double dx = verts[j].Location.X - verts[i].Location.X;
                double dy = verts[j].Location.Y - verts[i].Location.Y;
                perim += Math.Sqrt(dx * dx + dy * dy);
            }
            return perim;
        }

        // ======================== 质心 ========================

        static (double cx, double cy) ComputeCentroid(Entity ent)
        {
            if (ent is Circle c)
                return (c.Center.X, c.Center.Y);

            if (ent is Ellipse e)
                return (e.Center.X, e.Center.Y);

            if (ent is LwPolyline lw)
                return LwPolylineCentroid(lw);

            if (ent is Polyline2D p2)
                return Polyline2DCentroid(p2);

            return (0, 0);
        }

        static (double cx, double cy) LwPolylineCentroid(LwPolyline lw)
        {
            var verts = new List<LwPolyline.Vertex>(lw.Vertices);
            int n = verts.Count;
            if (n < 3) return (0, 0);

            double totalArea = 0;
            double cx = 0, cy = 0;

            // 多边形部分
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                double xi = verts[i].Location.X, yi = verts[i].Location.Y;
                double xj = verts[j].Location.X, yj = verts[j].Location.Y;
                double cross = xi * yj - xj * yi;
                totalArea += cross;
                cx += (xi + xj) * cross;
                cy += (yi + yj) * cross;
            }

            // 弧段修正
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                double bulge = verts[i].Bulge;
                if (Math.Abs(bulge) < 1e-10) continue;

                double x1 = verts[i].Location.X, y1 = verts[i].Location.Y;
                double x2 = verts[j].Location.X, y2 = verts[j].Location.Y;

                (double arcCx, double arcCy, double arcR, double theta, bool cw) =
                    BulgeToArc(x1, y1, x2, y2, bulge);

                double segArea = arcR * arcR * (theta - Math.Sin(theta)) / 2;
                if (bulge < 0) segArea = -segArea;

                // 弧段质心（相对于弦中点，沿法线方向到弧中心）
                double sinHalf = Math.Sin(theta / 2);
                double segCentroidDist = 0;
                if (Math.Abs(theta - Math.Sin(theta)) > 1e-12)
                    segCentroidDist = (4.0 * arcR * sinHalf * sinHalf * sinHalf) / (3.0 * (theta - Math.Sin(theta)));

                double mx = (x1 + x2) / 2, my = (y1 + y2) / 2;
                double nx = arcCx - mx, ny = arcCy - my;
                double ndist = Math.Sqrt(nx * nx + ny * ny);
                if (ndist > 1e-12) { nx /= ndist; ny /= ndist; }

                double segCx = mx + nx * segCentroidDist;
                double segCy = my + ny * segCentroidDist;

                totalArea += segArea * 2; // 转换为 shoelace 的 2x 面积约定
                cx += segCx * segArea * 2;
                cy += segCy * segArea * 2;
            }

            if (Math.Abs(totalArea) < 1e-10) return (0, 0);
            return (cx / (3 * totalArea / 2), cy / (3 * totalArea / 2));
        }

        static (double cx, double cy) Polyline2DCentroid(Polyline2D poly)
        {
            var verts = new List<Vertex2D>();
            verts.AddRange(poly.Vertices);
            int n = verts.Count;
            if (n < 3) return (0, 0);

            double totalArea = 0;
            double cx = 0, cy = 0;

            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                double xi = verts[i].Location.X, yi = verts[i].Location.Y;
                double xj = verts[j].Location.X, yj = verts[j].Location.Y;
                double cross = xi * yj - xj * yi;
                totalArea += cross;
                cx += (xi + xj) * cross;
                cy += (yi + yj) * cross;
            }

            if (Math.Abs(totalArea) < 1e-10) return (0, 0);
            return (cx / (3 * totalArea), cy / (3 * totalArea));
        }

        // ======================== 边界采样 ========================

        static (double x, double y)[] GetBoundarySamplePoints(Entity ent, int n)
        {
            if (ent is Circle c)
                return SampleCircle(c, n);
            if (ent is Ellipse e)
                return SampleEllipse(e, n);
            if (ent is LwPolyline lw)
                return SampleLwPolyline(lw, n);
            if (ent is Polyline2D p2)
                return SamplePolyline2D(p2, n);
            return new (double, double)[0];
        }

        static (double, double)[] SampleCircle(Circle c, int n)
        {
            var pts = new (double, double)[n];
            for (int i = 0; i < n; i++)
            {
                double angle = 2 * Math.PI * i / n;
                pts[i] = (c.Center.X + c.Radius * Math.Cos(angle),
                          c.Center.Y + c.Radius * Math.Sin(angle));
            }
            return pts;
        }

        static (double, double)[] SampleEllipse(Ellipse e, int n)
        {
            var pts = new (double, double)[n];
            double a = e.MajorAxis;
            double b = e.MinorAxis;
            double phi = e.Rotation;
            for (int i = 0; i < n; i++)
            {
                double t = 2 * Math.PI * i / n;
                double lx = a * Math.Cos(t);
                double ly = b * Math.Sin(t);
                pts[i] = (e.Center.X + lx * Math.Cos(phi) - ly * Math.Sin(phi),
                          e.Center.Y + lx * Math.Sin(phi) + ly * Math.Cos(phi));
            }
            return pts;
        }

        static (double, double)[] SampleLwPolyline(LwPolyline lw, int n)
        {
            var verts = new List<LwPolyline.Vertex>(lw.Vertices);
            int nv = verts.Count;

            // 计算总周长
            double totalLen = 0;
            double[] segLens = new double[nv];
            for (int i = 0; i < nv; i++)
            {
                int j = (i + 1) % nv;
                double bulge = verts[i].Bulge;
                if (Math.Abs(bulge) < 1e-10)
                    segLens[i] = Math.Sqrt(Sq(verts[j].Location.X - verts[i].Location.X) + Sq(verts[j].Location.Y - verts[i].Location.Y));
                else
                {
                    (_, _, double ar, double th, _) = BulgeToArc(
                        verts[i].Location.X, verts[i].Location.Y,
                        verts[j].Location.X, verts[j].Location.Y, bulge);
                    segLens[i] = ar * th;
                }
                totalLen += segLens[i];
            }

            // 等弧长采样
            var pts = new (double, double)[n];
            for (int k = 0; k < n; k++)
            {
                double s = totalLen * k / n;
                pts[k] = PointAtDistance(verts, segLens, totalLen, s);
            }
            return pts;
        }

        static (double, double)[] SamplePolyline2D(Polyline2D poly, int n)
        {
            var verts = new List<Vertex2D>();
            verts.AddRange(poly.Vertices);
            int nv = verts.Count;
            double[] segLens = new double[nv];
            double totalLen = 0;
            for (int i = 0; i < nv; i++)
            {
                int j = (i + 1) % nv;
                segLens[i] = Math.Sqrt(Sq(verts[j].Location.X - verts[i].Location.X) + Sq(verts[j].Location.Y - verts[i].Location.Y));
                totalLen += segLens[i];
            }

            var pts = new (double, double)[n];
            for (int k = 0; k < n; k++)
            {
                double s = totalLen * k / n;
                double accum = 0;
                int seg = 0;
                for (; seg < nv; seg++)
                {
                    if (accum + segLens[seg] >= s) break;
                    accum += segLens[seg];
                }
                if (seg >= nv) seg = nv - 1;
                int sj = (seg + 1) % nv;
                double t = segLens[seg] > 1e-12 ? (s - accum) / segLens[seg] : 0;
                if (t < 0) t = 0; if (t > 1) t = 1;
                pts[k] = (verts[seg].Location.X + t * (verts[sj].Location.X - verts[seg].Location.X),
                          verts[seg].Location.Y + t * (verts[sj].Location.Y - verts[seg].Location.Y));
            }
            return pts;
        }

        static (double, double) PointAtDistance(List<LwPolyline.Vertex> verts, double[] segLens, double totalLen, double s)
        {
            double accum = 0;
            int nv = verts.Count;
            for (int i = 0; i < nv; i++)
            {
                double segLen = segLens[i];
                if (accum + segLen >= s || i == nv - 1)
                {
                    double t = segLen > 1e-12 ? (s - accum) / segLen : 0;
                    if (t < 0) t = 0; if (t > 1) t = 1;
                    int j = (i + 1) % nv;
                    double x1 = verts[i].Location.X, y1 = verts[i].Location.Y;
                    double x2 = verts[j].Location.X, y2 = verts[j].Location.Y;
                    double bulge = verts[i].Bulge;

                    if (Math.Abs(bulge) < 1e-10)
                    {
                        return (x1 + t * (x2 - x1), y1 + t * (y2 - y1));
                    }
                    else
                    {
                        (double acx, double acy, double ar, double theta, bool cw) =
                            BulgeToArc(x1, y1, x2, y2, bulge);
                        // 弧上的方向角：从圆心到 P1 的角度 + t * theta
                        double a1 = Math.Atan2(y1 - acy, x1 - acx);
                        double aCur = a1 + t * theta * (cw ? -1 : 1);
                        return (acx + ar * Math.Cos(aCur), acy + ar * Math.Sin(aCur));
                    }
                }
                accum += segLen;
            }
            // fallback: 最后一个点
            return (verts[nv - 1].Location.X, verts[nv - 1].Location.Y);
        }

        // ======================== Bulge 数学 ========================

        /// <summary>
        /// bulge = tan(included_angle/4), 返回 (arcCenterX, arcCenterY, radius, theta, clockwise?)
        /// </summary>
        static (double cx, double cy, double r, double theta, bool cw) BulgeToArc(
            double x1, double y1, double x2, double y2, double bulge)
        {
            double dx = x2 - x1, dy = y2 - y1;
            double chordLen = Math.Sqrt(dx * dx + dy * dy);
            double absB = Math.Abs(bulge);
            double theta = 4 * Math.Atan(absB);
            double r = (absB > 1e-12) ? chordLen * (1 + absB * absB) / (4 * absB) : double.MaxValue;
            double dCenter = (absB > 1e-12) ? chordLen * (1 - absB * absB) / (4 * absB) : 0;
            // 左法线 n = (-dy/chordLen, dx/chordLen)
            double nx = -dy / chordLen;
            double ny = dx / chordLen;
            double cx = (x1 + x2) / 2 + nx * dCenter * Math.Sign(bulge);
            double cy = (y1 + y2) / 2 + ny * dCenter * Math.Sign(bulge);
            return (cx, cy, r, theta, bulge < 0);
        }

        // ======================== PCA / 椭圆轴 ========================

        static void ComputeEllipticAxes((double x, double y)[] samples, double cx, double cy,
            out double ra, out double rb)
        {
            int n = samples.Length;
            double m20 = 0, m02 = 0, m11 = 0;
            for (int i = 0; i < n; i++)
            {
                double dx = samples[i].x - cx;
                double dy = samples[i].y - cy;
                m20 += dx * dx;
                m02 += dy * dy;
                m11 += dx * dy;
            }
            m20 /= n; m02 /= n; m11 /= n;

            double trace = m20 + m02;
            double det = m20 * m02 - m11 * m11;
            double disc = Math.Sqrt(Math.Max(0, trace * trace - 4 * det));
            double lambda1 = (trace + disc) / 2;
            double lambda2 = (trace - disc) / 2;

            ra = Math.Sqrt(Math.Max(0, lambda1));
            rb = Math.Sqrt(Math.Max(0, lambda2));
        }

        static void ComputeOBB((double x, double y)[] samples,
            out double cx, out double cy, out double phi, out double L, out double W)
        {
            int n = samples.Length;

            // 质心
            cx = 0; cy = 0;
            for (int i = 0; i < n; i++) { cx += samples[i].x; cy += samples[i].y; }
            cx /= n; cy /= n;

            // 协方差
            double m20 = 0, m02 = 0, m11 = 0;
            for (int i = 0; i < n; i++)
            {
                double dx = samples[i].x - cx;
                double dy = samples[i].y - cy;
                m20 += dx * dx; m02 += dy * dy; m11 += dx * dy;
            }
            m20 /= n; m02 /= n; m11 /= n;

            // 特征向量
            double trace = m20 + m02;
            double det = m20 * m02 - m11 * m11;
            double disc = Math.Sqrt(Math.Max(0, trace * trace - 4 * det));
            double lambda1 = (trace + disc) / 2;
            double lambda2 = (trace - disc) / 2;

            // eigenvector for lambda1 (主方向)
            double vx, vy;
            if (Math.Abs(m11) > 1e-12)
            {
                vx = lambda1 - m02;
                vy = m11;
            }
            else if (m20 >= m02)
            {
                vx = 1; vy = 0;
            }
            else
            {
                vx = 0; vy = 1;
            }
            double vn = Math.Sqrt(vx * vx + vy * vy);
            vx /= vn; vy /= vn;

            phi = Math.Atan2(vy, vx);

            // 沿两个轴投影求 L, W
            double min1 = double.MaxValue, max1 = double.MinValue;
            double min2 = double.MaxValue, max2 = double.MinValue;
            for (int i = 0; i < n; i++)
            {
                double dx = samples[i].x - cx;
                double dy = samples[i].y - cy;
                double proj1 = dx * vx + dy * vy;
                double proj2 = -dx * vy + dy * vx; // 垂直方向
                min1 = Math.Min(min1, proj1); max1 = Math.Max(max1, proj1);
                min2 = Math.Min(min2, proj2); max2 = Math.Max(max2, proj2);
            }
            L = max1 - min1;
            W = max2 - min2;
        }

        // ======================== 工具 ========================

        static double Sq(double x) => x * x;
    }
}
