#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace MyFolder._1._Scripts._9000.Editor
{
    [Serializable]
    public class GoogleCsvEntry
    {
        public string Name;
        [TextArea]
        public string CsvUrl;
        public string OutputJsonPath = "Assets/SheetData.json"; // Assets 기준 경로 권장

        public bool AutoDetect = true;
        public int TypesRowIndex = 0;
        public int KeysRowIndex = 2;
        public int DataStartRowIndex = 3;
        public string Delimiter = ","; // AutoDetect=false일 때 사용(첫 글자만 사용)
    }

    public class GoogleSheetCsvRegistry : ScriptableObject
    {
        public List<GoogleCsvEntry> Entries = new List<GoogleCsvEntry>();

        [MenuItem("Assets/Create/Data/Google Sheet CSV Registry", priority = 2000)]
        public static void CreateAsset()
        {
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (string.IsNullOrEmpty(path)) path = "Assets";
            if (!AssetDatabase.IsValidFolder(path)) path = Path.GetDirectoryName(path).Replace("\\", "/");
            string assetPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(path, "GoogleSheetCsvRegistry.asset"));
            var asset = CreateInstance<GoogleSheetCsvRegistry>();
            AssetDatabase.CreateAsset(asset, assetPath);
            AssetDatabase.SaveAssets();
            EditorGUIUtility.PingObject(asset);
        }
    }

    [CustomEditor(typeof(GoogleSheetCsvRegistry))]
    public class GoogleSheetCsvRegistryEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.HelpBox("등록한 CSV 공개 링크를 일괄 변환합니다. OutputJsonPath는 'Assets/...' 권장.", MessageType.Info);

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("Entries"), true);

            EditorGUILayout.Space();
            if (GUILayout.Button("Run All (Download & Convert)"))
            {
                RunAll((GoogleSheetCsvRegistry)target);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private static void RunAll(GoogleSheetCsvRegistry registry)
        {
            int success = 0;
            int total = registry.Entries != null ? registry.Entries.Count : 0;
            for (int i = 0; i < total; i++)
            {
                var e = registry.Entries[i];
                if (string.IsNullOrWhiteSpace(e.CsvUrl) || string.IsNullOrWhiteSpace(e.OutputJsonPath))
                {
                    Debug.LogWarning($"[Registry] 항목 건너뜀(index {i}): URL 또는 경로 없음");
                    continue;
                }

                try
                {
                    string csv = DownloadText(e.CsvUrl);
                    char delimiter = ',';
                    int typesIdx = e.TypesRowIndex;
                    int keysIdx = e.KeysRowIndex;
                    int dataStartIdx = e.DataStartRowIndex;

                    if (e.AutoDetect)
                    {
                        delimiter = DetectDelimiter(csv);
                        DetectHeaderRows(csv, delimiter, out typesIdx, out keysIdx, out dataStartIdx);
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(e.Delimiter))
                            delimiter = e.Delimiter[0];
                    }

                    string json = GoogleSheetStructuredCsvToJsonEditor.ConvertStructuredCsvToJson(csv, typesIdx, keysIdx, dataStartIdx, delimiter);
                    string abs = ToAbsolutePath(e.OutputJsonPath);
                    Directory.CreateDirectory(Path.GetDirectoryName(abs));
                    File.WriteAllText(abs, json, new UTF8Encoding(false));
                    success++;
                    Debug.Log($"[Registry] 변환 성공: {e.Name} → {e.OutputJsonPath} (len:{json.Length}, delim:'{delimiter}', types:{typesIdx}, keys:{keysIdx}, data:{dataStartIdx})");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Registry] 변환 실패(index {i}, name:{e.Name}): {ex.Message}");
                }
            }

            AssetDatabase.Refresh();
            Debug.Log($"[Registry] 완료: {success}/{total}");
        }

        private static string ToAbsolutePath(string path)
        {
            if (Path.IsPathRooted(path)) return path;
            if (path.StartsWith("Assets/"))
            {
                string project = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                return Path.GetFullPath(Path.Combine(project, path));
            }
            return Path.GetFullPath(Path.Combine(Application.dataPath, path));
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
            typesRowIndex = 0;
            keysRowIndex = 2;
            dataStartRowIndex = 3;
            List<string> lines = ReadAllLines(csvText);
            if (lines.Count < 2) return;
            List<string> row0 = SplitCsvLine(lines[0], delimiter);
            int typeHits = 0;
            for (int i = 0; i < row0.Count; i++)
            {
                string s = Sanitize(row0[i]);
                if (s == "int" || s == "float" || s == "bool" || s == "ushort") typeHits++;
            }
            if (typeHits < Math.Max(1, row0.Count / 3))
            {
                typesRowIndex = -1;
                keysRowIndex = 0;
                dataStartRowIndex = 1;
            }
        }

        private static List<string> ReadAllLines(string text)
        {
            List<string> lines = new List<string>();
            using (StringReader sr = new StringReader(text))
            {
                string line;
                while ((line = sr.ReadLine()) != null) lines.Add(line);
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
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"') { sb.Append('"'); i++; }
                    else { inQuotes = !inQuotes; }
                }
                else if (ch == delimiter && !inQuotes)
                {
                    result.Add(sb.ToString());
                    sb.Length = 0;
                }
                else { sb.Append(ch); }
            }
            result.Add(sb.ToString());
            return result;
        }

        private static int CountChar(string s, char ch)
        {
            int cnt = 0; for (int i = 0; i < s.Length; i++) if (s[i] == ch) cnt++; return cnt;
        }

        private static string Sanitize(string t)
        {
            if (string.IsNullOrWhiteSpace(t)) return string.Empty;
            StringBuilder sb = new StringBuilder(t.Length);
            foreach (char ch in t.ToLowerInvariant()) if (char.IsLetter(ch)) sb.Append(ch);
            return sb.ToString();
        }
    }
}
#endif


