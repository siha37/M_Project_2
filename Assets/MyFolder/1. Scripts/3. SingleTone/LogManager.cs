using MyFolder._1._Scripts._5._EditorTool;
using UnityEngine;

namespace MyFolder._1._Scripts._3._SingleTone
{
    [System.Serializable]
    public enum LogCategory
    {
        Network,     // TCP, FishNet, 방 관리
        Lobby,       // Unity Lobby Service, 하트비트, 방 생성/참가
        Player,      // 플레이어 컨트롤, 동기화, 입력
        Enemy,       // 적 관리, AI, 상태 동기화  
        Spawner,     // 오브젝트 스포너 관리
        Projectile,  // 총알 풀링, 발사 시스템
        UI,          // 네트워크 UI, 방 리스트
        System,      // 싱글톤, 게임 설정 등
        Camera,      // 카메라 추적 등
        Quest,       // 퀘스트  
        Vivox,       //  Vivox
        All          // 전체 제어용
    }


    public static class LogManager
    {
        private static LogConfiguration config;
        private static bool isInitialized = false;
    
        public static LogConfiguration Config
        {
            get
            {
                if (!config)
                {
                    LoadConfiguration();
                }
                return config;
            }
        }
    
        // 설정 로드 및 이벤트 등록
        private static void LoadConfiguration()
        {
            config = Resources.Load<LogConfiguration>("LogConfiguration");
            if (!config)
            {
                Debug.LogWarning("[LogManager] LogConfiguration을 찾을 수 없습니다. Resources 폴더에 설정 파일을 생성하세요.");
                config = ScriptableObject.CreateInstance<LogConfiguration>();
            }
        
            // 처음 초기화할 때만 이벤트 등록
            if (!isInitialized)
            {
                LogConfiguration.OnConfigurationChanged += OnConfigurationChanged;
                isInitialized = true;
            }
        }
    
        // 설정 변경 시 호출되는 콜백
        private static void OnConfigurationChanged()
        {
            Debug.Log("🔄 [LogManager] 설정이 런타임에 변경되었습니다! 새 설정이 적용됩니다.");
        
            // 설정 출력 (선택사항)
            if (config)
            {
                PrintCurrentSettings();
            }
        }
    
        // 수동으로 설정 리로드 (API용)
        public static void ReloadConfiguration()
        {
            config = null;
            LoadConfiguration();
            Debug.Log("🔄 [LogManager] 설정을 수동으로 리로드했습니다.");
        }
    
        public static void Log(LogCategory category, string message, Object context = null)
        {
            if (Config.IsLogEnabled(category) && Config.showInfoLogs)
            {
                string categoryTag = $"[{category}]";
                string colorCode = Config.GetCategoryColor(category);
            
                if (Config.useColorCoding && !string.IsNullOrEmpty(colorCode))
                {
                    Debug.Log($"<color={colorCode}>{categoryTag}</color> {message}", context);
                }
                else
                {
                    Debug.Log($"{categoryTag} {message}", context);
                }
            }
        }
    
        public static void LogWarning(LogCategory category, string message, Object context = null)
        {
            if (Config.IsLogEnabled(category) && Config.showWarningLogs)
            {
                string categoryTag = $"[{category}]";
                string colorCode = Config.GetCategoryColor(category);
            
                if (Config.useColorCoding && !string.IsNullOrEmpty(colorCode))
                {
                    Debug.LogWarning($"<color={colorCode}>{categoryTag}</color> {message}", context);
                }
                else
                {
                    Debug.LogWarning($"{categoryTag} {message}", context);
                }
            }
        }
    
        public static void LogError(LogCategory category, string message, Object context = null)
        {
            if (Config.IsLogEnabled(category) && Config.showErrorLogs)
            {
                string categoryTag = $"[{category}]";
                string colorCode = Config.GetCategoryColor(category);
            
                if (Config.useColorCoding && !string.IsNullOrEmpty(colorCode))
                {
                    Debug.LogError($"<color={colorCode}>{categoryTag}</color> {message}", context);
                }
                else
                {
                    Debug.LogError($"{categoryTag} {message}", context);
                }
            }
        }
    
    
        // 현재 설정 상태 출력
        public static void PrintCurrentSettings()
        {
            if (Config == null) return;
        
            Debug.Log("=== LogManager 현재 설정 ===");
            Debug.Log($"전체 로그: {Config.enableAllLogs}");
            Debug.Log($"Network: {Config.enableNetworkLogs}");
            Debug.Log($"Lobby: {Config.enableLobbyLogs}");
            Debug.Log($"Player: {Config.enablePlayerLogs}");
            Debug.Log($"Enemy: {Config.enableEnemyLogs}");
            Debug.Log($"Spawner: {Config.enableSpawnerLogs}");
            Debug.Log($"Projectile: {Config.enableProjectileLogs}");
            Debug.Log($"UI: {Config.enableUILogs}");
            Debug.Log($"System: {Config.enableSystemLogs}");
            Debug.Log($"Camera: {Config.enableCameraLogs}");
            Debug.Log($"Camera: {Config.enableQuestLogs}");
            Debug.Log($"Camera: {Config.enableVivoxLogs}");
            Debug.Log("========================");
        }
    
        // 런타임에서 특정 카테고리의 현재 상태 확인
        public static bool IsLogEnabled(LogCategory category)
        {
            return Config.IsLogEnabled(category);
        }
    
        // 런타임에서 로그 시스템 상태 요약
        public static string GetStatusSummary()
        {
            if (Config == null) return "LogManager: 설정 없음";
        
            int enabledCount = 0;
            if (Config.enableNetworkLogs) enabledCount++;
            if (Config.enableLobbyLogs) enabledCount++;
            if (Config.enablePlayerLogs) enabledCount++;
            if (Config.enableEnemyLogs) enabledCount++;
            if (Config.enableSpawnerLogs) enabledCount++;
            if (Config.enableProjectileLogs) enabledCount++;
            if (Config.enableUILogs) enabledCount++;
            if (Config.enableSystemLogs) enabledCount++;
            if (Config.enableCameraLogs) enabledCount++;
            if (Config.enableVivoxLogs) enabledCount++;
            if (Config.enableQuestLogs) enabledCount++;
        
            return $"LogManager: {(Config.enableAllLogs ? "활성화" : "비활성화")} ({enabledCount}/9 카테고리 활성화)";
        }
    }
}