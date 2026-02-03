using System;
using System.Collections;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using FishNet.Managing.Debugging;
using MyFolder._1._Scripts._0._System.Data;
using MyFolder._1._Scripts._3._SingleTone;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace MyFolder._1._Scripts._0._System.Bootstrap
{
    public enum CsvStatus
    {
        Initial,   // 첫 다운로드
        Updated,   // 변경 감지
        Unchanged  // 변경 없음
    }

    public class PreTitleCsvBootstrap : MonoBehaviour
    {
        [SerializeField] private CsvDownloadConfig config;
        [SerializeField] private bool downloadOnStart = true;
        public bool downloadOnFinish = false;
        [SerializeField] private Image progressBar;
        [SerializeField] private TextMeshProUGUI versionInfoText;

        private void Start()
        {
            if (!downloadOnStart) 
                StartCoroutine(nameof(CSVBootstrap));
        }
            
        private IEnumerator CSVBootstrap()
        {
            if (!config) yield break;

            // 데이터 저장위치를 확인
            string dataDir = Path.Combine(Application.persistentDataPath, "Data");
            // 해당 경로 파일 없을 경우 새로운 파일 생성
            if (!Directory.Exists(dataDir)) Directory.CreateDirectory(dataDir);
            
            float onstep = 1.0f / config.entries.Count;
            progressBar.fillAmount = 0;
            
            // 버전 정보를 저장할 StringBuilder
            StringBuilder versionInfo = new StringBuilder();
            
            foreach (var e in config.entries)
            {
                if (e == null || string.IsNullOrWhiteSpace(e.key) || string.IsNullOrWhiteSpace(e.url))
                    continue;
                
                // 현 타임 + 랜덤값 = 버퍼  생성
                string cacheBuster = GenerateCacheBuster();
                // 주소에 버퍼라인 추가
                string urlWithCache = e.url + (e.url.Contains("?") ? "&" : "?") + $"cb={cacheBuster}";
                
                LogManager.Log(LogCategory.System,$"[{e.key}] 다운로드 시작 (CacheBuster: {cacheBuster})");

                using (var req = UnityWebRequest.Get(urlWithCache))
                {
                    // HTTP 캐시 무효화 헤더 추가
                    req.SetRequestHeader("Cache-Control", "no-cache, no-store, must-revalidate");
                    req.SetRequestHeader("Pragma", "no-cache");
                    req.SetRequestHeader("Expires", "0");
                    
                    // 웹에 요청
                    yield return req.SendWebRequest();
                    
                    if (req.result != UnityWebRequest.Result.Success)
                    {
                        LogManager.LogWarning(LogCategory.System,$"CSV download failed: {e.key} {req.error}");
                        continue;
                    }
                    
                    // 다운로드 정보 획득
                    var csv = req.downloadHandler.text;
                    
                    // 파일 해시 기반 변경 감지로 실제 수정 시간 추적
                    string formattedDate = GetCsvModifiedTimeByHash(e.key, csv, out CsvStatus status);
                    
                    // 버전 정보에 색상 추가하여 표시
                    string coloredLine = GetColoredVersionLine(e.key, formattedDate, status);
                    versionInfo.AppendLine(coloredLine);
                    
                    int headerIndex = e.headerLineIndex;
                    //Json 으로 전환
                    var json = CsvToJson.Convert(csv, headerIndex);

                    // 파일명 설정
                    var fileName = string.IsNullOrEmpty(e.fileName)
                        ? (e.key + ".json")
                        : (e.fileName.EndsWith(".json") ? e.fileName : e.fileName + ".json");
                    // 주소 설정
                    var path = Path.Combine(dataDir, fileName);
                    
                    var tmp = path + ".tmp";
                    try
                    {
                        //파일 작성 및 이동
                        File.WriteAllText(tmp, json);
                        if (File.Exists(path)) File.Delete(path);
                        File.Move(tmp, path);
                        Debug.Log($"Saved JSON: {path}");
                        progressBar.fillAmount += onstep;
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"Save JSON failed: {path} {ex.Message}");
                        if (File.Exists(tmp)) File.Delete(tmp);
                    }
                }
            }

            // 버전 정보를 TextMeshProUGUI에 표시
            if (versionInfoText)
            {
                versionInfoText.text = versionInfo.ToString();
            }

            downloadOnFinish = true;
        }

        public void StopDownload()
        {
            StopAllCoroutines();
            downloadOnFinish = true;
        }
        
        public void RefreashDownload()
        {
            downloadOnFinish = false;
            StartCoroutine(nameof(Start));
        }

        /// <summary>
        /// 파일 해시 기반으로 CSV의 실제 수정 시간을 추적합니다.
        /// CSV 내용이 변경되면 변경 시점의 시간을 저장하고 표시합니다.
        /// </summary>
        private string GetCsvModifiedTimeByHash(string key, string csvContent, out CsvStatus status)
        {
            // 현재 CSV 내용의 해시 계산
            string currentHash = ComputeHash(csvContent);
            
            // PlayerPrefs에서 저장된 해시와 시간 가져오기
            string savedHash = PlayerPrefs.GetString($"CSV_Hash_{key}", "");
            string savedTime = PlayerPrefs.GetString($"CSV_Time_{key}", "");
            
            if (string.IsNullOrEmpty(savedHash))
            {
                // 첫 다운로드: 현재 시간 저장
                DateTime now = DateTime.Now;
                PlayerPrefs.SetString($"CSV_Hash_{key}", currentHash);
                PlayerPrefs.SetString($"CSV_Time_{key}", now.ToString("o"));
                PlayerPrefs.Save();
                
                status = CsvStatus.Initial;
                Debug.Log($"<color=cyan>[{key}]</color> 첫 다운로드 완료 (해시: {currentHash.Substring(0, 8)}...)");
                return now.ToString("yyyy-MM-dd HH:mm:ss");
            }
            else if (currentHash != savedHash)
            {
                // 변경 감지: 새 시간 저장
                DateTime now = DateTime.Now;
                PlayerPrefs.SetString($"CSV_Hash_{key}", currentHash);
                PlayerPrefs.SetString($"CSV_Time_{key}", now.ToString("o"));
                PlayerPrefs.Save();
                
                status = CsvStatus.Updated;
                Debug.Log($"<color=yellow>[{key}]</color> 변경 감지! " +
                         $"이전 해시: {savedHash.Substring(0, 8)}... → " +
                         $"현재 해시: {currentHash.Substring(0, 8)}...");
                return now.ToString("yyyy-MM-dd HH:mm:ss");
            }
            else
            {
                // 변경 없음: 저장된 시간 표시
                status = CsvStatus.Unchanged;
                
                if (!string.IsNullOrEmpty(savedTime))
                {
                    try
                    {
                        DateTime lastUpdate = DateTime.Parse(savedTime);
                        Debug.Log($"<color=green>[{key}]</color> 변경 없음 (마지막 업데이트: {lastUpdate:yyyy-MM-dd HH:mm:ss})");
                        return lastUpdate.ToString("yyyy-MM-dd HH:mm:ss");
                    }
                    catch
                    {
                        Debug.LogWarning($"[{key}] 저장된 시간 파싱 실패: {savedTime}");
                    }
                }
                
                // Fallback: 현재 시간
                return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            }
        }

        /// <summary>
        /// CSV 상태에 따라 색상이 적용된 버전 라인을 생성합니다.
        /// </summary>
        private string GetColoredVersionLine(string key, string date, CsvStatus status)
        {
            string color;
            string statusText = "";
            
            switch (status)
            {
                case CsvStatus.Initial:
                    color = "#00BFFF"; // 청록색 (DeepSkyBlue)
                    statusText = " [신규]";
                    break;
                case CsvStatus.Updated:
                    color = "#FFD700"; // 노란색 (Gold)
                    statusText = " [갱신]";
                    break;
                case CsvStatus.Unchanged:
                    color = "#90EE90"; // 연한 초록색 (LightGreen)
                    statusText = "";
                    break;
                default:
                    color = "#FFFFFF"; // 흰색
                    statusText = "";
                    break;
            }
            
            return $"<color={color}>{key} - {date}{statusText}</color>";
        }

        /// <summary>
        /// SHA256 해시를 계산합니다.
        /// </summary>
        private string ComputeHash(string content)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(content);
                byte[] hash = sha256.ComputeHash(bytes);
                return BitConverter.ToString(hash).Replace("-", "");
            }
        }

        /// <summary>
        /// 강력한 캐시 버스팅을 위한 고유 값을 생성합니다.
        /// 밀리초 단위 타임스탬프 + 랜덤 값 조합으로 매번 다른 URL 생성
        /// </summary>
        private string GenerateCacheBuster()
        {
            // 밀리초 단위 타임스탬프
            long milliseconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            
            // 랜덤 값 추가 (더 확실한 캐시 버스팅)
            int random = UnityEngine.Random.Range(10000, 99999);
            
            return $"{milliseconds}{random}";
        }
    }
}

