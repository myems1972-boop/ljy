using System;
using System.Drawing;
using System.Windows.Forms;
using HalconDotNet;

namespace 测量2026
{
    public partial class FormBlob : Form
    {
        HWindow hwindow;
        HObject ho_Image;
        Form1 parentForm;
        FeatureEncoding m_LastEncoding;

        public FormBlob(Form1 parent)
        {
            InitializeComponent();
            parentForm = parent;
            hwindow = hSmartWindowControl1.HalconWindow;
            hSmartWindowControl1.MouseWheel += HSmartWindow_MouseWheel;

            ho_Image = new HImage();
            HOperatorSet.GenEmptyObj(out ho_Image);
        }

        private void HSmartWindow_MouseWheel(object sender, MouseEventArgs e)
        {
            System.Drawing.Point pt = this.Location;
            MouseEventArgs newe = new MouseEventArgs(e.Button, e.Clicks, e.X - pt.X, e.Y - pt.Y, e.Delta);
            hSmartWindowControl1.HSmartWindowControl_MouseWheel(sender, newe);
        }

        private void bnGrabImage_Click(object sender, EventArgs e)
        {
            HObject img = parentForm.GetCurrentImage();
            if (img == null || !img.IsInitialized())
            {
                MessageBox.Show("Form1窗口没有可用的图像，请先打开相机并开始采集");
                return;
            }

            ho_Image.Dispose();
            ho_Image = img.Clone();

            HTuple width, height;
            HOperatorSet.GetImageSize(ho_Image, out width, out height);
            adapt_window(width, height);

            hwindow.ClearWindow();
            HOperatorSet.DispObj(ho_Image, hwindow);
        }

        private void adapt_window(int width, int height)
        {
            double ratioWidth = (1.0) * width / hSmartWindowControl1.Width;
            double ratioHeight = (1.0) * height / hSmartWindowControl1.Height;
            HTuple row1, column1, row2, column2;
            if (ratioWidth >= ratioHeight)
            {
                row1 = -(1.0) * ((hSmartWindowControl1.Height * ratioWidth) - height) / 2;
                column1 = 0;
                row2 = row1 + hSmartWindowControl1.Height * ratioWidth;
                column2 = column1 + hSmartWindowControl1.Width * ratioWidth;
            }
            else
            {
                row1 = 0;
                column1 = -(1.0) * ((hSmartWindowControl1.Width * ratioHeight) - width) / 2;
                row2 = row1 + hSmartWindowControl1.Height * ratioHeight;
                column2 = column1 + hSmartWindowControl1.Width * ratioHeight;
            }
            hwindow.SetPart(row1, column1, row2, column2);
        }

