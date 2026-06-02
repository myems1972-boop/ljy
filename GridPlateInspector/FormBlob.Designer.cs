using System;
using System.Windows.Forms;
using HalconDotNet;

namespace GridPlateInspector
{
    partial class FormBlob : Form
    {
        private System.ComponentModel.IContainer components = null;
        private HSmartWindowControl hSmartWindowControl1;
        private Button bnGrabImage;
        private Button bnSaveImage;
        private Button bnBlobAnalyze;
        private Button bnEncode;
        private Button bnMatch;
        private GroupBox groupBox1;

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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormBlob));
            this.hSmartWindowControl1 = new HalconDotNet.HSmartWindowControl();
            this.bnGrabImage = new System.Windows.Forms.Button();
            this.bnSaveImage = new System.Windows.Forms.Button();
            this.bnBlobAnalyze = new System.Windows.Forms.Button();
            this.bnEncode = new System.Windows.Forms.Button();
            this.bnMatch = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
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
            // bnSaveImage
            //
            resources.ApplyResources(this.bnSaveImage, "bnSaveImage");
            this.bnSaveImage.Name = "bnSaveImage";
            this.bnSaveImage.UseVisualStyleBackColor = true;
            this.bnSaveImage.Click += new System.EventHandler(this.bnSaveImage_Click);
            //
            // bnBlobAnalyze
            //
            resources.ApplyResources(this.bnBlobAnalyze, "bnBlobAnalyze");
            this.bnBlobAnalyze.Name = "bnBlobAnalyze";
            this.bnBlobAnalyze.UseVisualStyleBackColor = true;
            this.bnBlobAnalyze.Click += new System.EventHandler(this.bnBlobAnalyze_Click);
            //
            // bnEncode
            //
            resources.ApplyResources(this.bnEncode, "bnEncode");
            this.bnEncode.Name = "bnEncode";
            this.bnEncode.UseVisualStyleBackColor = true;
            this.bnEncode.Click += new System.EventHandler(this.bnEncode_Click);
            //
            // bnMatch
            //
            resources.ApplyResources(this.bnMatch, "bnMatch");
            this.bnMatch.Name = "bnMatch";
            this.bnMatch.UseVisualStyleBackColor = true;
            this.bnMatch.Click += new System.EventHandler(this.bnMatch_Click);
            //
            // groupBox1
            //
            this.groupBox1.Controls.Add(this.bnBlobAnalyze);
            this.groupBox1.Controls.Add(this.bnMatch);
            this.groupBox1.Controls.Add(this.bnEncode);
            this.groupBox1.Controls.Add(this.bnSaveImage);
            this.groupBox1.Controls.Add(this.bnGrabImage);
            resources.ApplyResources(this.groupBox1, "groupBox1");
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.TabStop = false;
            //
            // FormBlob
            //
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.hSmartWindowControl1);
            this.Controls.Add(this.groupBox1);
            this.Name = "FormBlob";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FormBlob_FormClosing);
            this.groupBox1.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
