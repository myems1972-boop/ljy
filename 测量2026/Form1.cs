using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using HalconDotNet;
using MvCamCtrl.NET;
using System.Runtime.InteropServices;
using System.Threading;

namespace 测量2026
{
    public partial class Form1 : Form
    {
        MyCamera.MV_CC_DEVICE_INFO_LIST m_pDeviceList;
        private MyCamera m_pMyCamera;
        bool m_bGrabbing;
        HWindow hwindow;
        MyCamera.MV_FRAME_OUT stFrameOut = new MyCamera.MV_FRAME_OUT();
        IntPtr pImageBuf = IntPtr.Zero;
        HTuple hv_MatchModelID;
        HObject ho_MatchContours;
        bool m_MatchingEnabled;

        public Form1()
        {
            InitializeComponent();
            m_pDeviceList = new MyCamera.MV_CC_DEVICE_INFO_LIST();
            m_pMyCamera = new MyCamera();
            m_bGrabbing = false;
            DeviceListAcq();

            hwindow = hSmartWindowControl1.HalconWindow;
            hSmartWindowControl1.MouseWheel += HSmartWindow_MouseWheel;

            hv_MatchModelID = new HTuple();
            ho_MatchContours = new HObject();
            HOperatorSet.GenEmptyObj(out ho_MatchContours);
            m_MatchingEnabled = false;
        }

        private void HSmartWindow_MouseWheel(object sender, MouseEventArgs e)
        {
            Point pt = this.Location;
            MouseEventArgs newe = new MouseEventArgs(e.Button, e.Clicks, e.X - pt.X, e.Y - pt.Y, e.Delta);
            hSmartWindowControl1.HSmartWindowControl_MouseWheel(sender, newe);
        }

        private void bnEnum_Click(object sender, EventArgs e)
        {
            DeviceListAcq();
        }

        private void DeviceListAcq()
        {
            System.GC.Collect();
            cbDeviceList.Items.Clear();
            int nRet = MyCamera.MV_CC_EnumDevices_NET(MyCamera.MV_GIGE_DEVICE | MyCamera.MV_USB_DEVICE, ref m_pDeviceList);
            if (MyCamera.MV_OK != nRet)
            {
                MessageBox.Show("枚举设备失败");
                return;
            }

            for (int i = 0; i < m_pDeviceList.nDeviceNum; i++)
            {
                MyCamera.MV_CC_DEVICE_INFO device = (MyCamera.MV_CC_DEVICE_INFO)Marshal.PtrToStructure(
                    m_pDeviceList.pDeviceInfo[i], typeof(MyCamera.MV_CC_DEVICE_INFO));

                if (device.nTLayerType == MyCamera.MV_GIGE_DEVICE)
                {
                    IntPtr buffer = Marshal.UnsafeAddrOfPinnedArrayElement(device.SpecialInfo.stGigEInfo, 0);
                    MyCamera.MV_GIGE_DEVICE_INFO gigeInfo = (MyCamera.MV_GIGE_DEVICE_INFO)Marshal.PtrToStructure(
                        buffer, typeof(MyCamera.MV_GIGE_DEVICE_INFO));
                    if (gigeInfo.chUserDefinedName != "")
                        cbDeviceList.Items.Add("GigE: " + gigeInfo.chUserDefinedName + " (" + gigeInfo.chSerialNumber + ")");
                    else
                        cbDeviceList.Items.Add("GigE: " + gigeInfo.chManufacturerName + " " + gigeInfo.chModelName + " (" + gigeInfo.chSerialNumber + ")");
                }
                else if (device.nTLayerType == MyCamera.MV_USB_DEVICE)
                {
                    IntPtr buffer = Marshal.UnsafeAddrOfPinnedArrayElement(device.SpecialInfo.stUsb3VInfo, 0);
                    MyCamera.MV_USB3_DEVICE_INFO usbInfo = (MyCamera.MV_USB3_DEVICE_INFO)Marshal.PtrToStructure(
                        buffer, typeof(MyCamera.MV_USB3_DEVICE_INFO));
                    if (usbInfo.chUserDefinedName != "")
                        cbDeviceList.Items.Add("USB: " + usbInfo.chUserDefinedName + " (" + usbInfo.chSerialNumber + ")");
                    else
                        cbDeviceList.Items.Add("USB: " + usbInfo.chManufacturerName + " " + usbInfo.chModelName + " (" + usbInfo.chSerialNumber + ")");
                }
            }

            if (m_pDeviceList.nDeviceNum != 0)
                cbDeviceList.SelectedIndex = 0;
        }