        private void bnBlobAnalyze_Click(object sender, EventArgs e)
        {
            if (!ho_Image.IsInitialized())
            {
                MessageBox.Show("没有图像，请先抓取图像");
                return;
            }

            HObject ho_Median, ho_Regions, ho_RegionClosing, ho_ConnectedRegions;
            HObject ho_RegionFillUp, ho_Selected, ho_FinalSquare, ho_myRegion;
            HObject ho_ImageReduced, ho_mRegions, ho_Regionopen, ho_ConnectedmRegions;
            HObject ho_bigRegions, ho_circuRegions, ho_rectRegions, ho_otherRegions, ho_combined;

            HOperatorSet.GenEmptyObj(out ho_Median);
            HOperatorSet.GenEmptyObj(out ho_Regions);
            HOperatorSet.GenEmptyObj(out ho_RegionClosing);
            HOperatorSet.GenEmptyObj(out ho_ConnectedRegions);
            HOperatorSet.GenEmptyObj(out ho_RegionFillUp);
            HOperatorSet.GenEmptyObj(out ho_Selected);
            HOperatorSet.GenEmptyObj(out ho_FinalSquare);
            HOperatorSet.GenEmptyObj(out ho_myRegion);
            HOperatorSet.GenEmptyObj(out ho_ImageReduced);
            HOperatorSet.GenEmptyObj(out ho_mRegions);
            HOperatorSet.GenEmptyObj(out ho_Regionopen);
            HOperatorSet.GenEmptyObj(out ho_ConnectedmRegions);
            HOperatorSet.GenEmptyObj(out ho_bigRegions);
            HOperatorSet.GenEmptyObj(out ho_circuRegions);
            HOperatorSet.GenEmptyObj(out ho_rectRegions);
            HOperatorSet.GenEmptyObj(out ho_otherRegions);
            HOperatorSet.GenEmptyObj(out ho_combined);

            try
            {
                // 1. 中值滤波
                HOperatorSet.MedianImage(ho_Image, out ho_Median, "circle", 1, "mirrored");

                // 2. 阈值提取亮区域
                HOperatorSet.Threshold(ho_Median, out ho_Regions, 100, 255);

                // 3. 圆形闭运算填补孔洞
                HOperatorSet.ClosingCircle(ho_Regions, out ho_RegionClosing, 3);

                // 4. 连通域分割
                HOperatorSet.Connection(ho_RegionClosing, out ho_ConnectedRegions);

                // 5. 填充空洞
                HOperatorSet.FillUp(ho_ConnectedRegions, out ho_RegionFillUp);

                // 6. 筛选大面积区域（目标板）
                HOperatorSet.SelectShape(ho_RegionFillUp, out ho_Selected, "area", "and", 200000, 999999);

                // 7. 最小外接旋转矩形
                HTuple hv_Row, hv_Column, hv_Phi, hv_Length1, hv_Length2;
                HOperatorSet.SmallestRectangle2(ho_Selected, out hv_Row, out hv_Column,
                    out hv_Phi, out hv_Length1, out hv_Length2);

                // 8. 生成矩形XLD轮廓
                HOperatorSet.GenRectangle2ContourXld(out ho_FinalSquare, hv_Row, hv_Column,
                    hv_Phi, hv_Length1, hv_Length2);

                // 9. XLD转Region得到掩膜
                HOperatorSet.GenRegionContourXld(ho_FinalSquare, out ho_myRegion, "filled");

                // 10. 裁剪原图到板区域
                HOperatorSet.ReduceDomain(ho_Image, ho_myRegion, out ho_ImageReduced);

                // 11. 提取板内暗区域
                HOperatorSet.Threshold(ho_ImageReduced, out ho_mRegions, 0, 40);

                // 12. 圆形开运算去噪
                HOperatorSet.OpeningCircle(ho_mRegions, out ho_Regionopen, 7);

                // 13. 连通域分割
                HOperatorSet.Connection(ho_Regionopen, out ho_ConnectedmRegions);

                // 14. 筛选中等大小区域
                HOperatorSet.SelectShape(ho_ConnectedmRegions, out ho_bigRegions,
                    "area", "and", 25000, 99999);

                // 15. 筛选圆形标记（红色）
                HOperatorSet.SelectShape(ho_bigRegions, out ho_circuRegions,
                    "circularity", "and", 0.8, 1);

                // 16. 筛选矩形标记（蓝色）
                HOperatorSet.SelectShape(ho_bigRegions, out ho_rectRegions,
                    "rectangularity", "and", 0.9, 1);

                // 17. 其余孔（半圆、椭圆、不规则的） → 黄色
                HOperatorSet.Union2(ho_circuRegions, ho_rectRegions, out ho_combined);
                HOperatorSet.Difference(ho_bigRegions, ho_combined, out ho_otherRegions);

                // 显示结果
                hwindow.ClearWindow();
                HOperatorSet.DispObj(ho_Image, hwindow);

                hwindow.SetColor("yellow");
                HOperatorSet.DispObj(ho_otherRegions, hwindow);

                hwindow.SetColor("blue");
                HOperatorSet.DispObj(ho_rectRegions, hwindow);

                hwindow.SetColor("red");
                HOperatorSet.DispObj(ho_circuRegions, hwindow);

                hwindow.SetColor("green");
                HOperatorSet.DispObj(ho_FinalSquare, hwindow);
            }
            finally
            {
                ho_Median.Dispose();
                ho_Regions.Dispose();
                ho_RegionClosing.Dispose();
                ho_ConnectedRegions.Dispose();
                ho_RegionFillUp.Dispose();
                ho_Selected.Dispose();
                ho_FinalSquare.Dispose();
                ho_myRegion.Dispose();
                ho_ImageReduced.Dispose();
                ho_mRegions.Dispose();
                ho_Regionopen.Dispose();
                ho_ConnectedmRegions.Dispose();
                ho_bigRegions.Dispose();
                ho_circuRegions.Dispose();
                ho_rectRegions.Dispose();
                ho_otherRegions.Dispose();
                ho_combined.Dispose();
            }
        }

        private void bnSaveImage_Click(object sender, EventArgs e)
        {
            if (!ho_Image.IsInitialized())
            {
                MessageBox.Show("没有可保存的图像，请先抓取图像");
                return;
            }

            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = "PNG图像|*.png";
            dlg.DefaultExt = "png";
            dlg.FileName = "image.png";
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                HOperatorSet.WriteImage(ho_Image, "png", 0, dlg.FileName);
            }
        }

