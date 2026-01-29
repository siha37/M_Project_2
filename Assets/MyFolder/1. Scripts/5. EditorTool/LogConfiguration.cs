

using MyFolder._1._Scripts._3._SingleTone;
using UnityEngine;

namespace MyFolder._1._Scripts._5._EditorTool
{

[CreateAssetMenu(fileName = "LogConfiguration", menuName = "MyProject/Log Configuration")]
public class LogConfiguration : ScriptableObject
{
    [Header("🔧 전체 로그 제어")]
    public bool enableAllLogs = true;
    
    [Header("📋 카테고리별 로그 활성화")]
    public bool enableNetworkLogs = true;
    public bool enableLobbyLogs = true;
    public bool enablePlayerLogs = true;
    public bool enableEnemyLogs = true;
    public bool enableSpawnerLogs = true;
    public bool enableProjectileLogs = true;
    public bool enableUILogs = true;
    public bool enableSystemLogs = true;
    public bool enableCameraLogs = true;
    public bool enableQuestLogs = true;
    public bool enableVivoxLogs = true;
    
    [Header("📊 로그 레벨 설정")]
    public bool showInfoLogs = true;
    public bool showWarningLogs = true;
    public bool showErrorLogs = true;
    
    [Header("🎨 컬러 코딩 (선택사항)")]
    public bool useColorCoding = true;
    
    // 런타임 변경 감지를 위한 이벤트
    public static System.Action OnConfigurationChanged;
    
    // Unity 에디터에서 값이 변경될 때 호출됨 (런타임 포함)
    private void OnValidate()
    {
        // 게임이 실행 중일 때만 이벤트 발생
        if (Application.isPlaying)
        {
            OnConfigurationChanged?.Invoke();
        }
    }
    
    [Header("🧪 테스트 및 유틸리티")]
    [Space(10)]
    [Tooltip("런타임에서 각 카테고리별 테스트 로그를 출력해보세요!")]
    public bool showTestSection = true;
    
    // 인스펙터 테스트 버튼들 (ContextMenu로 구현)
    [ContextMenu("📊 현재 설정 출력")]
    public void PrintCurrentSettings()
    {
        if (Application.isPlaying)
        {
            LogManager.PrintCurrentSettings();
        }
        else
        {
            Debug.Log("⚠️ 게임이 실행 중일 때만 사용 가능합니다.");
        }
    }
    
    [ContextMenu("🧪 모든 카테고리 테스트 로그")]
    public void TestAllCategories()
    {
        if (Application.isPlaying)
        {
            Debug.Log("🧪 LogConfiguration 테스트 시작...");
            LogManager.Log(LogCategory.Network, "Network 테스트 로그입니다!");
            LogManager.Log(LogCategory.Lobby, "Lobby 테스트 로그입니다!");
            LogManager.Log(LogCategory.Player, "Player 테스트 로그입니다!");
            LogManager.Log(LogCategory.Enemy, "Enemy 테스트 로그입니다!");
            LogManager.Log(LogCategory.Spawner, "Spawner 테스트 로그입니다!");
            LogManager.Log(LogCategory.Projectile, "Projectile 테스트 로그입니다!");
            LogManager.Log(LogCategory.UI, "UI 테스트 로그입니다!");
            LogManager.Log(LogCategory.System, "System 테스트 로그입니다!");
            LogManager.Log(LogCategory.Camera, "Camera 테스트 로그입니다!");
            LogManager.Log(LogCategory.Quest, "Quest 테스트 로그입니다!");
            LogManager.Log(LogCategory.Vivox, "Vivox 테스트 로그입니다!");
            Debug.Log("✅ LogConfiguration 테스트 완료!");
        }
        else
        {
            Debug.Log("⚠️ 게임이 실행 중일 때만 사용 가능합니다.");
        }
    }
    
    [ContextMenu("🔄 설정 리로드")]
    public void ReloadConfiguration()
    {
        if (Application.isPlaying)
        {
            LogManager.ReloadConfiguration();
        }
        else
        {
            Debug.Log("⚠️ 게임이 실행 중일 때만 사용 가능합니다.");
        }
    }
    
    [ContextMenu("🏠 기본값으로 리셋")]
    public void ResetToDefaults()
    {
        enableAllLogs = true;
        enableNetworkLogs = true;
        enableLobbyLogs = true;
        enablePlayerLogs = true;
        enableEnemyLogs = true;
        enableSpawnerLogs = true;
        enableProjectileLogs = true;
        enableUILogs = true;
        enableSystemLogs = true;
        enableCameraLogs = true;
        enableQuestLogs = true;
        enableVivoxLogs = true;
        showInfoLogs = true;
        showWarningLogs = true;
        showErrorLogs = true;
        useColorCoding = true;
        
        Debug.Log("🏠 LogConfiguration을 기본값으로 리셋했습니다.");
        
        if (Application.isPlaying)
        {
            OnConfigurationChanged?.Invoke();
        }
    }
    
    [ContextMenu("❌ 모든 로그 끄기")]
    public void DisableAllLogs()
    {
        enableAllLogs = false;
        Debug.Log("❌ 모든 로그를 비활성화했습니다.");
        
        if (Application.isPlaying)
        {
            OnConfigurationChanged?.Invoke();
        }
    }
    
    [ContextMenu("✅ 모든 로그 켜기")]
    public void EnableAllLogs()
    {
        enableAllLogs = true;
        enableNetworkLogs = true;
        enableLobbyLogs = true;
        enablePlayerLogs = true;
        enableEnemyLogs = true;
        enableSpawnerLogs = true;
        enableProjectileLogs = true;
        enableUILogs = true;
        enableSystemLogs = true;
        enableCameraLogs = true;
        enableQuestLogs = true;
        enableVivoxLogs = true;
        Debug.Log("✅ 모든 로그를 활성화했습니다.");
        
        if (Application.isPlaying)
        {
            OnConfigurationChanged?.Invoke();
        }
    }
    
    public bool IsLogEnabled(LogCategory category)
    {
        if (!enableAllLogs) return false;
        
        return category switch
        {
            LogCategory.Network => enableNetworkLogs,
            LogCategory.Lobby => enableLobbyLogs,
            LogCategory.Player => enablePlayerLogs,
            LogCategory.Enemy => enableEnemyLogs,
            LogCategory.Spawner => enableSpawnerLogs,
            LogCategory.Projectile => enableProjectileLogs,
            LogCategory.UI => enableUILogs,
            LogCategory.System => enableSystemLogs,
            LogCategory.Camera => enableCameraLogs,
            LogCategory.Quest => enableQuestLogs,
            LogCategory.Vivox => enableVivoxLogs,
            LogCategory.All => enableAllLogs,
            _ => true
        };
    }
    
    public string GetCategoryColor(LogCategory category)
    {
        if (!useColorCoding) return "";
        
        return category switch
        {
            LogCategory.Network => "#4CAF50",      // 초록
            LogCategory.Lobby => "#FFC107",        // 노랑
            LogCategory.Player => "#2196F3",       // 파랑
            LogCategory.Enemy => "#F44336",        // 빨강
            LogCategory.Spawner => "#FF9800",      // 주황
            LogCategory.Projectile => "#9C27B0",   // 보라
            LogCategory.UI => "#00BCD4",           // 청록
            LogCategory.System => "#795548",       // 갈색
            LogCategory.Camera => "#607D8B",       // 청회색
            LogCategory.Quest => "#B33999",        // 핑크
            LogCategory.Vivox => "#FB7900",        // 주황
            _ => "#000000"                         // 검정
        };
    }
}
}