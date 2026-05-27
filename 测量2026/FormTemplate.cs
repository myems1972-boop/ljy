using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using HalconDotNet;

namespace 测量2026
{
    public partial class FormTemplate : Form
    {
        HWindow hwindow;
        HObject ho_Image;
        HObject ho_ROI;
        HObject ho_ReducedImage;
        HObject ho_ModelContours;
        HTuple hv_ModelID;
        HTuple hv_DrawObj;
        Form1 parentForm;

        public FormTemplate(Form1 parent)
        {
            InitializeComponent();
            parentForm = parent;
            hwindow = hSmartWindowControl1.HalconWindow;
            hSmartWindowControl1.MouseWheel += HSmartWindow_MouseWheel;

            ho_Image = new HImage();
            ho_ROI = new HRegion();
            ho_ReducedImage = new HImage();
            ho_ModelContours = new HObject();
            HOperatorSet.GenEmptyObj(out ho_Image);
            HOperatorSet.GenEmptyObj(out ho_ROI);
            HOperatorSet.GenEmptyObj(out ho_ReducedImage);
            HOperatorSet.GenEmptyObj(out ho_ModelContours);
            hv_ModelID = new HTuple();
            hv_DrawObj = new HTuple();
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

            DetachDrawingObject();

            ho_Image.Dispose();
            ho_Image = img.Clone();

            HTuple width, height;
            HOperatorSet.GetImageSize(ho_Image, out width, out height);
            adapt_window(width, height);

            hwindow.ClearWindow();
            HOperatorSet.DispObj(ho_Image, hwindow);

            bnDrawROI.Enabled = true;
            bnDrawROI.Text = "绘制ROI";
            bnCreateModel.Enabled = false;
            bnSaveModel.Enabled = false;

            ho_ROI.Dispose();
            HOperatorSet.GenEmptyObj(out ho_ROI);
            ho_ModelContours.Dispose();
            HOperatorSet.GenEmptyObj(out ho_ModelContours);
            if (hv_ModelID.Length > 0)
            {
                HOperatorSet.ClearShapeModel(hv_ModelID);
                hv_ModelID = new HTuple();
            }
        }

        private void DetachDrawingObject()
        {
            if (hv_DrawObj.Length > 0)
            {
                try
                {
                    HOperatorSet.DetachDrawingObjectFromWindow(hwindow, hv_DrawObj);
                }
                catch { }
                HOperatorSet.ClearDrawingObject(hv_DrawObj);
                hv_DrawObj = new HTuple();
            }
        }

        private void bnDrawROI_Click(object sender, EventArgs e)
        {
            if (!ho_Image.IsInitialized())
            {
                MessageBox.Show("请先抓取图像");
                return;
            }

            try
            {
                DetachDrawingObject();

                HTuple imgWidth, imgHeight;
                HOperatorSet.GetImageSize(ho_Image, out imgWidth, out imgHeight);
                HTuple r1 = imgHeight * 0.3;
                HTuple c1 = imgWidth * 0.3;
                HTuple r2 = imgHeight * 0.7;
                HTuple c2 = imgWidth * 0.7;

                HOperatorSet.CreateDrawingObjectRectangle1(r1, c1, r2, c2, out hv_DrawObj);
                HOperatorSet.SetDrawingObjectParams(hv_DrawObj, "color", "red");
                HOperatorSet.SetDrawingObjectParams(hv_DrawObj, "line_width", 2);
                HOperatorSet.AttachDrawingObjectToWindow(hwindow, hv_DrawObj);

                hwindow.ClearWindow();
                HOperatorSet.SetColor(hwindow, "red");
                HOperatorSet.SetDraw(hwindow, "margin");
                HOperatorSet.DispObj(ho_Image, hwindow);

                bnCreateModel.Enabled = true;
                bnDrawROI.Text = "重绘ROI";
                MessageBox.Show("请在图像上拖拽矩形框调整ROI区域，完成后点击 创建模板 ");
            }
            catch (Exception ex)
            {
                MessageBox.Show("创建ROI失败: " + ex.Message);
            }
        }

