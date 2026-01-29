using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using FishNet;
using FishNet.Object;
using FishNet.Managing.Scened;
using MyFolder._1._Scripts._3._SingleTone;
using MyFolder._1._Scripts._4._Network;
using MyFolder._1._Scripts._9._Vivox;
using TMPro;
using Unity.Services.Lobbies.Models;
using Unity.Services.Vivox;
using UnityEngine.UI;

namespace MyFolder._1._Scripts._9._Scene
{
    public class BootstrapLoadingController : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private TextMeshProUGUI loadingText;
        [SerializeField] private Image progressBar; // Progress Bar
        [SerializeField] private TextMeshProUGUI progressPercentText; // "45%" 형태로 표시
        
        [Header("Loading Settings")]
        [SerializeField] private float timeoutSeconds = 20f;
        [SerializeField] private bool forceUseCsvDownloader = true;
        [SerializeField] private float retryInterval = 3f; // 재시도 간격
        [SerializeField] private int maxRetryCount = 5; // 최대 재시도 횟수
        
        private bool sentReady;
        private int retryCount = 0;
        
        // 로딩 단계별 가중치 (총 100%)
        private const float STEP_CLIENT_WEIGHT = 15f;      // 15%
        private const float STEP_GAMEDATA_WEIGHT = 25f;    // 25%
        private const float STEP_PLAYER_WEIGHT = 15f;      // 15%
        private const float STEP_VIVOX_WEIGHT = 15f;       // 15%
        private const float STEP_ROOM_WEIGHT = 15f;        // 15%
        private const float STEP_SCENE_WEIGHT = 15f;       // 15%
        
        private float currentProgress = 0f;

