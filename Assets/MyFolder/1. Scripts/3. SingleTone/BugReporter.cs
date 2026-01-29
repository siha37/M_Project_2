using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Firebase;
using Firebase.Firestore;
using Firebase.Extensions;

namespace MyFolder._1._Scripts._3._SingleTone
{
    /// <summary>
    /// 버그 리포트 수집 및 전송 시스템
    /// - 로그, 스크린샷, 시스템 정보 자동 수집
    /// - Firebase Firestore에 저장
    /// - 스팸 방지 (쿨다운 + 일일 제한)
    /// </summary>
    public class BugReporter : SingleTone<BugReporter>
    {
        [Header("Firebase 설정")]
        [SerializeField] private string firestoreCollection = "bugReports";
        
        [Header("수집 설정")]
        [SerializeField] private int maxLogLines = 100;
        
        [Header("스팸 방지")]
        [SerializeField] private float cooldownSeconds = 60f;
        [SerializeField] private int dailyLimit = 10;
        
        [Header("디버그")]
        [SerializeField] private bool showDebugLogs = true;
        
        // Firebase
        private FirebaseFirestore firestore;
        private bool isFirebaseInitialized = false;
        
        // 로그 수집
        private readonly List<string> capturedLogs = new List<string>();
        private readonly object logLock = new object();
        
        // 쿨다운 타이머
        private float lastSendTime = -999f;
        
        // PlayerPrefs 키
        private const string PREF_SEND_COUNT = "BugReport_SendCount";
        private const string PREF_LAST_DATE = "BugReport_LastDate";
        
        protected override void Awake()
        {
            base.Awake();
            
            // 로그 리스너 등록 (에러/예외만 캡처)
            Application.logMessageReceivedThreaded += OnLogReceived;
            
            // Firebase 초기화 시작
            InitializeFirebase();
            
            if (showDebugLogs)
            {
                LogManager.Log(LogCategory.System, "BugReporter 초기화 완료", this);
            }
        }
        
        private void OnDestroy()
        {
            Application.logMessageReceivedThreaded -= OnLogReceived;
        }
        
        /// <summary>
        /// Firebase 초기화
        /// </summary>
        private void InitializeFirebase()
        {
            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
            {
                DependencyStatus dependencyStatus = task.Result;
                
                if (dependencyStatus == DependencyStatus.Available)
                {
                    firestore = FirebaseFirestore.DefaultInstance;
                    isFirebaseInitialized = true;
                    
                    if (showDebugLogs)
                    {
                        LogManager.Log(LogCategory.System, "Firebase 초기화 성공", this);
                    }
                }
                else
                {
                    isFirebaseInitialized = false;
                    LogManager.LogError(LogCategory.System, 
                        $"Firebase 초기화 실패: {dependencyStatus}", this);
                }
            });
        }
        
        /// <summary>
        /// 로그 캡처 (에러/예외만)
        /// </summary>
        private void OnLogReceived(string logString, string stackTrace, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception)
                return;
            
