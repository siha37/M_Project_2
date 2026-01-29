#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace MyFolder._1._Scripts._9000.Editor
{
    public static class GoogleSheetStructuredCsvToJsonEditor
    {
        [MenuItem("Tools/Data/Google Sheet CSV → JSON (Structured Headers)")]
        public static void ConvertMenu()
        {
            string csvUrl = EditorGUIUtility.systemCopyBuffer;
            if (string.IsNullOrWhiteSpace(csvUrl))
            {
                EditorUtility.DisplayDialog("안내", "CSV 공개 링크(URL)를 클립보드에 복사해 주세요.\n예: https://docs.google.com/spreadsheets/d/SPREADSHEET_ID/export?format=csv&gid=0", "확인");
                return;
            }

            string outJson = EditorUtility.SaveFilePanel("저장할 JSON 경로", Application.dataPath, "SheetData", "json");
            if (string.IsNullOrEmpty(outJson))
                return;

            try
            {
                string csv = DownloadText(csvUrl);
                char delimiter = DetectDelimiter(csv);
                int typesIdx, keysIdx, dataStartIdx;
                DetectHeaderRows(csv, delimiter, out typesIdx, out keysIdx, out dataStartIdx);

                string json = ConvertStructuredCsvToJson(csv, typesIdx, keysIdx, dataStartIdx, delimiter);
                File.WriteAllText(outJson, json, new UTF8Encoding(false));
                AssetDatabase.Refresh();
                Debug.Log($"CSV → JSON 완료: {outJson}\n구분자:'{delimiter}', types:{typesIdx}, keys:{keysIdx}, dataStart:{dataStartIdx}, 길이:{json.Length}");
            }
            catch (Exception e)
            {
                Debug.LogError($"변환 실패: {e.Message}");
            }
        }

        public static string ConvertStructuredCsvToJson(string csvText, int typesRowIndex, int keysRowIndex, int dataStartRowIndex, char delimiter = ',')
        {
            List<string> lines = ReadAllLines(csvText);
            int minNeeded = Math.Max(typesRowIndex, Math.Max(keysRowIndex, dataStartRowIndex - 1));
            if (lines.Count <= minNeeded)
                return "[]";

            List<string> typeCells = typesRowIndex >= 0 ? SplitCsvLine(lines[typesRowIndex], delimiter) : new List<string>();
            List<string> keyCells = SplitCsvLine(lines[keysRowIndex], delimiter);

            int colCount = Math.Max(typeCells.Count, keyCells.Count);
            string[] colTypes = new string[colCount];
            string[] colKeys = new string[colCount];

            for (int c = 0; c < colCount; c++)
            {
                string t = (typesRowIndex >= 0 && c < typeCells.Count) ? typeCells[c] : string.Empty;
                string k = c < keyCells.Count ? keyCells[c] : string.Empty;
                colTypes[c] = SanitizeType(t);
                colKeys[c] = string.IsNullOrWhiteSpace(k) ? null : k.Trim();
            }

            List<Dictionary<string, object>> rows = new List<Dictionary<string, object>>(Mathf.Max(0, lines.Count - dataStartRowIndex));
            for (int r = dataStartRowIndex; r < lines.Count; r++)
            {
                List<string> cols = SplitCsvLine(lines[r], delimiter);
                bool allEmpty = true;
                Dictionary<string, object> obj = new Dictionary<string, object>(colCount, StringComparer.OrdinalIgnoreCase);

                for (int c = 0; c < colCount; c++)
                {
                    string key = colKeys[c];
                    if (string.IsNullOrEmpty(key))
                        continue;

                    string raw = c < cols.Count ? cols[c] : string.Empty;
                    if (!string.IsNullOrWhiteSpace(raw))
                        allEmpty = false;

                    object val = CastValue(raw, colTypes[c]);
                    obj[key] = val;
                }

                if (!allEmpty)
                    rows.Add(obj);
            }

            // DataArray 구조로 래핑하여 JsonParcing.ReaderArray<T>와 호환되도록 함
            var dataWrapper = new { data = rows };
            return JsonConvert.SerializeObject(dataWrapper, Formatting.Indented);
        }

        private static char DetectDelimiter(string csvText)
        {
            List<string> lines = ReadAllLines(csvText);
            int sample = Math.Min(3, lines.Count);
            char[] candidates = new[] { ',', ';', '\t' };
            char best = ',';
            int bestScore = -1;
            for (int ci = 0; ci < candidates.Length; ci++)
            {
                char d = candidates[ci];
                int score = 0;
                for (int i = 0; i < sample; i++)
                    score += CountChar(lines[i], d);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = d;
                }
            }
            return best;
        }

        private static void DetectHeaderRows(string csvText, char delimiter, out int typesRowIndex, out int keysRowIndex, out int dataStartRowIndex)
        {
            // 기본값(0:형, 2:변수명, 3:데이터)
            typesRowIndex = 0;
            keysRowIndex = 2;
            dataStartRowIndex = 3;

            List<string> lines = ReadAllLines(csvText);
            if (lines.Count < 2)
                return;

            List<string> row0 = SplitCsvLine(lines[0], delimiter);
            int typeHits = 0;
            for (int i = 0; i < row0.Count; i++)
            {
                string s = SanitizeType(row0[i]);
                if (s == "int" || s == "float" || s == "bool" || s == "ushort")
                    typeHits++;
            }
            if (typeHits < Math.Max(1, row0.Count / 3))
            {
                // 타입 행이 없다고 판단
                typesRowIndex = -1;
                keysRowIndex = 0;
                dataStartRowIndex = 1;
            }
        }

        private static object CastValue(string raw, string typeName)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return null;

            raw = raw.Trim();
            switch (typeName)
            {
                case "ushort":
                case "unsignedshort":
                    if (uint.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out uint uVal))
                        return (int)Mathf.Clamp((float)uVal, 0f, 65535f);
                    break;
                case "int":
                    if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int iVal))
                        return iVal;
                    break;
                case "float":
                case "single":
                    if (float.TryParse(raw, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out float fVal))
                        return fVal;
                    break;
                case "bool":
                case "boolean":
                    if (TryParseBool(raw, out bool bVal))
                        return bVal;
                    break;
            }
            return raw; // 미확인 타입은 문자열로 유지
        }

        private static bool TryParseBool(string s, out bool value)
        {
            s = s.Trim().ToLowerInvariant();
            if (s == "true" || s == "t" || s == "1" || s == "y" || s == "yes") { value = true; return true; }
            if (s == "false" || s == "f" || s == "0" || s == "n" || s == "no") { value = false; return true; }
            value = false; return false;
        }

        private static string SanitizeType(string t)
        {
            if (string.IsNullOrWhiteSpace(t))
                return string.Empty;
            StringBuilder sb = new StringBuilder(t.Length);
            foreach (char ch in t.ToLowerInvariant())
            {
                if (char.IsLetter(ch))
                    sb.Append(ch);
            }
            return sb.ToString();
        }

        private static List<string> ReadAllLines(string text)
        {
            List<string> lines = new List<string>();
            using (StringReader sr = new StringReader(text))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                    lines.Add(line);
            }
            return lines;
        }

        private static List<string> SplitCsvLine(string line, char delimiter)
        {
            List<string> result = new List<string>();
            StringBuilder sb = new StringBuilder();
            bool inQuotes = false;
            for (int i = 0; i < line.Length; i++)
            {
                char ch = line[i];
                if (ch == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        sb.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (ch == delimiter && !inQuotes)
                {
                    result.Add(sb.ToString());
                    sb.Length = 0;
                }
                else
                {
                    sb.Append(ch);
                }
            }
            result.Add(sb.ToString());
            return result;
        }

        private static int CountChar(string s, char ch)
        {
            int cnt = 0;
            for (int i = 0; i < s.Length; i++) if (s[i] == ch) cnt++;
            return cnt;
        }

        private static string DownloadText(string url)
        {
            using (UnityWebRequest req = UnityWebRequest.Get(url))
            {
                var op = req.SendWebRequest();
                while (!op.isDone) { }
#if UNITY_2020_2_OR_NEWER
                if (req.result != UnityWebRequest.Result.Success)
#else
            if (req.isHttpError || req.isNetworkError)
#endif
                    throw new Exception(req.error);
                return req.downloadHandler.text;
            }
        }
    }
}
#endif