        private void bnOpen_Click(object sender, EventArgs e)
        {
            if (m_pDeviceList.nDeviceNum == 0 || cbDeviceList.SelectedIndex == -1)
            {
                MessageBox.Show("没有设备，请先查找设备");
                return;
            }

            MyCamera.MV_CC_DEVICE_INFO device =
                (MyCamera.MV_CC_DEVICE_INFO)Marshal.PtrToStructure(
                    m_pDeviceList.pDeviceInfo[cbDeviceList.SelectedIndex],
                    typeof(MyCamera.MV_CC_DEVICE_INFO));

            int nRet = m_pMyCamera.MV_CC_CreateDevice_NET(ref device);
            if (MyCamera.MV_OK != nRet)
            {
                MessageBox.Show("创建设备失败");
                return;
            }

            nRet = m_pMyCamera.MV_CC_OpenDevice_NET();
            if (MyCamera.MV_OK != nRet)
            {
                MessageBox.Show("打开设备失败");
                return;
            }

            // 设置连续采集模式
            m_pMyCamera.MV_CC_SetEnumValue_NET("AcquisitionMode", 2);
            m_pMyCamera.MV_CC_SetEnumValue_NET("TriggerMode", 0);

            SetCtrlWhenOpen();
        }

        private void SetCtrlWhenOpen()
        {
            bnOpen.Enabled = false;
            bnClose.Enabled = true;
            bnStartGrab.Enabled = true;
            bnStopGrab.Enabled = false;
            tbExposure.Enabled = true;
            tbGain.Enabled = true;
            bnGetParam.Enabled = true;
            bnSetParam.Enabled = true;
            bnTemplate.Enabled = true;
        }

        private void SetCtrlWhenClose()
        {
            bnOpen.Enabled = true;
            bnClose.Enabled = false;
            bnStartGrab.Enabled = false;
            bnStopGrab.Enabled = false;
            tbExposure.Enabled = false;
            tbGain.Enabled = false;
            bnGetParam.Enabled = false;
            bnSetParam.Enabled = false;
            bnTemplate.Enabled = false;
            if (pbTemplate.Image != null)
            {
                pbTemplate.Image.Dispose();
                pbTemplate.Image = null;
            }
            pbTemplate.Visible = false;
        }

        private void bnClose_Click(object sender, EventArgs e)
        {
            if (m_bGrabbing)
            {
                m_bGrabbing = false;
                m_pMyCamera.MV_CC_StopGrabbing_NET();
                SetCtrlWhenStopGrab();
            }

            m_pMyCamera.MV_CC_CloseDevice_NET();
            SetCtrlWhenClose();
            m_bGrabbing = false;
        }

        private void SetCtrlWhenStartGrab()
        {
            bnStartGrab.Enabled = false;
            bnStopGrab.Enabled = true;
        }

        private void SetCtrlWhenStopGrab()
        {
            bnStartGrab.Enabled = true;
            bnStopGrab.Enabled = false;
        }

        private bool IsMonoPixelFormat(MyCamera.MvGvspPixelType enType)
        {
            switch (enType)
            {
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_Mono8:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_Mono10:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_Mono10_Packed:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_Mono12:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_Mono12_Packed:
                    return true;
                default:
                    return false;
            }
        }

