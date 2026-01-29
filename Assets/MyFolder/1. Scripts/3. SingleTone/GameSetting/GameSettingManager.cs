using System;
using System.Collections;
using System.Collections.Generic;
using FishNet;
using FishNet.Managing.Scened;
using FishNet.Object;
using MyFolder._1._Scripts._4._Network;
using MyFolder._1._Scripts._7._PlayerRole;
using MyFolder._1._Scripts._8999._Utility.Corutin;
using UnityEngine;

namespace MyFolder._1._Scripts._3._SingleTone.GameSetting
{
    [Serializable]
    public class PlayerRoleSettings
    {
        public PlayerRoleType RoleType = PlayerRoleType.Destroyer;
        public int RoleAmount = 1;
    }
    [Serializable]
    public class GameSettings
    {
        public GameSettings()
        {
            playerRoleSettings = new Dictionary<PlayerRoleType, PlayerRoleSettings>();
            playerRoleSettings.Add(PlayerRoleType.Destroyer, new PlayerRoleSettings());
        }

        // 게임 씬 이름
        private string sceneName;
        
        // 역할 설정
        private Dictionary<PlayerRoleType, PlayerRoleSettings> playerRoleSettings;
        
        // 카드 시스템 설정 추가
        // 퀘스트 실패 시 패배카드를 제거자 플레이어 최대 몇명에게 부여할지
        public int maxDestroyersForDefeatCards = 2;


        public Dictionary<PlayerRoleType, PlayerRoleSettings> PlayerRoleSettings { get => playerRoleSettings; set => playerRoleSettings = value; }
    }
    /// <summary>
    /// 게임에 관련 커스터 설정 관리 싱글톤
    /// </summary>
    public class GameSettingManager : NetworkBehaviour
    {
        // ✅ 싱글톤 인스턴스
        public static GameSettingManager Instance { get; private set; }
        
        [Header("Settings")]
        [SerializeField] private GameSettings defaultSettings = new();
        
        // 현 플레이어 인원
        private int currentPlayer = 0;
        
        // 로딩 씬 
        readonly string loadingScene = "LoadingStageScene";

        
        // 엔딩 스크립트 임시 저장용
        public string EndDescription { get;  set; }
        
        // 설정 변경 이벤트
        public Action A_RoleStateChanged;
        
        private void Awake()
        {
            // ✅ 싱글톤 패턴 구현 (DontDestroyOnLoad 제거)
            if (!Instance)
            {
                Instance = this;
                LogManager.Log(LogCategory.System, "GameSettingManager 싱글톤 인스턴스 생성", this);
            }
            else if (Instance != this)
            {
                LogManager.LogWarning(LogCategory.System, "GameSettingManager 이미 인스턴스가 존재합니다. 중복 오브젝트를 제거합니다.", this);
                Destroy(gameObject);
            }
        }

        public override void OnStartServer()
        {
            // ✅ 인스턴스가 설정되어 있는지 확인 (DontDestroyOnLoad 제거)
            if (!Instance)
            {
                Instance = this;
            }

            StartCoroutine(nameof(WaitGameSessionManager));
            
            LogManager.Log(LogCategory.System, "GameSettingManager 서버 시작 - 기본 설정 적용됨", this);
        }

        private IEnumerator WaitGameSessionManager()
        {
            while(!GameSessionManager.Instance)
            {
                yield return WaitForSecondsCache.Get(0.1f);
            }
            
            GameSessionManager.Instance.OnPlayerCountChanged += UpdatePlayerCount;
            // 초반 초기화 호출 진행
            UpdatePlayerCount(GameSessionManager.Instance.PlayerCount,RoomManager.Instance.CustomMaxPlayers);
        }
        
        public override void OnStartClient()
        {
            // ✅ 인스턴스가 설정되어 있는지 확인 (DontDestroyOnLoad 제거)
            if (!Instance)
            {
                Instance = this;
            }
            
            LogManager.Log(LogCategory.System, "GameSettingManager 클라이언트 시작 - 동기화 콜백 등록됨", this);
        }
        
        
        public void RequestStartGame()
        {
            StartGame();
        }

