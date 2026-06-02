using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace GridPlateInspector
{
    public class CadRepository : IDisposable
    {
        string m_ConnStr;
        SqlConnection m_Conn;

        public CadRepository(string connectionString)
        {
            m_ConnStr = connectionString;
        }

        // ======================== 连接管理 ========================

        public bool TestConnection()
        {
            try
            {
                using (var conn = new SqlConnection(m_ConnStr))
                {
                    conn.Open();
                    return true;
                }
            }
            catch { return false; }
        }

        void EnsureOpen()
        {
            if (m_Conn == null)
            {
                m_Conn = new SqlConnection(m_ConnStr);
                m_Conn.Open();
            }
            else if (m_Conn.State != ConnectionState.Open)
            {
                m_Conn.Open();
            }
        }

        public void Dispose()
        {
            if (m_Conn != null)
            {
                m_Conn.Dispose();
                m_Conn = null;
            }
        }

        // ======================== 建表 ========================

        public void CreateTablesIfNotExist()
        {
            EnsureOpen();
            using (var cmd = m_Conn.CreateCommand())
            {
                cmd.CommandText = @"
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'cad_drawings')
BEGIN
    CREATE TABLE cad_drawings (
        id          INT IDENTITY PRIMARY KEY,
        drawing_no  NVARCHAR(64)  NOT NULL,
        file_path   NVARCHAR(512) NULL,
        L           FLOAT         NOT NULL,
        W           FLOAT         NOT NULL,
        feat_count  INT           NOT NULL,
        created_at  DATETIME2     DEFAULT GETDATE()
    );
    CREATE INDEX IX_cad_drawings_L ON cad_drawings(L);
    CREATE INDEX IX_cad_drawings_W ON cad_drawings(W);
    CREATE INDEX IX_cad_drawings_feat_count ON cad_drawings(feat_count);
END

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'cad_features')
BEGIN
    CREATE TABLE cad_features (
        id            INT IDENTITY PRIMARY KEY,
        drawing_id    INT           NOT NULL,
        seq           INT           NOT NULL,
        cx            FLOAT         NOT NULL,
        cy            FLOAT         NOT NULL,
        area          FLOAT         NOT NULL,
        circularity   FLOAT         NOT NULL,
        anisometry    FLOAT         NOT NULL,
        bulkiness     FLOAT         NOT NULL,
        ra            FLOAT         NOT NULL,
        rb            FLOAT         NOT NULL,
        FOREIGN KEY (drawing_id) REFERENCES cad_drawings(id) ON DELETE CASCADE
    );
    CREATE INDEX IX_cad_features_drawing ON cad_features(drawing_id);
END";
                cmd.ExecuteNonQuery();
            }
        }

        // ======================== 导入 ========================

        /// <summary>
        /// 单条导入编码（使用参数化SQL）
        /// </summary>
        public int ImportEncoding(FeatureEncoding enc, string drawingNo, string filePath = null)
        {
            EnsureOpen();
            using (var cmd = m_Conn.CreateCommand())
            {
                cmd.CommandText = @"
INSERT INTO cad_drawings (drawing_no, file_path, L, W, feat_count)
OUTPUT INSERTED.id
VALUES (@no, @fp, @L, @W, @fc)";
                cmd.Parameters.AddWithValue("@no", drawingNo);
                cmd.Parameters.AddWithValue("@fp", (object)filePath ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@L", enc.L);
                cmd.Parameters.AddWithValue("@W", enc.W);
                cmd.Parameters.AddWithValue("@fc", enc.features.Count);
                int drawingId = (int)cmd.ExecuteScalar();

                // 批量插入特征
                using (var bulk = new SqlBulkCopy(m_Conn, SqlBulkCopyOptions.Default, null))
                {
                    bulk.DestinationTableName = "cad_features";
                    bulk.ColumnMappings.Add("drawing_id", "drawing_id");
                    bulk.ColumnMappings.Add("seq", "seq");
                    bulk.ColumnMappings.Add("cx", "cx");
                    bulk.ColumnMappings.Add("cy", "cy");
                    bulk.ColumnMappings.Add("area", "area");
                    bulk.ColumnMappings.Add("circularity", "circularity");
                    bulk.ColumnMappings.Add("anisometry", "anisometry");
                    bulk.ColumnMappings.Add("bulkiness", "bulkiness");
                    bulk.ColumnMappings.Add("ra", "ra");
                    bulk.ColumnMappings.Add("rb", "rb");

                    DataTable dt = new DataTable();
                    dt.Columns.Add("drawing_id", typeof(int));
                    dt.Columns.Add("seq", typeof(int));
                    dt.Columns.Add("cx", typeof(double));
                    dt.Columns.Add("cy", typeof(double));
                    dt.Columns.Add("area", typeof(double));
                    dt.Columns.Add("circularity", typeof(double));
                    dt.Columns.Add("anisometry", typeof(double));
                    dt.Columns.Add("bulkiness", typeof(double));
                    dt.Columns.Add("ra", typeof(double));
                    dt.Columns.Add("rb", typeof(double));

                    for (int i = 0; i < enc.features.Count; i++)
                    {
                        var f = enc.features[i];
                        dt.Rows.Add(drawingId, i, f.cx, f.cy, f.area,
                            f.circularity, f.anisometry, f.bulkiness, f.ra, f.rb);
                    }
                    bulk.WriteToServer(dt);
                }
                return drawingId;
            }
        }

        /// <summary>
        /// 批量导入（事务包裹 + SqlBulkCopy 分批写 features）
        /// </summary>
        public int ImportBatch(List<Tuple<FeatureEncoding, string, string>> encodings)
        {
            // encodings: (encoding, drawing_no, file_path)
            EnsureOpen();
            int count = 0;
            using (var tx = m_Conn.BeginTransaction())
            {
                foreach (var item in encodings)
                {
                    var enc = item.Item1;
                    var no = item.Item2;
                    var fp = item.Item3;

                    int drawingId;
                    using (var cmd = m_Conn.CreateCommand())
                    {
                        cmd.Transaction = tx;
                        cmd.CommandText = @"
INSERT INTO cad_drawings (drawing_no, file_path, L, W, feat_count)
OUTPUT INSERTED.id
VALUES (@no, @fp, @L, @W, @fc)";
                        cmd.Parameters.AddWithValue("@no", no);
                        cmd.Parameters.AddWithValue("@fp", (object)fp ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@L", enc.L);
                        cmd.Parameters.AddWithValue("@W", enc.W);
                        cmd.Parameters.AddWithValue("@fc", enc.features.Count);
                        drawingId = (int)cmd.ExecuteScalar();
                    }

                    DataTable dt = BuildFeatureTable(drawingId, enc.features);
                    using (var bulk = new SqlBulkCopy(m_Conn, SqlBulkCopyOptions.Default, tx))
                    {
                        bulk.DestinationTableName = "cad_features";
                        foreach (DataColumn col in dt.Columns)
                            bulk.ColumnMappings.Add(col.ColumnName, col.ColumnName);
                        bulk.WriteToServer(dt);
                    }
                    count++;
                }
                tx.Commit();
            }
            return count;
        }

        DataTable BuildFeatureTable(int drawingId, List<FeaturePoint> features)
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("drawing_id", typeof(int));
            dt.Columns.Add("seq", typeof(int));
            dt.Columns.Add("cx", typeof(double));
            dt.Columns.Add("cy", typeof(double));
            dt.Columns.Add("area", typeof(double));
            dt.Columns.Add("circularity", typeof(double));
            dt.Columns.Add("anisometry", typeof(double));
            dt.Columns.Add("bulkiness", typeof(double));
            dt.Columns.Add("ra", typeof(double));
            dt.Columns.Add("rb", typeof(double));
            for (int i = 0; i < features.Count; i++)
            {
                var f = features[i];
                dt.Rows.Add(drawingId, i, f.cx, f.cy, f.area,
                    f.circularity, f.anisometry, f.bulkiness, f.ra, f.rb);
            }
            return dt;
        }

        // ======================== Stage 1+2: SQL 过滤 ========================

        /// <summary>
        /// Stage 1+2: L/W双向过滤 + 特征数量±1，返回候选 drawing_id 列表
        /// </summary>
        public List<int> FilterCandidates(double imgL, double imgW, int imgFeatCount)
        {
            EnsureOpen();
            var ids = new List<int>();
            double tolL = Math.Max(2.0, imgL * 0.005);
            double tolW = Math.Max(2.0, imgW * 0.005);

            using (var cmd = m_Conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT id FROM cad_drawings
WHERE L BETWEEN @L_lo AND @L_hi
  AND W BETWEEN @W_lo AND @W_hi
  AND feat_count BETWEEN @fc_lo AND @fc_hi";
                cmd.Parameters.AddWithValue("@L_lo", imgL - tolL);
                cmd.Parameters.AddWithValue("@L_hi", imgL + tolL);
                cmd.Parameters.AddWithValue("@W_lo", imgW - tolW);
                cmd.Parameters.AddWithValue("@W_hi", imgW + tolW);
                cmd.Parameters.AddWithValue("@fc_lo", imgFeatCount - 1);
                cmd.Parameters.AddWithValue("@fc_hi", imgFeatCount + 1);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        ids.Add(reader.GetInt32(0));
                }
            }
            return ids;
        }

        // ======================== Stage 3: 批量加载特征 ========================

        /// <summary>
        /// 根据 drawing_id 列表批量加载特征，返回 id → FeatureEncoding 字典
        /// </summary>
        public Dictionary<int, FeatureEncoding> LoadFeatures(List<int> drawingIds)
        {
            EnsureOpen();
            var result = new Dictionary<int, FeatureEncoding>();

            if (drawingIds.Count == 0)
                return result;

            // 先加载主表
            string inClause = string.Join(",", drawingIds.ConvertAll(id => id.ToString()));
            using (var cmd = m_Conn.CreateCommand())
            {
                cmd.CommandText = string.Format(
                    "SELECT id, drawing_no, file_path, L, W FROM cad_drawings WHERE id IN ({0})", inClause);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int id = reader.GetInt32(0);
                        var enc = new FeatureEncoding();
                        enc.L = reader.GetDouble(3);
                        enc.W = reader.GetDouble(4);
                        enc.FilePath = reader.IsDBNull(1) ? "" : reader.GetString(1);
                        result[id] = enc;
                    }
                }
            }

            // 加载特征明细
            using (var cmd = m_Conn.CreateCommand())
            {
                cmd.CommandText = string.Format(
                    "SELECT drawing_id, cx, cy, area, circularity, anisometry, bulkiness, ra, rb FROM cad_features WHERE drawing_id IN ({0}) ORDER BY drawing_id, seq", inClause);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int drawingId = reader.GetInt32(0);
                        if (result.ContainsKey(drawingId))
                        {
                            result[drawingId].features.Add(new FeaturePoint
                            {
                                cx = reader.GetDouble(1),
                                cy = reader.GetDouble(2),
                                area = reader.GetDouble(3),
                                circularity = reader.GetDouble(4),
                                anisometry = reader.GetDouble(5),
                                bulkiness = reader.GetDouble(6),
                                ra = reader.GetDouble(7),
                                rb = reader.GetDouble(8)
                            });
                        }
                    }
                }
            }

            return result;
        }

        // ======================== 完整匹配流水线（从DB） ========================

        /// <summary>
        /// 从数据库执行完整四级匹配
        /// </summary>
        public List<MatchResult> MatchFromDb(FeatureEncoding image, Matcher matcher)
        {
            // Stage 1+2
            List<int> ids = FilterCandidates(image.L, image.W, image.features.Count);
            if (ids.Count == 0)
                return new List<MatchResult>();

            // Stage 3: 加载特征
            Dictionary<int, FeatureEncoding> cadMap = LoadFeatures(ids);
            List<FeatureEncoding> cadLib = new List<FeatureEncoding>();
            foreach (var kv in cadMap)
            {
                kv.Value.FilePath = kv.Value.FilePath ?? ("DB:" + kv.Key);
                cadLib.Add(kv.Value);
            }

            // Stage 3+4: 匈牙利匹配
            return matcher.Match(image, cadLib);
        }

        /// <summary>
        /// 获取总图纸数
        /// </summary>
        public int GetDrawingCount()
        {
            EnsureOpen();
            using (var cmd = m_Conn.CreateCommand())
            {
                cmd.CommandText = "SELECT COUNT(*) FROM cad_drawings";
                return (int)cmd.ExecuteScalar();
            }
        }
    }
}