        private void bnEncode_Click(object sender, EventArgs e)
        {
            if (!ho_Image.IsInitialized())
            {
                MessageBox.Show("没有图像，请先抓取图像");
                return;
            }

            HObject ho_Median, ho_Regions, ho_RegionClosing, ho_ConnectedRegions;
            HObject ho_RegionFillUp, ho_Selected, ho_FinalSquare, ho_myRegion;
            HObject ho_ImageReduced, ho_mRegions, ho_Regionopen, ho_ConnectedmRegions;
            HObject ho_bigRegions, ho_circuRegions, ho_rectRegions;

            HOperatorSet.GenEmptyObj(out ho_Median);
            HOperatorSet.GenEmptyObj(out ho_Regions);
            HOperatorSet.GenEmptyObj(out ho_RegionClosing);
            HOperatorSet.GenEmptyObj(out ho_ConnectedRegions);
            HOperatorSet.GenEmptyObj(out ho_RegionFillUp);
            HOperatorSet.GenEmptyObj(out ho_Selected);
            HOperatorSet.GenEmptyObj(out ho_FinalSquare);
            HOperatorSet.GenEmptyObj(out ho_myRegion);
            HOperatorSet.GenEmptyObj(out ho_ImageReduced);
            HOperatorSet.GenEmptyObj(out ho_mRegions);
            HOperatorSet.GenEmptyObj(out ho_Regionopen);
            HOperatorSet.GenEmptyObj(out ho_ConnectedmRegions);
            HOperatorSet.GenEmptyObj(out ho_bigRegions);
            HOperatorSet.GenEmptyObj(out ho_circuRegions);
            HOperatorSet.GenEmptyObj(out ho_rectRegions);

            try
            {
                HOperatorSet.MedianImage(ho_Image, out ho_Median, "circle", 1, "mirrored");
                HOperatorSet.Threshold(ho_Median, out ho_Regions, 100, 255);
                HOperatorSet.ClosingCircle(ho_Regions, out ho_RegionClosing, 3);
                HOperatorSet.Connection(ho_RegionClosing, out ho_ConnectedRegions);
                HOperatorSet.FillUp(ho_ConnectedRegions, out ho_RegionFillUp);
                HOperatorSet.SelectShape(ho_RegionFillUp, out ho_Selected, "area", "and", 200000, 999999);

                HTuple hv_Row, hv_Column, hv_Phi, hv_Length1, hv_Length2;
                HOperatorSet.SmallestRectangle2(ho_Selected, out hv_Row, out hv_Column,
                    out hv_Phi, out hv_Length1, out hv_Length2);
                HOperatorSet.GenRectangle2ContourXld(out ho_FinalSquare, hv_Row, hv_Column,
                    hv_Phi, hv_Length1, hv_Length2);
                HOperatorSet.GenRegionContourXld(ho_FinalSquare, out ho_myRegion, "filled");
                HOperatorSet.ReduceDomain(ho_Image, ho_myRegion, out ho_ImageReduced);
                HOperatorSet.Threshold(ho_ImageReduced, out ho_mRegions, 0, 40);
                HOperatorSet.OpeningCircle(ho_mRegions, out ho_Regionopen, 7);
                HOperatorSet.Connection(ho_Regionopen, out ho_ConnectedmRegions);
                HOperatorSet.SelectShape(ho_ConnectedmRegions, out ho_bigRegions,
                    "area", "and", 25000, 99999);

                double L = hv_Length1[0].D * 2;
                double W = hv_Length2[0].D * 2;

                HTuple hv_Area, hv_FeatRow, hv_FeatCol;
                HOperatorSet.AreaCenter(ho_bigRegions, out hv_Area, out hv_FeatRow, out hv_FeatCol);

                HTuple hv_Circ;
                HOperatorSet.Circularity(ho_bigRegions, out hv_Circ);

                HTuple hv_Ra, hv_Rb, hv_FeatPhi;
                HOperatorSet.EllipticAxis(ho_bigRegions, out hv_Ra, out hv_Rb, out hv_FeatPhi);

                int n = hv_Area.Length;
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.Append("{\r\n");
                sb.AppendFormat("  \"L\": {0:F1},\r\n", L);
                sb.AppendFormat("  \"W\": {0:F1},\r\n", W);
                sb.Append("  \"features\": [\r\n");

                for (int i = 0; i < n; i++)
                {
                    double dx = hv_FeatCol[i].D - hv_Column[0].D;
                    double dy = hv_FeatRow[i].D - hv_Row[0].D;
                    double phi = hv_Phi[0].D;
                    double cx = (dx * Math.Cos(phi) + dy * Math.Sin(phi)) / L;
                    double cy = (-dx * Math.Sin(phi) + dy * Math.Cos(phi)) / W;

                    double area = hv_Area[i].D;
                    double ra = hv_Ra[i].D;
                    double rb = hv_Rb[i].D;
                    double anis = (rb > 0.001) ? ra / rb : 0;
                    double bulk = (ra * rb > 0.001) ? area / (Math.PI * ra * rb) : 0;

                    sb.Append("    {\r\n");
                    sb.AppendFormat("      \"cx\": {0:F4},\r\n", cx);
                    sb.AppendFormat("      \"cy\": {0:F4},\r\n", cy);
                    sb.AppendFormat("      \"area\": {0:F1},\r\n", area);
                    sb.AppendFormat("      \"circularity\": {0:F3},\r\n", hv_Circ[i].D);
                    sb.AppendFormat("      \"anisometry\": {0:F3},\r\n", anis);
                    sb.AppendFormat("      \"bulkiness\": {0:F3},\r\n", bulk);
                    sb.AppendFormat("      \"ra\": {0:F1},\r\n", ra);
                    sb.AppendFormat("      \"rb\": {0:F1}\r\n", rb);
                    sb.Append("    }");
                    if (i < n - 1) sb.Append(",");
                    sb.Append("\r\n");
                }
                sb.Append("  ]\r\n");
                sb.Append("}");

                string json = sb.ToString();
                Clipboard.SetText(json);

                m_LastEncoding = new FeatureEncoding();
                m_LastEncoding.L = L;
                m_LastEncoding.W = W;
                for (int i = 0; i < n; i++)
                {
                    double dx = hv_FeatCol[i].D - hv_Column[0].D;
                    double dy = hv_FeatRow[i].D - hv_Row[0].D;
                    double phi = hv_Phi[0].D;
                    double cx = (dx * Math.Cos(phi) + dy * Math.Sin(phi)) / L;
                    double cy = (-dx * Math.Sin(phi) + dy * Math.Cos(phi)) / W;
                    double area = hv_Area[i].D;
                    double ra = hv_Ra[i].D;
                    double rb = hv_Rb[i].D;
                    double anis = (rb > 0.001) ? ra / rb : 0;
                    double bulk = (ra * rb > 0.001) ? area / (Math.PI * ra * rb) : 0;
                    m_LastEncoding.features.Add(new FeaturePoint
                    {
                        cx = cx, cy = cy, area = area,
                        circularity = hv_Circ[i].D,
                        anisometry = anis,
                        bulkiness = bulk,
                        ra = ra, rb = rb
                    });
                }

                hwindow.ClearWindow();
                HOperatorSet.DispObj(ho_Image, hwindow);
                HOperatorSet.SetColor(hwindow, "green");
                HOperatorSet.DispObj(ho_FinalSquare, hwindow);
                HOperatorSet.SetColor(hwindow, "cyan");
                HOperatorSet.DispObj(ho_bigRegions, hwindow);

                // 显示每个孔的归一化坐标
                for (int i = 0; i < n; i++)
                {
                    double dx = hv_FeatCol[i].D - hv_Column[0].D;
                    double dy = hv_FeatRow[i].D - hv_Row[0].D;
                    double phi = hv_Phi[0].D;
                    double cx = (dx * Math.Cos(phi) + dy * Math.Sin(phi)) / L;
                    double cy = (-dx * Math.Sin(phi) + dy * Math.Cos(phi)) / W;
                    string label = string.Format("{0}:({1:F3},{2:F3})", i + 1, cx, cy);
                    HOperatorSet.DispText(hwindow, label, "image", hv_FeatRow[i], hv_FeatCol[i],
                        "yellow", "box", "true");
                }

                MessageBox.Show("编码已复制到剪贴板，共 " + n + " 个特征\nL=" + L.ToString("F1") + " W=" + W.ToString("F1"));
            }
            catch (Exception ex)
            {
                MessageBox.Show("编码失败: " + ex.Message);
            }
            finally
            {
                ho_Median.Dispose();
                ho_Regions.Dispose();
                ho_RegionClosing.Dispose();
                ho_ConnectedRegions.Dispose();
                ho_RegionFillUp.Dispose();
                ho_Selected.Dispose();
                ho_FinalSquare.Dispose();
                ho_myRegion.Dispose();
                ho_ImageReduced.Dispose();
                ho_mRegions.Dispose();
                ho_Regionopen.Dispose();
                ho_ConnectedmRegions.Dispose();
                ho_bigRegions.Dispose();
                ho_circuRegions.Dispose();
                ho_rectRegions.Dispose();
            }
        }

        private void bnMatch_Click(object sender, EventArgs e)
        {
            if (m_LastEncoding == null || m_LastEncoding.features.Count == 0)
            {
                MessageBox.Show("请先点击 编码 按钮生成编码");
                return;
            }
            FormMatch formMatch = new FormMatch(m_LastEncoding);
            formMatch.Show();
        }

        private void FormBlob_FormClosing(object sender, FormClosingEventArgs e)
        {
            ho_Image.Dispose();
        }
    }
}