        private void StartGame()
        {
            
            if (NetworkManager?.ServerManager)
            {
                var clients = NetworkManager.ServerManager.Clients;
                LogManager.Log(LogCategory.System, $"연결된 클라이언트 수: {clients.Count}", this);
                foreach (var client in clients.Values)
                {
                    LogManager.Log(LogCategory.System, $"클라이언트 ID: {client.ClientId}, 활성: {client.IsActive}", this);
                }
            }
            
            // ✅ GameSessionManager를 통한 플레이어 수 체크
            if (GameSessionManager.Instance)
            {
                // 최소 플레이어 수 체크 후 게임 세션 시작
                if (GameSessionManager.Instance.PlayerCount >= GameSessionManager.Instance.minPlayers)
                {
                    LogManager.Log(LogCategory.System, "게임 시작 - 로딩 씬으로 전환", this);
                    LoadGlobalScene();
                }
                else
                {
                    LogManager.LogWarning(LogCategory.System, "최소 2명의 플레이어가 필요합니다", this);
                }
            }
            else
            {
                LogManager.LogError(LogCategory.System, "GameSessionManager를 찾을 수 없습니다", this);
            }
        }
        

        public GameSettings GetCurrentSettings()
        {
            return defaultSettings;
        }

        // ✅ 게임 상태 리셋 메서드 (삭제하지 않고 상태만 리셋)
        private void ResetGameState()
        {
            if (!IsServerInitialized) return;
            LogManager.Log(LogCategory.System, "GameSettingManager 게임 상태 리셋", this);
            defaultSettings = new GameSettings();
        }

        
        /// <summary>
        /// FishNet GlobalScene 로드
        /// </summary>
        private void LoadGlobalScene()
        {
            // ✅ 4단계: 서버에서만 FishNet GlobalScene 로드
            if (IsServerInitialized)
            {
                LogManager.Log(LogCategory.System, "FishNet GlobalScene으로 LoadingStageScene 로드 요청", this);

                
                SceneLoadData data = new SceneLoadData(new List<string> { loadingScene })
                {
                    ReplaceScenes = ReplaceOption.All
                };
                InstanceFinder.SceneManager.LoadGlobalScenes(data);
            }
        }


        /// <summary>
        /// 현재 플레이어의 인원수 변경됨을 알림
        /// </summary>
        /// <param name="count">현재 인원 수</param>
        /// <param name="max">최대 입장 가능 인원 수</param>
        private void UpdatePlayerCount(int count, int max)
        {
            currentPlayer = count;

            // 변경 후 최대값보다 현재 제거자 역할 수가 클 경우
            if (GetDestoryerMaxAmount() < GetDestroyerCurrentAmount())
            {
                SetDestroyerAmount(GetDestoryerMaxAmount());
            }
            
            A_RoleStateChanged?.Invoke();
        }
        
        #region 역할 설정

        /// <summary>
        /// 현재 가능한 제거자 역할 최대 수
        /// </summary>
        /// <returns></returns>
        public int GetDestoryerMaxAmount()
        {
            return currentPlayer - 1;
        }

        /// <summary>
        /// 현재 시민 역할 수
        /// </summary>
        /// <returns></returns>
        public int GetNormalCurrentAmount()
        {
            return currentPlayer - defaultSettings.PlayerRoleSettings[PlayerRoleType.Destroyer].RoleAmount;
        }

        /// <summary>
        /// 현재 제거자 역할 수
        /// </summary>
        /// <returns></returns>
        public int GetDestroyerCurrentAmount()
        {
            return defaultSettings.PlayerRoleSettings[PlayerRoleType.Destroyer].RoleAmount;
        }

        /// <summary>
        /// 제거자 역할의 수를 변경
        /// </summary>
        /// <param name="amount"></param>
        public bool SetDestroyerAmount(int amount)
        {
            // 제거자의 수치 0 또는 과다 시 예외 처리
            if (amount > GetDestoryerMaxAmount() || amount == 0)
            {
                return false;
            }
            else
            {
                //제거자 역할 수 적용
                defaultSettings.PlayerRoleSettings[PlayerRoleType.Destroyer].RoleAmount = amount;
                
                // 상태 변경 콜백
                A_RoleStateChanged?.Invoke();
                
                return true;
            }
        }
        
        

        #endregion

        #region 서버,게임종료 처리 로직

        public override void OnStopServer()
        {
            ResetGameState();
            
            LogManager.Log(LogCategory.System, "GameSettingManager 서버 정지 - 리소스 정리 완료", this);
        }

        private void OnDestroy()
        {
            // ✅ 인스턴스 정리
            if (Instance == this)
            {
                Instance = null;
                LogManager.Log(LogCategory.System, "GameSettingManager 인스턴스 해제됨", this);
            }
        }

        #endregion
    }
}