            lock (logLock)
            {
                string timestamp = DateTime.Now.ToString("HH:mm:ss");
                string logEntry = $"[{timestamp}] [{type}] {logString}\n{stackTrace}";
                
                capturedLogs.Add(logEntry);
                
                // 로그 개수 제한
                if (capturedLogs.Count > maxLogLines)
                {
                    capturedLogs.RemoveAt(0);
                }
            }
        }
        
        /// <summary>
        /// 버그 리포트 전송 (옵션 메뉴에서 호출)
        /// </summary>
        public void SendBugReport(string userDescription = "")
        {
            // 쿨다운 체크
            if (Time.realtimeSinceStartup - lastSendTime < cooldownSeconds)
            {
                float remainingTime = cooldownSeconds - (Time.realtimeSinceStartup - lastSendTime);
                AlertManager.Instance.ShowAlert(
                    "전송 제한",
                    $"잠시 후 다시 시도해주세요. ({Mathf.CeilToInt(remainingTime)}초 후)",
                    AlertManager.AlertType.Warning
                );
                return;
            }
            
            // 일일 제한 체크
            if (!CheckDailyLimit())
            {
                AlertManager.Instance.ShowAlert(
                    "죄송합니다.",
                    "오늘 버그 리포트 전송 횟수를 초과했습니다.",
                    AlertManager.AlertType.Warning
                );
                return;
            }
            
            if (showDebugLogs)
            {
                LogManager.Log(LogCategory.System, "버그 리포트 전송 시작...", this);
            }
            
            StartCoroutine(CollectAndSendReport(userDescription));
        }
        
        /// <summary>
        /// 일일 제한 확인
        /// </summary>
        private bool CheckDailyLimit()
        {
            string today = DateTime.Now.ToString("yyyy-MM-dd");
            string lastDate = PlayerPrefs.GetString(PREF_LAST_DATE, "");
            int sendCount = PlayerPrefs.GetInt(PREF_SEND_COUNT, 0);
            
            // 날짜가 바뀌면 카운트 리셋
            if (lastDate != today)
            {
                PlayerPrefs.SetString(PREF_LAST_DATE, today);
                PlayerPrefs.SetInt(PREF_SEND_COUNT, 0);
                PlayerPrefs.Save();
                sendCount = 0;
            }
            
            return sendCount < dailyLimit;
        }
        
        /// <summary>
        /// 일일 카운트 증가
        /// </summary>
        private void IncrementDailyCount()
        {
            int sendCount = PlayerPrefs.GetInt(PREF_SEND_COUNT, 0);
            PlayerPrefs.SetInt(PREF_SEND_COUNT, sendCount + 1);
            PlayerPrefs.Save();
            
            if (showDebugLogs)
            {
                LogManager.Log(LogCategory.System, 
                    $"버그 리포트 전송 카운트: {sendCount + 1}/{dailyLimit}", this);
            }
        }
        
        /// <summary>
        /// 데이터 수집 및 전송
        /// </summary>
        private IEnumerator CollectAndSendReport(string userDescription)
        {
            // 수집 시작 알림
            AlertManager.Instance.ShowAlert(
                "버그 리포트",
                "데이터를 수집하고 있습니다...",
                AlertManager.AlertType.Info
            );
            
            // 1. 시스템 정보 수집
            SystemInfoData systemInfo = CollectSystemInfo();
            
            // 2. 로그 수집
            string logs = CollectLogs();
            
            // 3. 수집 완료 - 알람 닫기
            AlertManager.Instance.CloseCurrentAlert();
            
            // 4. 버그 리포트 데이터 생성
            BugReportData reportData = new BugReportData
            {
                UserDescription = userDescription,
                SystemInfo = systemInfo,
                Logs = logs,
                Screenshot = null,
                Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };
            
            // 5. Firebase Firestore에 전송
            bool uploadSuccess = false;
            yield return StartCoroutine(UploadToFirestore(reportData, result => uploadSuccess = result));
            
            // 6. 결과 처리
            if (uploadSuccess)
            {
                lastSendTime = Time.realtimeSinceStartup;
                IncrementDailyCount();
                
                AlertManager.Instance.ShowAlert(
                    "버그 리포트 전송 완료",
                    "버그 제보해주셔서 감사합니다!",
                    AlertManager.AlertType.Success
                );
            }
            else
            {
                AlertManager.Instance.ShowAlert(
                    "전송 실패",
                    "버그 리포트 전송에 실패했습니다.\n네트워크 연결을 확인하고 다시 시도해주세요.",
                    AlertManager.AlertType.Error
                );
            }
        }
        
        /// <summary>
        /// 시스템 정보 수집 (최소한의 정보만)
        /// </summary>
        private SystemInfoData CollectSystemInfo()
        {
            return new SystemInfoData
            {
                OperatingSystem = SystemInfo.operatingSystem,
                UnityVersion = Application.unityVersion,
                GameVersion = Application.version,
                Platform = Application.platform.ToString()
            };
        }
        
        /// <summary>
        /// 로그 수집 (에러/예외만)
        /// </summary>
        private string CollectLogs()
        {
            lock (logLock)
            {
                if (capturedLogs.Count == 0)
                {
                    return "(로그 없음)";
                }
                
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"=== 캡처된 로그 ({capturedLogs.Count}개) ===\n");
                
                foreach (string log in capturedLogs)
                {
                    sb.AppendLine(log);
                    sb.AppendLine("---");
                }
                
                return sb.ToString();
            }
        }
        
        /// <summary>
        /// Firebase Firestore에 데이터 업로드
        /// </summary>
        private IEnumerator UploadToFirestore(BugReportData reportData, Action<bool> callback)
        {
            // Firebase 초기화 확인
            if (!isFirebaseInitialized)
            {
                LogManager.LogError(LogCategory.System, 
                    "Firebase가 초기화되지 않았습니다. Firestore에 전송할 수 없습니다.", this);
                callback?.Invoke(false);
                yield break;
            }
            
            if (showDebugLogs)
            {
                LogManager.Log(LogCategory.System, "Firestore 업로드 시작...", this);
            }
            
            bool uploadComplete = false;
            bool uploadSuccess = false;
            
            // Firestore에 저장할 데이터 딕셔너리 생성
            Dictionary<string, object> data = new Dictionary<string, object>
            {
                { "userDescription", reportData.UserDescription ?? "" },
                { "operatingSystem", reportData.SystemInfo.OperatingSystem },
                { "unityVersion", reportData.SystemInfo.UnityVersion },
                { "gameVersion", reportData.SystemInfo.GameVersion },
                { "platform", reportData.SystemInfo.Platform },
                { "logs", reportData.Logs },
                { "screenshot", reportData.Screenshot ?? "" },
                { "timestamp", reportData.Timestamp },
                { "submittedAt", FieldValue.ServerTimestamp }
            };
            
            // Firestore에 문서 추가
            firestore.Collection(firestoreCollection).AddAsync(data).ContinueWithOnMainThread(task =>
            {
                uploadComplete = true;
                
                if (task.IsCompleted && !task.IsFaulted && !task.IsCanceled)
                {
                    uploadSuccess = true;
                    
                    if (showDebugLogs)
                    {
                        DocumentReference docRef = task.Result;
                        LogManager.Log(LogCategory.System, 
                            $"Firestore 업로드 성공! 문서 ID: {docRef.Id}", this);
                    }
                }
                else if (task.IsFaulted)
                {
                    uploadSuccess = false;
                    LogManager.LogError(LogCategory.System, 
                        $"Firestore 업로드 실패: {task.Exception?.Message}", this);
                }
            });
            
            // 업로드 완료까지 대기
            while (!uploadComplete)
            {
                yield return null;
            }
            
            callback?.Invoke(uploadSuccess);
        }
        
        /// <summary>
        /// 현재 일일 전송 횟수 조회 (UI 표시용)
        /// </summary>
        public int GetTodaySendCount()
        {
            string today = DateTime.Now.ToString("yyyy-MM-dd");
            string lastDate = PlayerPrefs.GetString(PREF_LAST_DATE, "");
            
            if (lastDate != today)
                return 0;
            
            return PlayerPrefs.GetInt(PREF_SEND_COUNT, 0);
        }
        
        /// <summary>
        /// 남은 일일 전송 횟수 조회 (UI 표시용)
        /// </summary>
        public int GetRemainingDailyCount()
        {
            return dailyLimit - GetTodaySendCount();
        }
    }
    
    /// <summary>
    /// 버그 리포트 데이터 구조
    /// </summary>
    [Serializable]
    public class BugReportData
    {
        public string UserDescription;
        public SystemInfoData SystemInfo;
        public string Logs;
        public string Screenshot;
        public string Timestamp;
    }
    
    /// <summary>
    /// 시스템 정보 데이터
    /// </summary>
    [Serializable]
    public class SystemInfoData
    {
        public string OperatingSystem;
        public string UnityVersion;
        public string GameVersion;
        public string Platform;
    }
}