        private void bnCreateModel_Click(object sender, EventArgs e)
        {
            if (hv_DrawObj.Length == 0)
            {
                MessageBox.Show("请先绘制ROI区域");
                return;
            }

            try
            {
                HTuple paramNames = new HTuple("row1", "column1", "row2", "column2");
                HTuple paramValues;
                HOperatorSet.GetDrawingObjectParams(hv_DrawObj, paramNames, out paramValues);
                double row1 = paramValues[0].D;
                double col1 = paramValues[1].D;
                double row2 = paramValues[2].D;
                double col2 = paramValues[3].D;

                ho_ROI.Dispose();
                HOperatorSet.GenRectangle1(out ho_ROI, row1, col1, row2, col2);

                ho_ReducedImage.Dispose();
                HOperatorSet.ReduceDomain(ho_Image, ho_ROI, out ho_ReducedImage);

                if (hv_ModelID.Length > 0)
                {
                    HOperatorSet.ClearShapeModel(hv_ModelID);
                }

                HOperatorSet.CreateScaledShapeModel(ho_ReducedImage, "auto",
                    (new HTuple(0)).TupleRad(), (new HTuple(360)).TupleRad(),
                    "auto", 0.6, 1.4, "auto", "auto", "use_polarity",
                    "auto", "auto", out hv_ModelID);

                ho_ModelContours.Dispose();
                HOperatorSet.GetShapeModelContours(out ho_ModelContours, hv_ModelID, 1);

                HTuple hv_Row1, hv_Col1, hv_Row2, hv_Col2;
                HOperatorSet.SmallestRectangle1Xld(ho_ModelContours, out hv_Row1, out hv_Col1, out hv_Row2, out hv_Col2);
                HTuple hv_RefRow = ((hv_Row2.TupleMax()) - (hv_Row1.TupleMin())) / 2;
                HTuple hv_RefCol = ((hv_Col2.TupleMax()) - (hv_Col1.TupleMin())) / 2;

                HTuple hv_HomMat2D;
                HOperatorSet.VectorAngleToRigid(0, 0, 0, hv_RefRow, hv_RefCol, 0, out hv_HomMat2D);

                HObject ho_TransContours;
                HOperatorSet.AffineTransContourXld(ho_ModelContours, out ho_TransContours, hv_HomMat2D);

                hwindow.ClearWindow();
                HOperatorSet.DispObj(ho_Image, hwindow);
                HOperatorSet.SetColor(hwindow, "yellow");
                HOperatorSet.SetDraw(hwindow, "margin");
                HOperatorSet.SetLineWidth(hwindow, 2);
                HOperatorSet.DispObj(ho_TransContours, hwindow);
                HOperatorSet.SetColor(hwindow, "red");
                HOperatorSet.DispObj(ho_ROI, hwindow);

                ho_TransContours.Dispose();

                // Generate thumbnail from window dump (captures rendered image + contours + ROI)
                GenerateThumbnailFromWindow();

                bnSaveModel.Enabled = true;

                string tempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "测量2026_template.shm");
                HOperatorSet.WriteShapeModel(hv_ModelID, tempPath);
                parentForm.LoadMatchModel(tempPath);

                MessageBox.Show("模板创建成功");
            }
            catch (Exception ex)
            {
                MessageBox.Show("创建模板失败: " + ex.Message);
            }
        }

        private void bnSaveModel_Click(object sender, EventArgs e)
        {
            if (hv_ModelID.Length == 0)
            {
                MessageBox.Show("请先创建模板");
                return;
            }

            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "Shape Model文件|*.shm";
            sfd.DefaultExt = "shm";
            sfd.FileName = "Template.shm";

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    HOperatorSet.WriteShapeModel(hv_ModelID, sfd.FileName);
                    MessageBox.Show("模板保存成功: " + sfd.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("保存模板失败: " + ex.Message);
                }
            }
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

