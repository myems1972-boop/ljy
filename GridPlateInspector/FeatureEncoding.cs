using System;
using System.Collections.Generic;
using System.Text;
using System.Web.Script.Serialization;

namespace GridPlateInspector
{
    public class FeatureEncoding
    {
        public double L { get; set; }
        public double W { get; set; }
        public List<FeaturePoint> features { get; set; }

        [ScriptIgnore]
        public string FilePath { get; set; }

        public FeatureEncoding()
        {
            features = new List<FeaturePoint>();
        }

        public static FeatureEncoding FromJson(string json)
        {
            JavaScriptSerializer ser = new JavaScriptSerializer();
            return ser.Deserialize<FeatureEncoding>(json);
        }

        public string ToJson()
        {
            JavaScriptSerializer ser = new JavaScriptSerializer();
            StringBuilder sb = new StringBuilder();
            ser.Serialize(this, sb);
            return sb.ToString();
        }

        public string ToPrettyJson()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("{\r\n");
            sb.AppendFormat("  \"L\": {0:F1},\r\n", L);
            sb.AppendFormat("  \"W\": {0:F1},\r\n", W);
            sb.Append("  \"features\": [\r\n");
            for (int i = 0; i < features.Count; i++)
            {
                var f = features[i];
                sb.Append("    {\r\n");
                sb.AppendFormat("      \"cx\": {0:F4},\r\n", f.cx);
                sb.AppendFormat("      \"cy\": {0:F4},\r\n", f.cy);
                sb.AppendFormat("      \"area\": {0:F1},\r\n", f.area);
                sb.AppendFormat("      \"circularity\": {0:F3},\r\n", f.circularity);
                sb.AppendFormat("      \"anisometry\": {0:F3},\r\n", f.anisometry);
                sb.AppendFormat("      \"bulkiness\": {0:F3},\r\n", f.bulkiness);
                sb.AppendFormat("      \"ra\": {0:F1},\r\n", f.ra);
                sb.AppendFormat("      \"rb\": {0:F1}\r\n", f.rb);
                sb.Append("    }");
                if (i < features.Count - 1) sb.Append(",");
                sb.Append("\r\n");
            }
            sb.Append("  ]\r\n");
            sb.Append("}");
            return sb.ToString();
        }
    }

    public class FeaturePoint
    {
        public double cx { get; set; }
        public double cy { get; set; }
        public double area { get; set; }
        public double circularity { get; set; }
        public double anisometry { get; set; }
        public double bulkiness { get; set; }
        public double ra { get; set; }
        public double rb { get; set; }
    }
}
