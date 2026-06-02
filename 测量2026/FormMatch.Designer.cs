using System;
using System.Windows.Forms;

namespace 测量2026
{
    partial class FormMatch : Form
    {
        private System.ComponentModel.IContainer components = null;
        private TextBox tbEncoding;
        private TextBox tbConnStr;
        private Button bnLoadCAD;
        private Button bnMatch;
        private Label lbStatus;
        private Label lbBest;
        private DataGridView dgvResults;
        private RadioButton rbFile;
        private RadioButton rbDb;
        private Button bnImportDwg;
        private GroupBox gbEncoding;
        private GroupBox gbResults;

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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormMatch));
            this.tbEncoding = new System.Windows.Forms.TextBox();
            this.bnLoadCAD = new System.Windows.Forms.Button();
            this.bnImportDwg = new System.Windows.Forms.Button();
            this.bnMatch = new System.Windows.Forms.Button();
            this.tbConnStr = new System.Windows.Forms.TextBox();
            this.rbFile = new System.Windows.Forms.RadioButton();
            this.rbDb = new System.Windows.Forms.RadioButton();
            this.lbStatus = new System.Windows.Forms.Label();
            this.lbBest = new System.Windows.Forms.Label();
            this.dgvResults = new System.Windows.Forms.DataGridView();
            this.gbEncoding = new System.Windows.Forms.GroupBox();
            this.gbResults = new System.Windows.Forms.GroupBox();
            ((System.ComponentModel.ISupportInitialize)(this.dgvResults)).BeginInit();
            this.gbEncoding.SuspendLayout();
            this.gbResults.SuspendLayout();
            this.SuspendLayout();
            //
            // tbEncoding
            //
            resources.ApplyResources(this.tbEncoding, "tbEncoding");
            this.tbEncoding.BackColor = System.Drawing.Color.FromArgb(30, 30, 30);
            this.tbEncoding.ForeColor = System.Drawing.Color.LightGreen;
            this.tbEncoding.Name = "tbEncoding";
            this.tbEncoding.ReadOnly = true;
            //
            // bnLoadCAD
            //
            resources.ApplyResources(this.bnLoadCAD, "bnLoadCAD");
            this.bnLoadCAD.Name = "bnLoadCAD";
            this.bnLoadCAD.UseVisualStyleBackColor = true;
            this.bnLoadCAD.Click += new System.EventHandler(this.bnLoadCAD_Click);
            //
            // bnImportDwg
            //
            resources.ApplyResources(this.bnImportDwg, "bnImportDwg");
            this.bnImportDwg.Name = "bnImportDwg";
            this.bnImportDwg.UseVisualStyleBackColor = true;
            this.bnImportDwg.Click += new System.EventHandler(this.bnImportDwg_Click);
            //
            // bnMatch
            //
            resources.ApplyResources(this.bnMatch, "bnMatch");
            this.bnMatch.Name = "bnMatch";
            this.bnMatch.UseVisualStyleBackColor = true;
            this.bnMatch.Click += new System.EventHandler(this.bnMatch_Click);
            //
            // tbConnStr
            //
            resources.ApplyResources(this.tbConnStr, "tbConnStr");
            this.tbConnStr.Name = "tbConnStr";
            //
            // rbFile
            //
            resources.ApplyResources(this.rbFile, "rbFile");
            this.rbFile.Name = "rbFile";
            this.rbFile.Checked = true;
            this.rbFile.CheckedChanged += new System.EventHandler(this.rbMode_CheckedChanged);
            //
            // rbDb
            //
            resources.ApplyResources(this.rbDb, "rbDb");
            this.rbDb.Name = "rbDb";
            this.rbDb.CheckedChanged += new System.EventHandler(this.rbMode_CheckedChanged);
            //
            // lbStatus
            //
            resources.ApplyResources(this.lbStatus, "lbStatus");
            this.lbStatus.Name = "lbStatus";
            //
            // lbBest
            //
            resources.ApplyResources(this.lbBest, "lbBest");
            this.lbBest.Name = "lbBest";
            //
            // dgvResults
            //
            resources.ApplyResources(this.dgvResults, "dgvResults");
            this.dgvResults.AllowUserToAddRows = false;
            this.dgvResults.AllowUserToDeleteRows = false;
            this.dgvResults.AllowUserToResizeRows = false;
            this.dgvResults.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvResults.Name = "dgvResults";
            this.dgvResults.ReadOnly = true;
            this.dgvResults.RowHeadersVisible = false;
            this.dgvResults.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvResults.MultiSelect = false;
            this.dgvResults.SelectionChanged += new System.EventHandler(this.dgvResults_SelectionChanged);
            //
            // gbEncoding
            //
            this.gbEncoding.Controls.Add(this.tbEncoding);
            resources.ApplyResources(this.gbEncoding, "gbEncoding");
            this.gbEncoding.Name = "gbEncoding";
            this.gbEncoding.TabStop = false;
            //
            // gbResults
            //
            this.gbResults.Controls.Add(this.dgvResults);
            this.gbResults.Controls.Add(this.tbConnStr);
            this.gbResults.Controls.Add(this.rbDb);
            this.gbResults.Controls.Add(this.rbFile);
            this.gbResults.Controls.Add(this.bnLoadCAD);
            this.gbResults.Controls.Add(this.bnImportDwg);
            this.gbResults.Controls.Add(this.bnMatch);
            this.gbResults.Controls.Add(this.lbStatus);
            resources.ApplyResources(this.gbResults, "gbResults");
            this.gbResults.Name = "gbResults";
            this.gbResults.TabStop = false;
            //
            // FormMatch
            //
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.gbEncoding);
            this.Controls.Add(this.gbResults);
            this.Controls.Add(this.lbBest);
            this.Name = "FormMatch";
            ((System.ComponentModel.ISupportInitialize)(this.dgvResults)).EndInit();
            this.gbEncoding.ResumeLayout(false);
            this.gbEncoding.PerformLayout();
            this.gbResults.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
