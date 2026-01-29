using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace MyFolder._1._Scripts._0._System.Data
{
    public static class CsvToJson
    {
        public static string Convert(string csv, int headerLineIndex = 1)
        {
            if (string.IsNullOrEmpty(csv)) return "{\"data\":[]}";
            var norm = csv.Replace("\r\n", "\n").Replace("\r", "\n");
            var lines = norm.Split('\n');
            int headerIdx = Math.Max(0, headerLineIndex - 1);

            // Skip blank lines to find header
            while (headerIdx < lines.Length && string.IsNullOrWhiteSpace(lines[headerIdx]))
                headerIdx++;
            if (headerIdx >= lines.Length) return "{\"data\":[]}";

            var headers = Split(lines[headerIdx]);
            var rows = new List<Dictionary<string, object>>();

            for (int i = headerIdx + 1; i < lines.Length; i++)
            {
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;
                var cols = Split(line);
                var row = new Dictionary<string, object>(headers.Count, StringComparer.OrdinalIgnoreCase);
                bool any = false;
                for (int c = 0; c < Math.Min(headers.Count, cols.Count); c++)
                {
                    var key = headers[c];
                    if (string.IsNullOrWhiteSpace(key)) continue;
                    var raw = cols[c];
                    if (!string.IsNullOrWhiteSpace(raw)) any = true;
                    row[key] = raw;
                }
                if (any) rows.Add(row);
            }

            var wrapper = new { data = rows };
            return JsonConvert.SerializeObject(wrapper, Formatting.Indented);
        }

        // Simple CSV splitter with minimal quote handling
        private static List<string> Split(string line)
        {
            var res = new List<string>();
            var sb = new StringBuilder();
            bool quoted = false;
            for (int i = 0; i < line.Length; i++)
            {
                char ch = line[i];
                if (ch == '"') { quoted = !quoted; continue; }
                if (ch == ',' && !quoted)
                {
                    res.Add(sb.ToString());
                    sb.Length = 0;
                }
                else sb.Append(ch);
            }
            res.Add(sb.ToString());
            return res;
        }
    }
}