        private IEnumerator Start()
        {
            // 초기화
            UpdateProgress(0f, "초기화 중...");
            
            // ===== 1단계: ClientManager 대기 (0% → 15%) =====
            UpdateProgress(0f, "네트워크 연결 대기 중...");
            float t = 0f;
            while ((!InstanceFinder.ClientManager || !InstanceFinder.ClientManager.Started) && t < timeoutSeconds)
            {
                float stepProgress = Mathf.Clamp01(t / timeoutSeconds);
                UpdateProgress(STEP_CLIENT_WEIGHT * stepProgress, "네트워크 연결 대기 중...");
                yield return null; 
                t += Time.deltaTime;
            }
            UpdateProgress(STEP_CLIENT_WEIGHT, "네트워크 연결 완료");
            
            // ===== 2단계: GameDataManager 대기 (15% → 40%) =====
            UpdateProgress(STEP_CLIENT_WEIGHT, "게임 데이터 로딩 중...");
            t = 0f;
            while (!GameDataManager.Instance && t < timeoutSeconds)
            {
                float stepProgress = Mathf.Clamp01(t / timeoutSeconds);
                UpdateProgress(STEP_CLIENT_WEIGHT + (STEP_GAMEDATA_WEIGHT * stepProgress), "게임 데이터 로딩 중...");
                yield return null; 
                t += Time.deltaTime;
            }
            
            if (GameDataManager.Instance)
            {
                if (forceUseCsvDownloader)
                    GameDataManager.Instance.SetUseCsvDownloader(true);

                t = 0f;
                while (!GameDataManager.Instance.IsDataInitialized && t < timeoutSeconds)
                {
                    float stepProgress = Mathf.Clamp01(t / timeoutSeconds);
                    UpdateProgress(STEP_CLIENT_WEIGHT + (STEP_GAMEDATA_WEIGHT * stepProgress), "게임 데이터 초기화 중...");
                    yield return null; 
                    t += Time.deltaTime;
                }
            }
            UpdateProgress(STEP_CLIENT_WEIGHT + STEP_GAMEDATA_WEIGHT, "게임 데이터 로딩 완료");

            // ===== 3단계: PlayerSettingManager 대기 (40% → 55%) =====
            UpdateProgress(STEP_CLIENT_WEIGHT + STEP_GAMEDATA_WEIGHT, "플레이어 설정 로딩 중...");
            
            //스킨 셔플 초기화
            PlayerSettingManager.Instance.SkinSupple();
            t = 0f;
            while ((!PlayerSettingManager.Instance || PlayerSettingManager.Instance.GetLocalPlayerSettings() == null) && t < timeoutSeconds)
            {
                float stepProgress = Mathf.Clamp01(t / timeoutSeconds);
                UpdateProgress(STEP_CLIENT_WEIGHT + STEP_GAMEDATA_WEIGHT + (STEP_PLAYER_WEIGHT * stepProgress), "플레이어 설정 로딩 중...");
                yield return new WaitForSeconds(0.1f); 
                t += 0.1f;
            }
            UpdateProgress(STEP_CLIENT_WEIGHT + STEP_GAMEDATA_WEIGHT + STEP_PLAYER_WEIGHT, "플레이어 설정 로딩 완료");
            
            // ===== 4단계: Vivox 참가 (55% → 70%) =====
            UpdateProgress(STEP_CLIENT_WEIGHT + STEP_GAMEDATA_WEIGHT + STEP_PLAYER_WEIGHT, "음성 채팅 연결 중...");
            t = 0f;
            if (VivoxManager.Instance)
            {
                Lobby room = RoomManager.Instance.GetCurrentRoom();
                VivoxManager.Instance.JoinPositionalChannel(room.Id);
                while (!VivoxManager.Instance._isChannelActive && t < timeoutSeconds)
                {
                    float stepProgress = Mathf.Clamp01(t / timeoutSeconds);
                    UpdateProgress(STEP_CLIENT_WEIGHT + STEP_GAMEDATA_WEIGHT + STEP_PLAYER_WEIGHT + (STEP_VIVOX_WEIGHT * stepProgress), "음성 채팅 연결 중...");
                    yield return new WaitForSeconds(0.1f);
                    t += 0.1f;
                }
            }
            UpdateProgress(STEP_CLIENT_WEIGHT + STEP_GAMEDATA_WEIGHT + STEP_PLAYER_WEIGHT + STEP_VIVOX_WEIGHT, "음성 채팅 연결 완료");
            
            // ===== 5단계: 방 상태 업데이트 (70% → 85%) =====
            UpdateProgress(STEP_CLIENT_WEIGHT + STEP_GAMEDATA_WEIGHT + STEP_PLAYER_WEIGHT + STEP_VIVOX_WEIGHT, "방 설정 업데이트 중...");
            var updateRoomStatus = UpdateRoomStatus();
            yield return new WaitForSeconds(0.5f); // 업데이트 대기
            UpdateProgress(STEP_CLIENT_WEIGHT + STEP_GAMEDATA_WEIGHT + STEP_PLAYER_WEIGHT + STEP_VIVOX_WEIGHT + STEP_ROOM_WEIGHT, "방 설정 업데이트 완료");
            
            // ===== 6단계: 씬 전환 (85% → 100%) =====
            UpdateProgress(STEP_CLIENT_WEIGHT + STEP_GAMEDATA_WEIGHT + STEP_PLAYER_WEIGHT + STEP_VIVOX_WEIGHT + STEP_ROOM_WEIGHT, "게임 씬으로 이동 중...");
            
            // 씬 로드 완료 대기
            bool sceneLoadCompleted = false;
            float sceneLoadTimeout = 10f;
            float sceneLoadElapsed = 0f;
            
            // 씬 로드 완료 이벤트 구독
            void OnSceneLoadEnd(SceneLoadEndEventArgs args)
            {
                sceneLoadCompleted = true;
            }
            
            if (InstanceFinder.NetworkManager && InstanceFinder.NetworkManager.SceneManager)
            {
                InstanceFinder.NetworkManager.SceneManager.OnLoadEnd += OnSceneLoadEnd;
            }
            
            // 씬 전환 시작
            NetworkFlowManager.Instance.LoadSceneForClient(InstanceFinder.ClientManager.Connection, "Ready");
            
            // 씬 로드 완료까지 대기 (진행도 업데이트)
            while (!sceneLoadCompleted && sceneLoadElapsed < sceneLoadTimeout)
            {
                float stepProgress = Mathf.Clamp01(sceneLoadElapsed / sceneLoadTimeout);
                float currentStepProgress = STEP_CLIENT_WEIGHT + STEP_GAMEDATA_WEIGHT + STEP_PLAYER_WEIGHT + STEP_VIVOX_WEIGHT + STEP_ROOM_WEIGHT;
                UpdateProgress(currentStepProgress + (STEP_SCENE_WEIGHT * stepProgress), "게임 씬으로 이동 중...");
                
                yield return null;
                sceneLoadElapsed += Time.deltaTime;
            }
            
            // 이벤트 구독 해제
            if (InstanceFinder.NetworkManager && InstanceFinder.NetworkManager.SceneManager)
            {
                InstanceFinder.NetworkManager.SceneManager.OnLoadEnd -= OnSceneLoadEnd;
            }
            
            if (!sceneLoadCompleted)
            {
                LogManager.LogError(LogCategory.System, "Ready 씬 로드 타임아웃", this);
            }
            else
            {
                UpdateProgress(100f, "로딩 완료!");
                LogManager.Log(LogCategory.System, "Ready 씬 전환 완료", this);
            }
        }
        /// <summary>
        /// 진행도 UI 업데이트
        /// </summary>
        /// <param name="progress">0~100 범위의 진행도</param>
        /// <param name="message">표시할 메시지</param>
        private void UpdateProgress(float progress, string message)
        {
            currentProgress = Mathf.Clamp(progress, 0f, 100f);
            
            // Progress Bar 업데이트
            if (progressBar)
            {
                progressBar.fillAmount = currentProgress / 100f; // 0~1 범위로 정규화
            }
            
            // 퍼센트 텍스트 업데이트
            if (progressPercentText)
            {
                progressPercentText.text = $"{currentProgress:F0}%";
            }
            
            // 로딩 메시지 업데이트
            if (loadingText)
            {
                loadingText.text = message;
            }
            
            LogManager.Log(LogCategory.System, $"[Loading] {currentProgress:F1}% - {message}", this);
        }
        