        private bool IsColorPixelFormat(MyCamera.MvGvspPixelType enType)
        {
            switch (enType)
            {
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_RGB8_Packed:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BGR8_Packed:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_RGBA8_Packed:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BGRA8_Packed:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_YUV422_Packed:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_YUV422_YUYV_Packed:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerGR8:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerRG8:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerGB8:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerBG8:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerGB10:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerGB10_Packed:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerBG10:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerBG10_Packed:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerRG10:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerRG10_Packed:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerGR10:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerGR10_Packed:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerGB12:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerGB12_Packed:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerBG12:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerBG12_Packed:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerRG12:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerRG12_Packed:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerGR12:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerGR12_Packed:
                    return true;
                default:
                    return false;
            }
        }

        public void adapt_window(int width, int height)
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

        HObject Hobj = new HImage();

        public void ReceiveImageWorkThread(object obj)
        {
            MyCamera device = obj as MyCamera;
            pImageBuf = IntPtr.Zero;
            int nImageBufSize = 0;
            IntPtr pTemp;

            while (m_bGrabbing)
            {
                int nRet = device.MV_CC_GetImageBuffer_NET(ref stFrameOut, 1000);
                if (MyCamera.MV_OK == nRet)
                {
                    if (IsColorPixelFormat(stFrameOut.stFrameInfo.enPixelType))
                    {
                        if (stFrameOut.stFrameInfo.enPixelType == MyCamera.MvGvspPixelType.PixelType_Gvsp_RGB8_Packed)
                        {
                            pTemp = stFrameOut.pBufAddr;
                        }
                        else
                        {
                            if (IntPtr.Zero == pImageBuf || nImageBufSize < (stFrameOut.stFrameInfo.nWidth * stFrameOut.stFrameInfo.nHeight * 3))
                            {
                                if (pImageBuf != IntPtr.Zero)
                                    Marshal.FreeHGlobal(pImageBuf);
                                pImageBuf = Marshal.AllocHGlobal((int)stFrameOut.stFrameInfo.nWidth * stFrameOut.stFrameInfo.nHeight * 3);
                                if (IntPtr.Zero == pImageBuf)
                                    break;
                                nImageBufSize = stFrameOut.stFrameInfo.nWidth * stFrameOut.stFrameInfo.nHeight * 3;
                            }

                            MyCamera.MV_PIXEL_CONVERT_PARAM stPixelConvertParam = new MyCamera.MV_PIXEL_CONVERT_PARAM();
                            stPixelConvertParam.pSrcData = stFrameOut.pBufAddr;
                            stPixelConvertParam.nWidth = stFrameOut.stFrameInfo.nWidth;
                            stPixelConvertParam.nHeight = stFrameOut.stFrameInfo.nHeight;
                            stPixelConvertParam.enSrcPixelType = stFrameOut.stFrameInfo.enPixelType;
                            stPixelConvertParam.nSrcDataLen = stFrameOut.stFrameInfo.nFrameLen;
                            stPixelConvertParam.nDstBufferSize = (uint)nImageBufSize;
                            stPixelConvertParam.pDstBuffer = pImageBuf;
                            stPixelConvertParam.enDstPixelType = MyCamera.MvGvspPixelType.PixelType_Gvsp_RGB8_Packed;
                            nRet = device.MV_CC_ConvertPixelType_NET(ref stPixelConvertParam);
                            if (MyCamera.MV_OK != nRet)
                                break;
                            pTemp = pImageBuf;
                        }

                        try
                        {
                            HOperatorSet.GenImageInterleaved(out Hobj, (HTuple)pTemp, (HTuple)"rgb",
                                (HTuple)stFrameOut.stFrameInfo.nWidth, (HTuple)stFrameOut.stFrameInfo.nHeight,
                                -1, "byte", 0, 0, 0, 0, -1, 0);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.ToString());
                            break;
                        }
                    }
                    else if (IsMonoPixelFormat(stFrameOut.stFrameInfo.enPixelType))
                    {
                        if (stFrameOut.stFrameInfo.enPixelType == MyCamera.MvGvspPixelType.PixelType_Gvsp_Mono8)
                        {
                            pTemp = stFrameOut.pBufAddr;
                        }
                        else
                        {
                            if (IntPtr.Zero == pImageBuf || nImageBufSize < (stFrameOut.stFrameInfo.nWidth * stFrameOut.stFrameInfo.nHeight))
                            {
                                if (pImageBuf != IntPtr.Zero)
                                    Marshal.FreeHGlobal(pImageBuf);
                                pImageBuf = Marshal.AllocHGlobal((int)stFrameOut.stFrameInfo.nWidth * stFrameOut.stFrameInfo.nHeight);
                                if (IntPtr.Zero == pImageBuf)
                                    break;
                                nImageBufSize = stFrameOut.stFrameInfo.nWidth * stFrameOut.stFrameInfo.nHeight;
                            }

                            MyCamera.MV_PIXEL_CONVERT_PARAM stPixelConvertParam = new MyCamera.MV_PIXEL_CONVERT_PARAM();
                            stPixelConvertParam.pSrcData = stFrameOut.pBufAddr;
                            stPixelConvertParam.nWidth = stFrameOut.stFrameInfo.nWidth;
                            stPixelConvertParam.nHeight = stFrameOut.stFrameInfo.nHeight;
                            stPixelConvertParam.enSrcPixelType = stFrameOut.stFrameInfo.enPixelType;
                            stPixelConvertParam.nSrcDataLen = stFrameOut.stFrameInfo.nFrameLen;
                            stPixelConvertParam.nDstBufferSize = (uint)nImageBufSize;
                            stPixelConvertParam.pDstBuffer = pImageBuf;
                            stPixelConvertParam.enDstPixelType = MyCamera.MvGvspPixelType.PixelType_Gvsp_Mono8;
                            nRet = device.MV_CC_ConvertPixelType_NET(ref stPixelConvertParam);
                            if (MyCamera.MV_OK != nRet)
                                break;
                            pTemp = pImageBuf;
                        }
                        try
                        {
                            HOperatorSet.GenImage1Extern(out Hobj, "byte", stFrameOut.stFrameInfo.nWidth,
                                stFrameOut.stFrameInfo.nHeight, pTemp, IntPtr.Zero);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.ToString());
                            break;
                        }
                    }
                    else
                    {
                        device.MV_CC_FreeImageBuffer_NET(ref stFrameOut);
                        continue;
                    }

                    // 显示图像到Halcon窗口
                    try
                    {
                        adapt_window(stFrameOut.stFrameInfo.nWidth, stFrameOut.stFrameInfo.nHeight);
                        hwindow.ClearWindow();
                        HOperatorSet.DispObj(Hobj, hwindow);

                        if (m_MatchingEnabled && hv_MatchModelID.Length > 0)
                        {
                            try
                            {
                                HTuple hv_Row, hv_Col, hv_Angle, hv_Scale, hv_Score;
                                HOperatorSet.FindScaledShapeModel(Hobj, hv_MatchModelID,
                                    (new HTuple(0)).TupleRad(), (new HTuple(360)).TupleRad(),
                                    0.6, 1.4, 0.5, 1, 0.5, "least_squares",
                                    0, 0.9, out hv_Row, out hv_Col, out hv_Angle, out hv_Scale, out hv_Score);

                                if (hv_Score.Length > 0)
                                {
                                    HOperatorSet.SetColor(hwindow, "yellow");
                                    HOperatorSet.SetLineWidth(hwindow, 2);
                                    for (int i = 0; i < hv_Score.Length; i++)
                                    {
                                        HTuple hv_HomMat2D;
                                        HOperatorSet.VectorAngleToRigid(0, 0, 0,
                                            hv_Row[i], hv_Col[i], hv_Angle[i], out hv_HomMat2D);
                                        HOperatorSet.HomMat2dScale(hv_HomMat2D,
                                            hv_Scale[i], hv_Scale[i], hv_Row[i], hv_Col[i], out hv_HomMat2D);
                                        HObject ho_TransContours;
                                        HOperatorSet.AffineTransContourXld(ho_MatchContours,
                                            out ho_TransContours, hv_HomMat2D);
                                        HOperatorSet.DispObj(ho_TransContours, hwindow);
                                        ho_TransContours.Dispose();
                                    }
                                }
                            }
                            catch { }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.ToString());
                    }

                    device.MV_CC_FreeImageBuffer_NET(ref stFrameOut);
                }
            }

