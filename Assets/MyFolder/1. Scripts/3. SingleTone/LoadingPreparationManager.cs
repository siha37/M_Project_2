using System.Collections;
using System.Collections.Generic;
using FishNet;
using FishNet.Managing.Scened;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using MyFolder._1._Scripts._7._PlayerRole;
using MyFolder._1._Scripts._8999._Utility.Corutin;
using UnityEngine;

namespace MyFolder._1._Scripts._3._SingleTone
{
    /// <summary>
    /// 로딩 씬에서 게임 시작 전 전처리를 담당하는 매니저
    /// </summary>
    public class LoadingPreparationManager : NetworkBehaviour
    {
        public static LoadingPreparationManager Instance { get; private set; }
        [SerializeField] private string nextSceneName;
        private void Awake()
        {
            if (!Instance)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }
        
        
        [Header("Loading Settings")]
        [SerializeField] private float minLoadingTime = 3f;
        
        // 로딩 상태 동기화
        private readonly SyncVar<LoadingPreparationState> syncLoadingState = new SyncVar<LoadingPreparationState>();
        
        // 로딩 전처리 이벤트
        public event System.Action<LoadingPreparationState> OnLoadingStateChanged;
        public event System.Action OnLoadingPreparationCompleted;
        public event System.Action OnGameReadyToStart;
        
        
        /// <summary>
        /// 로딩 씬에서 전처리 시작 (자동 시작)
        /// </summary>
        public override void OnStartServer()
        {
            if (!Instance)
            {
                Instance = this;
            }
            
            // 로딩 상태 초기화
            syncLoadingState.Value = LoadingPreparationState.WaitingForPlayers;
            
            LogManager.Log(LogCategory.System, "LoadingPreparationManager 서버 시작", this);
            
            // 자동으로 전처리 시작
            StartCoroutine(AutoStartPreparationCoroutine());
        }

        public override void OnStartClient()
        {
            syncLoadingState.OnChange += OnLoadingStateChangedCallback;
        }
        
        /// <summary>
        /// 자동 전처리 시작 코루틴
        /// </summary>
        private IEnumerator AutoStartPreparationCoroutine()
        {
            // 잠시 대기 (씬 로딩 완료 대기)
            yield return WaitForSecondsCache.Get(1f);
            
            // 전처리 시작
            StartCoroutine(LoadingPreparationCoroutine());
        }
        
        /// <summary>
        /// 로딩 전처리 코루틴
        /// </summary>
        private IEnumerator LoadingPreparationCoroutine()
        {
            // 1단계: 역할 배정
            syncLoadingState.Value = LoadingPreparationState.AssigningRoles;
            LogManager.Log(LogCategory.System, "1단계: 역할 배정 시작", this);
            
            // PlayerRoleManager에 역할 배정 요청
            PlayerRoleManager.Instance.AssignRolesToAllPlayers();
            
            // 역할 배정 완료 대기
            yield return new WaitUntil(() => PlayerRoleManager.Instance.AreRolesAssigned());
            
            LogManager.Log(LogCategory.System, "1단계: 역할 배정 완료", this);
            
            // 2단계: 리소스 로딩
            syncLoadingState.Value = LoadingPreparationState.LoadingResources;
            LogManager.Log(LogCategory.System, "2단계: 리소스 로딩 시작", this);
            yield return StartCoroutine(LoadGameResourcesCoroutine());
            
            
            // 3단계: MainGame 씬 로딩
            syncLoadingState.Value = LoadingPreparationState.LoadingStage;
            LogManager.Log(LogCategory.System, "3단계: MainGame 씬 로딩 시작", this);
            yield return StartCoroutine(LoadStageSceneCoroutine());
            
            LogManager.Log(LogCategory.System, "3단계: Ready 씬 로딩 완료", this);
            
            // 4단계: 전처리 완료
            syncLoadingState.Value = LoadingPreparationState.Completed;
            LogManager.Log(LogCategory.System, "로딩 전처리 완료", this);
            
            // 전처리 완료 알림
            NotifyLoadingPreparationCompletedClientRpc();
            
            // 잠시 대기 후 게임 시작
            yield return WaitForSecondsCache.Get(1f);
            
            // 게임 시작 이벤트 발생
            OnGameReadyToStart?.Invoke();
        }
            
