using System;
using HalconDotNet;
using System.Windows.Forms;

namespace GridPlateInspector
{
    partial class Form1 : Form
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.cbDeviceList = new System.Windows.Forms.ComboBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.bnClose = new System.Windows.Forms.Button();
            this.bnOpen = new System.Windows.Forms.Button();
            this.bnEnum = new System.Windows.Forms.Button();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.bnStopGrab = new System.Windows.Forms.Button();
            this.bnStartGrab = new System.Windows.Forms.Button();
            this.bnTemplate = new System.Windows.Forms.Button();
            this.bnBlob = new System.Windows.Forms.Button();
            this.bnBlobLive = new System.Windows.Forms.Button();
            this.pbTemplate = new System.Windows.Forms.PictureBox();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.bnGetParam = new System.Windows.Forms.Button();
            this.bnSetParam = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.tbGain = new System.Windows.Forms.TextBox();
            this.tbExposure = new System.Windows.Forms.TextBox();
            this.hSmartWindowControl1 = new HalconDotNet.HSmartWindowControl();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.SuspendLayout();
            // 
            // cbDeviceList
            // 
            this.cbDeviceList.FormattingEnabled = true;
            resources.ApplyResources(this.cbDeviceList, "cbDeviceList");
            this.cbDeviceList.Name = "cbDeviceList";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.bnClose);
            this.groupBox1.Controls.Add(this.bnOpen);
            this.groupBox1.Controls.Add(this.bnEnum);
            resources.ApplyResources(this.groupBox1, "groupBox1");
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.TabStop = false;
            // 
            // bnClose
            // 
            resources.ApplyResources(this.bnClose, "bnClose");
            this.bnClose.Name = "bnClose";
            this.bnClose.UseVisualStyleBackColor = true;
            this.bnClose.Click += new System.EventHandler(this.bnClose_Click);
            // 
            // bnOpen
            // 
            resources.ApplyResources(this.bnOpen, "bnOpen");
            this.bnOpen.Name = "bnOpen";
            this.bnOpen.UseVisualStyleBackColor = true;
            this.bnOpen.Click += new System.EventHandler(this.bnOpen_Click);
            // 
            // bnEnum
            // 
            resources.ApplyResources(this.bnEnum, "bnEnum");
            this.bnEnum.Name = "bnEnum";
            this.bnEnum.UseVisualStyleBackColor = true;
            this.bnEnum.Click += new System.EventHandler(this.bnEnum_Click);
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.bnStopGrab);
            this.groupBox2.Controls.Add(this.bnStartGrab);
            resources.ApplyResources(this.groupBox2, "groupBox2");
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.TabStop = false;
            // 
            // bnStopGrab
            // 
            resources.ApplyResources(this.bnStopGrab, "bnStopGrab");
            this.bnStopGrab.Name = "bnStopGrab";
            this.bnStopGrab.UseVisualStyleBackColor = true;
            this.bnStopGrab.Click += new System.EventHandler(this.bnStopGrab_Click);
            // 
            // bnStartGrab
            // 
            resources.ApplyResources(this.bnStartGrab, "bnStartGrab");
            this.bnStartGrab.Name = "bnStartGrab";
            this.bnStartGrab.UseVisualStyleBackColor = true;
            this.bnStartGrab.Click += new System.EventHandler(this.bnStartGrab_Click);
            //
            // bnTemplate
            //
            resources.ApplyResources(this.bnTemplate, "bnTemplate");
            this.bnTemplate.Name = "bnTemplate";
            this.bnTemplate.UseVisualStyleBackColor = true;
            this.bnTemplate.Click += new System.EventHandler(this.bnTemplate_Click);
            //
            // pbTemplate
            //
            resources.ApplyResources(this.pbTemplate, "pbTemplate");
            this.pbTemplate.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pbTemplate.Name = "pbTemplate";
            this.pbTemplate.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pbTemplate.TabStop = false;
            this.pbTemplate.Visible = false;
            //
            // bnBlob
            //
            resources.ApplyResources(this.bnBlob, "bnBlob");
            this.bnBlob.Name = "bnBlob";
            this.bnBlob.UseVisualStyleBackColor = true;
            this.bnBlob.Click += new System.EventHandler(this.bnBlob_Click);
            //
            // bnBlobLive
            //
            resources.ApplyResources(this.bnBlobLive, "bnBlobLive");
            this.bnBlobLive.Name = "bnBlobLive";
            this.bnBlobLive.UseVisualStyleBackColor = true;
            this.bnBlobLive.Click += new System.EventHandler(this.bnBlobLive_Click);
            //
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.bnGetParam);
            this.groupBox3.Controls.Add(this.bnSetParam);
            this.groupBox3.Controls.Add(this.label2);
            this.groupBox3.Controls.Add(this.label1);
            this.groupBox3.Controls.Add(this.tbGain);
            this.groupBox3.Controls.Add(this.tbExposure);
            resources.ApplyResources(this.groupBox3, "groupBox3");
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.TabStop = false;
            // 
            // bnGetParam
            // 
            resources.ApplyResources(this.bnGetParam, "bnGetParam");
            this.bnGetParam.Name = "bnGetParam";
            this.bnGetParam.UseVisualStyleBackColor = true;
            this.bnGetParam.Click += new System.EventHandler(this.bnGetParam_Click);
            // 
            // bnSetParam
            // 
            resources.ApplyResources(this.bnSetParam, "bnSetParam");
            this.bnSetParam.Name = "bnSetParam";
            this.bnSetParam.UseVisualStyleBackColor = true;
            this.bnSetParam.Click += new System.EventHandler(this.bnSetParam_Click);
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // tbGain
            // 
            resources.ApplyResources(this.tbGain, "tbGain");
            this.tbGain.Name = "tbGain";
            // 
            // tbExposure
            // 
            resources.ApplyResources(this.tbExposure, "tbExposure");
            this.tbExposure.Name = "tbExposure";
            // 
            // hSmartWindowControl1
            // 
            resources.ApplyResources(this.hSmartWindowControl1, "hSmartWindowControl1");
            this.hSmartWindowControl1.AutoValidate = System.Windows.Forms.AutoValidate.EnableAllowFocusChange;
            this.hSmartWindowControl1.HDoubleClickToFitContent = true;
            this.hSmartWindowControl1.HDrawingObjectsModifier = HalconDotNet.HSmartWindowControl.DrawingObjectsModifier.None;
            this.hSmartWindowControl1.HImagePart = new System.Drawing.Rectangle(0, 0, 640, 480);
            this.hSmartWindowControl1.HKeepAspectRatio = true;
            this.hSmartWindowControl1.HMoveContent = true;
            this.hSmartWindowControl1.HZoomContent = HalconDotNet.HSmartWindowControl.ZoomContent.WheelForwardZoomsIn;
            this.hSmartWindowControl1.Name = "hSmartWindowControl1";
            this.hSmartWindowControl1.WindowSize = new System.Drawing.Size(720, 534);
            // 
            // Form1
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.bnTemplate);
            this.Controls.Add(this.bnBlob);
            this.Controls.Add(this.bnBlobLive);
            this.Controls.Add(this.pbTemplate);
            this.Controls.Add(this.hSmartWindowControl1);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.cbDeviceList);
            this.Name = "Form1";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.groupBox1.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.ResumeLayout(false);

        }

        private ComboBox cbDeviceList;
        private GroupBox groupBox1;
        private Button bnClose;
        private Button bnOpen;
        private Button bnEnum;
        private GroupBox groupBox2;
        private Button bnStopGrab;
        private Button bnStartGrab;
        private GroupBox groupBox3;
        private Label label2;
        private Label label1;
        private TextBox tbGain;
        private TextBox tbExposure;
        private Button bnSetParam;
        private Button bnGetParam;
        private HSmartWindowControl hSmartWindowControl1;
        private Button bnTemplate;
        private Button bnBlob;
        private Button bnBlobLive;
        private PictureBox pbTemplate;
    }
}
