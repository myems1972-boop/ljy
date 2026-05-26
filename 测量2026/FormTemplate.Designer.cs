using System;
using System.Windows.Forms;
using HalconDotNet;

namespace 测量2026
{
    partial class FormTemplate : Form
    {
        private System.ComponentModel.IContainer components = null;
        private HSmartWindowControl hSmartWindowControl1;
        private Button bnGrabImage;
        private Button bnDrawROI;
        private Button bnCreateModel;
        private Button bnSaveModel;
        private GroupBox groupBox1;
        private Label label1;

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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormTemplate));
            this.hSmartWindowControl1 = new HalconDotNet.HSmartWindowControl();
            this.bnGrabImage = new System.Windows.Forms.Button();
            this.bnDrawROI = new System.Windows.Forms.Button();
            this.bnCreateModel = new System.Windows.Forms.Button();
            this.bnSaveModel = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label1 = new System.Windows.Forms.Label();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
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
            this.hSmartWindowControl1.WindowSize = new System.Drawing.Size(640, 480);
            //
            // bnGrabImage
            //
            resources.ApplyResources(this.bnGrabImage, "bnGrabImage");
            this.bnGrabImage.Name = "bnGrabImage";
            this.bnGrabImage.UseVisualStyleBackColor = true;
            this.bnGrabImage.Click += new System.EventHandler(this.bnGrabImage_Click);
            //
            // bnDrawROI
            //
            resources.ApplyResources(this.bnDrawROI, "bnDrawROI");
            this.bnDrawROI.Name = "bnDrawROI";
            this.bnDrawROI.UseVisualStyleBackColor = true;
            this.bnDrawROI.Click += new System.EventHandler(this.bnDrawROI_Click);
            //
            // bnCreateModel
            //
            resources.ApplyResources(this.bnCreateModel, "bnCreateModel");
            this.bnCreateModel.Name = "bnCreateModel";
            this.bnCreateModel.UseVisualStyleBackColor = true;
            this.bnCreateModel.Click += new System.EventHandler(this.bnCreateModel_Click);
            //
            // bnSaveModel
            //
            resources.ApplyResources(this.bnSaveModel, "bnSaveModel");
            this.bnSaveModel.Name = "bnSaveModel";
            this.bnSaveModel.UseVisualStyleBackColor = true;
            this.bnSaveModel.Click += new System.EventHandler(this.bnSaveModel_Click);
            //
            // groupBox1
            //
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.bnGrabImage);
            this.groupBox1.Controls.Add(this.bnDrawROI);
            this.groupBox1.Controls.Add(this.bnCreateModel);
            this.groupBox1.Controls.Add(this.bnSaveModel);
            resources.ApplyResources(this.groupBox1, "groupBox1");
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.TabStop = false;
            //
            // label1
            //
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            //
            // FormTemplate
            //
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.hSmartWindowControl1);
            this.Controls.Add(this.groupBox1);
            this.Name = "FormTemplate";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FormTemplate_FormClosing);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
        }
    }
}