            if (pImageBuf != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(pImageBuf);
                pImageBuf = IntPtr.Zero;
            }
        }

        private void bnStartGrab_Click(object sender, EventArgs e)
        {
            int nRet = m_pMyCamera.MV_CC_StartGrabbing_NET();
            if (MyCamera.MV_OK != nRet)
            {
                MessageBox.Show("开始采集失败");
                return;
            }
            m_bGrabbing = true;

            Thread hReceiveImageThreadHandle = new Thread(ReceiveImageWorkThread);
            hReceiveImageThreadHandle.IsBackground = true;
            hReceiveImageThreadHandle.Start(m_pMyCamera);

            SetCtrlWhenStartGrab();
        }

        private void bnStopGrab_Click(object sender, EventArgs e)
        {
            int nRet = m_pMyCamera.MV_CC_StopGrabbing_NET();
            if (nRet != MyCamera.MV_OK)
            {
                MessageBox.Show("停止采集失败");
            }
            m_bGrabbing = false;
            SetCtrlWhenStopGrab();
        }

        private void bnSetParam_Click(object sender, EventArgs e)
        {
            try
            {
                float.Parse(tbExposure.Text);
                float.Parse(tbGain.Text);
            }
            catch
            {
                MessageBox.Show("请输入正确的数值");
                return;
            }
            m_pMyCamera.MV_CC_SetFloatValue_NET("ExposureTime", float.Parse(tbExposure.Text));
            m_pMyCamera.MV_CC_SetFloatValue_NET("Gain", float.Parse(tbGain.Text));
        }

        private void bnGetParam_Click(object sender, EventArgs e)
        {
            MyCamera.MVCC_FLOATVALUE x = new MyCamera.MVCC_FLOATVALUE();
            MyCamera.MVCC_FLOATVALUE y = new MyCamera.MVCC_FLOATVALUE();
            m_pMyCamera.MV_CC_GetFloatValue_NET("ExposureTime", ref x);
            tbExposure.Text = x.fCurValue.ToString("F1");
            m_pMyCamera.MV_CC_GetFloatValue_NET("Gain", ref y);
            tbGain.Text = y.fCurValue.ToString("F1");
        }

        public HObject GetCurrentImage()
        {
            if (Hobj != null && Hobj.IsInitialized())
                return Hobj.Clone();
            return null;
        }

        public void SetTemplateThumbnail(System.Drawing.Bitmap bmp)
        {
            if (pbTemplate.Image != null)
                pbTemplate.Image.Dispose();
            pbTemplate.Image = bmp;
            pbTemplate.Visible = true;
        }

        public void LoadMatchModel(string filePath)
        {
            try
            {
                if (hv_MatchModelID.Length > 0)
                    HOperatorSet.ClearShapeModel(hv_MatchModelID);
                HOperatorSet.ReadShapeModel(filePath, out hv_MatchModelID);
                ho_MatchContours.Dispose();
                HOperatorSet.GetShapeModelContours(out ho_MatchContours, hv_MatchModelID, 1);
                m_MatchingEnabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("加载匹配模板失败: " + ex.Message);
            }
        }

        private void bnTemplate_Click(object sender, EventArgs e)
        {
            FormTemplate formTemplate = new FormTemplate(this);
            formTemplate.Show();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            bnClose_Click(sender, e);
            if (hv_MatchModelID.Length > 0)
                HOperatorSet.ClearShapeModel(hv_MatchModelID);
            ho_MatchContours.Dispose();
        }
    }
}