        private async Task<bool> UpdateRoomStatus()
        {
            bool success = false;
            if (RoomManager.Instance)
            {
                success = await RoomManager.Instance.UpdateRoomStatusAsync(true);
                if(success)
                    LogManager.Log(LogCategory.Lobby,"JoinAbleRoom Success");
                else
                    LogManager.LogError(LogCategory.Lobby,"JoinAbleRoom Failed");
            }
            return success;
        }
        /// <summary>
        /// Ready 씬 전환이 안 되었을 경우 재시도하는 코루틴
        /// </summary>
        private IEnumerator RetryReadyTransitionIfNeeded()
        {
            while (retryCount < maxRetryCount)
            {
                // retryInterval만큼 대기
                yield return new WaitForSeconds(retryInterval);
                
                // 아직 LoadingRoom 씬에 있는지 확인
                string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                if (currentScene != "LoadingRoom")
                {
                    LogManager.Log(LogCategory.System, $"씬이 {currentScene}으로 변경됨. 재시도 중단.", this);
                    yield break; // 이미 다른 씬으로 넘어갔으면 중단
                }
                
                retryCount++;
                LogManager.LogWarning(LogCategory.System, $"Ready 씬 전환 재시도 #{retryCount}/{maxRetryCount}", this);
                
                // 재시도 조건 체크
                if (CanRetryReadyTransition())
                {   
                    LogManager.Log(LogCategory.System, "재시도 조건 만족. Ready 전환 재시도 중...", this);
                    
                    // sentReady 플래그를 무시하고 다시 시도
                    if (NetworkFlowManager.Instance && NetworkFlowManager.Instance.IsSpawned)
                    {
                        try
                        {
                            LogManager.Log(LogCategory.System, "NetworkFlowManager RPC 재호출", this);
                                    NetworkFlowManager.Instance.LoadSceneForClient(InstanceFinder.ClientManager.Connection,"Ready");
                        }
                        catch (System.Exception ex)
                        {
                            LogManager.LogError(LogCategory.System, $"RPC 재호출 실패: {ex.Message}", this);
                            
                            yield break;
                        }
                    }
                    else
                    {
                        LogManager.LogError(LogCategory.System, "NetworkFlowManager 사용 불가. 직접 씬 전환", this);
                        yield break;
                    }
                }
                else
                {
                    LogManager.LogWarning(LogCategory.System, "재시도 조건 불만족. 다음 재시도까지 대기...", this);
                }
            }
            
            // 최대 재시도 횟수 초과
            LogManager.LogError(LogCategory.System, $"Ready 씬 전환 재시도 {maxRetryCount}회 실패. 강제 전환 시도.", this);
        }
        
        /// <summary>
        /// 재시도 가능한 조건인지 체크
        /// </summary>
        private bool CanRetryReadyTransition()
        {
            // 기본 조건들 재확인
            if (!InstanceFinder.ClientManager || !InstanceFinder.ClientManager.Started)
            {
                LogManager.Log(LogCategory.System, "ClientManager 미시작 - 재시도 불가", this);
                return false;
            }
            
            if (!NetworkFlowManager.Instance || !NetworkFlowManager.Instance.IsSpawned)
            {
                LogManager.Log(LogCategory.System, "NetworkFlowManager 미스폰 - 재시도 불가", this);
                return false;
            }
            
            if (!GameDataManager.Instance || !GameDataManager.Instance.IsDataInitialized)
            {
                LogManager.Log(LogCategory.System, "GameDataManager 미초기화 - 재시도 불가", this);
                return false;
            }
            
            if (!PlayerSettingManager.Instance || PlayerSettingManager.Instance.GetLocalPlayerSettings() == null)
            {
                LogManager.Log(LogCategory.System, "PlayerSettingManager 미로드 - 재시도 불가", this);
                return false;
            }
            
            return true;
        }
    }
}
