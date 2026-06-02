using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace GridPlateInspector
{
    public partial class FormMatch : Form
    {
        FeatureEncoding m_ImageEncoding;
        List<FeatureEncoding> m_CadLibrary;
        List<MatchResult> m_LastResults;
        Matcher m_Matcher;
        CadRepository m_Repo;

        public FormMatch(FeatureEncoding imageEncoding)
        {
            InitializeComponent();
            m_ImageEncoding = imageEncoding;
            m_CadLibrary = new List<FeatureEncoding>();
            m_LastResults = new List<MatchResult>();
            m_Matcher = new Matcher();

            dgvResults.Columns.Add("Rank", "排名");
            dgvResults.Columns.Add("File", "文件名");
            dgvResults.Columns.Add("Score", "总分");
            dgvResults.Columns.Add("Pos", "位置");
            dgvResults.Columns.Add("Size", "尺寸");
            dgvResults.Columns.Add("Shape", "形状");
            dgvResults.Columns.Add("Matched", "配对");
            dgvResults.Columns.Add("Unmatched", "未配对");
            dgvResults.Columns.Add("Valid", "有效");
            dgvResults.Columns["Rank"].Width = 45;
            dgvResults.Columns["File"].Width = 130;
            dgvResults.Columns["Score"].Width = 65;
            dgvResults.Columns["Pos"].Width = 55;
            dgvResults.Columns["Size"].Width = 55;
            dgvResults.Columns["Shape"].Width = 55;
            dgvResults.Columns["Matched"].Width = 50;
            dgvResults.Columns["Unmatched"].Width = 55;
            dgvResults.Columns["Valid"].Width = 40;

            if (m_ImageEncoding != null)
                tbEncoding.Text = m_ImageEncoding.ToPrettyJson();

            // 读取默认连接字符串
            try
            {
                var connStrSettings = ConfigurationManager.ConnectionStrings["CadDb"];
                if (connStrSettings != null)
                    tbConnStr.Text = connStrSettings.ConnectionString;
            }
            catch { }

            tbConnStr.Visible = false;
            UpdateModeUI();
        }

        // ======================== 模式切换 ========================

        private void rbMode_CheckedChanged(object sender, EventArgs e)
        {
            UpdateModeUI();
        }

        void UpdateModeUI()
        {
            bool isDb = rbDb.Checked;
            tbConnStr.Visible = isDb;
            bnLoadCAD.Text = isDb ? "连接数据库" : "加载CAD库";
            lbStatus.Text = isDb ? "未连接" : "未加载";

            if (isDb)
            {
                if (m_Repo != null) { m_Repo.Dispose(); m_Repo = null; }
                m_CadLibrary.Clear();
            }
            else
            {
                if (m_Repo != null) { m_Repo.Dispose(); m_Repo = null; }
                m_CadLibrary.Clear();
            }
        }

        // ======================== 加载 / 连接 ========================

        private void bnLoadCAD_Click(object sender, EventArgs e)
        {
            if (rbDb.Checked)
                ConnectDatabase();
            else
                LoadFileLibrary();
        }

        void ConnectDatabase()
        {
            string connStr = tbConnStr.Text.Trim();
            if (string.IsNullOrEmpty(connStr))
            {
                MessageBox.Show("请输入数据库连接字符串");
                return;
            }

            try
            {
                if (m_Repo != null) m_Repo.Dispose();
                m_Repo = new CadRepository(connStr);

                if (!m_Repo.TestConnection())
                {
                    MessageBox.Show("数据库连接失败");
                    return;
                }

                m_Repo.CreateTablesIfNotExist();
                int count = m_Repo.GetDrawingCount();
                lbStatus.Text = string.Format("已连接，共 {0} 张图纸", count);
                bnLoadCAD.Text = "刷新计数";
            }
            catch (Exception ex)
            {
                MessageBox.Show("数据库连接失败: " + ex.Message);
            }
        }

        void LoadFileLibrary()
        {
            FolderBrowserDialog dlg = new FolderBrowserDialog();
            dlg.Description = "选择CAD编码文件夹";
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                m_CadLibrary = m_Matcher.LoadCADLibrary(dlg.SelectedPath);
                lbStatus.Text = string.Format("已加载 {0} 张图纸", m_CadLibrary.Count);
            }
        }

        // ======================== DWG导入 ========================

        private void bnImportDwg_Click(object sender, EventArgs e)
        {
            if (rbDb.Checked)
                ImportDwgToDatabase();
            else
                ImportDwgToLibrary();
        }

        void ImportDwgToLibrary()
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Title = "选择DWG图纸";
            dlg.Filter = "DWG文件|*.dwg";
            dlg.Multiselect = true;
            if (dlg.ShowDialog() != DialogResult.OK) return;

            int ok = 0, fail = 0;
            foreach (string path in dlg.FileNames)
            {
                try
                {
                    var enc = DwgExtractor.Extract(path);
                    enc.FilePath = path;
                    m_CadLibrary.Add(enc);
                    ok++;
                }
                catch
                {
                    fail++;
                }
            }
            lbStatus.Text = string.Format("已加载 {0} 张图纸 ({1} DWG + {2} JSON)",
                m_CadLibrary.Count, ok, m_CadLibrary.Count - ok);
            if (fail > 0)
                MessageBox.Show(string.Format("{0} 张导入成功，{1} 张失败", ok, fail));
        }

        void ImportDwgToDatabase()
        {
            if (m_Repo == null)
            {
                MessageBox.Show("请先连接数据库");
                return;
            }

            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Title = "选择DWG图纸导入数据库";
            dlg.Filter = "DWG文件|*.dwg";
            dlg.Multiselect = true;
            if (dlg.ShowDialog() != DialogResult.OK) return;

            int ok = 0, fail = 0;
            foreach (string path in dlg.FileNames)
            {
                try
                {
                    var enc = DwgExtractor.Extract(path);
                    string drawingNo = Path.GetFileNameWithoutExtension(path);
                    m_Repo.ImportEncoding(enc, drawingNo, path);
                    ok++;
                }
                catch
                {
                    fail++;
                }
            }

            int count = m_Repo.GetDrawingCount();
            lbStatus.Text = string.Format("已连接，共 {0} 张图纸", count);
            bnLoadCAD.Text = "刷新计数";
            MessageBox.Show(string.Format("{0} 张导入成功，{1} 张失败", ok, fail));
        }

        // ======================== 匹配 ========================

        private void bnMatch_Click(object sender, EventArgs e)
        {
            if (m_ImageEncoding == null)
            {
                MessageBox.Show("没有图像编码");
                return;
            }

            List<MatchResult> results;

            if (rbDb.Checked)
            {
                if (m_Repo == null)
                {
                    MessageBox.Show("请先连接数据库");
                    return;
                }
                results = m_Repo.MatchFromDb(m_ImageEncoding, m_Matcher);
            }
            else
            {
                if (m_CadLibrary.Count == 0)
                {
                    MessageBox.Show("请先加载CAD库");
                    return;
                }
                results = m_Matcher.Match(m_ImageEncoding, m_CadLibrary);
            }

            ShowResults(results);
        }

        void ShowResults(List<MatchResult> results)
        {
            m_LastResults = results;
            dgvResults.Rows.Clear();
            int rank = 0;
            foreach (var r in results)
            {
                rank++;
                bool valid = r.IsValid;
                dgvResults.Rows.Add(
                    rank,
                    Path.GetFileName(r.FileName),
                    valid ? r.Score.ToString("F4") : "—",
                    valid ? r.PosCost.ToString("F4") : "—",
                    valid ? r.SizeCost.ToString("F4") : "—",
                    valid ? r.ShapeCost.ToString("F4") : "—",
                    r.MatchedCount,
                    r.UnmatchedCount,
                    valid ? "✓" : "✗"
                );
                if (rank >= 50) break;
            }

            if (results.Count > 0 && results[0].IsValid)
            {
                var best = results[0];
                lbBest.Text = string.Format(
                    "最佳匹配: {0}\r\n分数: {1:F4} | 配对: {2} | 未配对: {3}\r\n位置代价: {4:F4} | 尺寸代价: {5:F4} | 形状代价: {6:F4}",
                    Path.GetFileName(best.FileName),
                    best.Score, best.MatchedCount, best.UnmatchedCount,
                    best.PosCost, best.SizeCost, best.ShapeCost);

                if (best.Score < m_Matcher.T_MATCH)
                    lbBest.Text += "\r\n✓ 匹配成功";
                else
                    lbBest.Text += "\r\n⚠ 分数高于阈值，可能无匹配";
            }
            else
            {
                lbBest.Text = "未找到有效匹配";
            }
        }

        // ======================== 结果浏览 ========================

        private void dgvResults_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvResults.SelectedRows.Count > 0)
            {
                int idx = dgvResults.SelectedRows[0].Index;
                if (idx < m_LastResults.Count)
                {
                    string fname = m_LastResults[idx].FileName;
                    var match = m_CadLibrary.Find(c => c.FilePath == fname);
                    if (match != null)
                        tbEncoding.Text = match.ToPrettyJson();
                }
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (m_Repo != null) m_Repo.Dispose();
            base.OnFormClosing(e);
        }
    }
}