        private Bitmap HImageToBitmap(HObject image)
        {
            HTuple channels;
            HOperatorSet.CountChannels(image, out channels);

            if (channels.I == 3)
            {
                HTuple ptrR, ptrG, ptrB, type, width, height;
                HOperatorSet.GetImagePointer3(image, out ptrR, out ptrG, out ptrB, out type, out width, out height);

                int w = width.I;
                int h = height.I;
                Bitmap bmp = new Bitmap(w, h, PixelFormat.Format24bppRgb);
                BitmapData bmpData = bmp.LockBits(
                    new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

                unsafe
                {
                    byte* pR = (byte*)ptrR.IP.ToPointer();
                    byte* pG = (byte*)ptrG.IP.ToPointer();
                    byte* pB = (byte*)ptrB.IP.ToPointer();
                    byte* pDst = (byte*)bmpData.Scan0.ToPointer();
                    int srcStride = w;
                    int dstStride = bmpData.Stride;
                    for (int y = 0; y < h; y++)
                    {
                        for (int x = 0; x < w; x++)
                        {
                            int srcIdx = y * srcStride + x;
                            int dstIdx = y * dstStride + x * 3;
                            pDst[dstIdx] = pB[srcIdx];
                            pDst[dstIdx + 1] = pG[srcIdx];
                            pDst[dstIdx + 2] = pR[srcIdx];
                        }
                    }
                }
                bmp.UnlockBits(bmpData);
                return bmp;
            }
            else
            {
                HTuple ptr, type, width, height;
                HOperatorSet.GetImagePointer1(image, out ptr, out type, out width, out height);

                int w = width.I;
                int h = height.I;
                Bitmap bmp = new Bitmap(w, h, PixelFormat.Format24bppRgb);
                BitmapData bmpData = bmp.LockBits(
                    new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

                unsafe
                {
                    byte* pSrc = (byte*)ptr.IP.ToPointer();
                    byte* pDst = (byte*)bmpData.Scan0.ToPointer();
                    int srcStride = w;
                    int dstStride = bmpData.Stride;
                    for (int y = 0; y < h; y++)
                    {
                        for (int x = 0; x < w; x++)
                        {
                            byte gray = pSrc[y * srcStride + x];
                            int dstIdx = y * dstStride + x * 3;
                            pDst[dstIdx] = gray;
                            pDst[dstIdx + 1] = gray;
                            pDst[dstIdx + 2] = gray;
                        }
                    }
                }
                bmp.UnlockBits(bmpData);
                return bmp;
            }
        }

        private void GenerateThumbnailFromWindow()
        {
            if (!ho_ReducedImage.IsInitialized())
                return;

            try
            {
                HObject ho_Cropped;
                HOperatorSet.CropDomain(ho_ReducedImage, out ho_Cropped);

                HTuple w, h;
                HOperatorSet.GetImageSize(ho_Cropped, out w, out h);
                int thumbW = 180;
                int thumbH = (int)(h.D * thumbW / w.D);
                if (thumbH > 90)
                {
                    thumbH = 90;
                    thumbW = (int)(w.D * thumbH / h.D);
                }

                HObject ho_Thumb;
                HOperatorSet.ZoomImageSize(ho_Cropped, out ho_Thumb, thumbW, thumbH, "constant");
                Bitmap bmp = HImageToBitmap(ho_Thumb);

                ho_Cropped.Dispose();
                ho_Thumb.Dispose();

                parentForm.SetTemplateThumbnail(bmp);
            }
            catch { }
        }

        private void FormTemplate_FormClosing(object sender, FormClosingEventArgs e)
        {
            DetachDrawingObject();
            if (hv_ModelID.Length > 0)
            {
                HOperatorSet.ClearShapeModel(hv_ModelID);
            }
            ho_Image.Dispose();
            ho_ROI.Dispose();
            ho_ReducedImage.Dispose();
            ho_ModelContours.Dispose();
        }
    }
}