        /// <summary>
        /// 게임 리소스 로딩 코루틴
        /// </summary>
        private IEnumerator LoadGameResourcesCoroutine()
        {
            // Addressables 사용 시
            #if UNITY_ADDRESSABLES
            var loadOperation = UnityEngine.AddressableAssets.Addressables.LoadSceneAsync("MainScene", UnityEngine.SceneManagement.LoadSceneMode.Additive);
            
            while (!loadOperation.IsDone)
            {
                // 로딩 진행률 업데이트 (UI에 표시 가능)
                float progress = loadOperation.PercentComplete;
                LogManager.Log(LogCategory.System, $"리소스 로딩 진행률: {progress:P0}", this);
                yield return null;
            }
            
            // Addressables 정리
            UnityEngine.AddressableAssets.Addressables.Release(loadOperation);
            
            #else
            // Resources 폴더 사용 시
            //var resourceRequest = Resources.LoadAsync<GameObject>("Prefabs/GameResources");
            
            //while (!resourceRequest.isDone)
            //{
            //    float progress = resourceRequest.progress;
            //    LogManager.Log(LogCategory.System, $"리소스 로딩 진행률: {progress:P0}", this);
            //    yield return null;
            //}
            
            // 추가 리소스 로딩 (필요한 경우)
            yield return WaitForSecondsCache.Get(0.5f);
            #endif
            
            LogManager.Log(LogCategory.System, "게임 리소스 로딩 완료", this);
        }

        /// <summary>
        /// 스테이지 씬 로딩 코루틴 (Ready 씬으로 전환)
        /// </summary>
        private IEnumerator LoadStageSceneCoroutine()
        {
            if (IsServerInitialized && NetworkManager?.SceneManager)
            {
                LogManager.Log(LogCategory.System, $"FishNet GlobalScene으로 {nextSceneName} 씬 로딩 시작", this);
                
                // 씬 로드 완료 플래그
                bool sceneLoadCompleted = false;
                bool loadFailed = false;
        
                // 씬 로드 완료 이벤트 구독
                void OnSceneLoadEnd(SceneLoadEndEventArgs args)
                {
                    sceneLoadCompleted = true;
                }
                
                NetworkManager.SceneManager.OnLoadEnd += OnSceneLoadEnd;
                
                SceneLoadData sceneData = new SceneLoadData(new List<string> {nextSceneName})
                {
                    ReplaceScenes = ReplaceOption.All
                };
                

                try
                {
                    NetworkManager.SceneManager.LoadGlobalScenes(sceneData);
                    LogManager.Log(LogCategory.System, $"FishNet GlobalScene으로 {nextSceneName} 씬 로드 시작 성공", this);
                }
                catch (System.Exception e)
                {
                    LogManager.LogError(LogCategory.System, $"FishNet GlobalScene 로드 실패: {e.Message}", this);
                    loadFailed = true;
                }
        
                // yield return은 try-catch 밖에서 사용
                if (!loadFailed)
                {
                    // 씬 로드 완료까지 대기 (타임아웃 10초)
                    float timeout = 10f;
                    float elapsed = 0f;
            
                    while (!sceneLoadCompleted && elapsed < timeout)
                    {
                        yield return null;
                        elapsed += Time.deltaTime;
                    }
            
                    if (!sceneLoadCompleted)
                    {
                        LogManager.LogError(LogCategory.System, $"씬 로드 타임아웃 (10초 초과)", this);
                    }
                    else
                    {
                        LogManager.Log(LogCategory.System, $"FishNet GlobalScene으로 {nextSceneName} 씬 로드 완료", this);
                    }
                }
        
                // 이벤트 구독 해제
                NetworkManager.SceneManager.OnLoadEnd -= OnSceneLoadEnd;
            }
            else
            {
                LogManager.Log(LogCategory.System, $"클라이언트는 서버의 {nextSceneName} 씬 전환을 대기합니다", this);
            }
    
            yield return null;
        }
        
        /// <summary>
        /// 로딩 전처리 완료 알림
        /// </summary>
        [ObserversRpc]
        private void NotifyLoadingPreparationCompletedClientRpc()
        {
            OnLoadingPreparationCompleted?.Invoke();
            LogManager.Log(LogCategory.System, "로딩 전처리 완료 알림", this);
        }
            
        /// <summary>
        /// 로딩 상태 변경 콜백
        /// </summary>
        private void OnLoadingStateChangedCallback(LoadingPreparationState prev, LoadingPreparationState next, bool asserver)
        {
            OnLoadingStateChanged?.Invoke(next);
            LogManager.Log(LogCategory.System, $"로딩 상태 변경: {prev} → {next}", this);
        }
        
        /// <summary>
        /// 현재 로딩 상태 가져오기
        /// </summary>
        public LoadingPreparationState GetLoadingState()
        {
            return syncLoadingState.Value;
        }
        
        public override void OnStopClient()
        {
            syncLoadingState.OnChange -= OnLoadingStateChangedCallback;
            LogManager.Log(LogCategory.System, "LoadingPreparationManager 클라이언트 정지", this);
        }
        
        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                LogManager.Log(LogCategory.System, "LoadingPreparationManager 인스턴스 해제됨", this);
            }
        }
    }
    
    /// <summary>
    /// 로딩 씬 전처리 상태
    /// </summary>
    public enum LoadingPreparationState
    {
        WaitingForPlayers,      // 플레이어 대기
        AssigningRoles,         // 역할 배정 중
        LoadingResources,       // 리소스 로딩 중
        LoadingStage,           // 스테이지 씬 로딩 중
        Completed               // 전처리 완료
    }

}
