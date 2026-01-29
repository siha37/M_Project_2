using System;
using System.Collections;
using FishNet;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Managing.Server;
using FishNet.Transporting;
using MyFolder._1._Scripts._4._Network;
using MyFolder._1._Scripts._8999._Utility.Corutin;
using UnityEngine;

namespace MyFolder._1._Scripts._3._SingleTone
{
    /// <summary>
    /// 게임 세션 관리자 - 플레이어 수 관리 및 게임 종료 처리
    /// </summary>
    public class GameSessionManager : NetworkBehaviour
    {
        // ✅ 싱글톤 인스턴스
        public static GameSessionManager Instance { get; private set; }
        
        [Header("게임 세션 설정")]
        [SerializeField] private int minPlayersRequired = 1;
        [SerializeField] private float gameEndDelay = 3f; // 게임 종료 전 대기 시간
        
        public int minPlayers => minPlayersRequired;
        // 동기화된 플레이어 수
        private readonly SyncVar<int> syncPlayerCount = new SyncVar<int>();
        private readonly SyncVar<bool> syncIsGameActive = new SyncVar<bool>();
        
        // 이벤트
        public event System.Action<int,int> OnPlayerCountChanged;
        public event System.Action OnGameEnded;
        public event System.Action<string> OnGameEndedWithReason; // 종료 사유 포함
        
        // 게임 상태
        public int PlayerCount => syncPlayerCount.Value;
        public bool IsGameActive => syncIsGameActive.Value;
        
        private void Awake()
        {
            if (!Instance)
            {
                Instance = this;
                LogManager.Log(LogCategory.System, "GameSessionManager 인스턴스 생성 완료", this);
            }
            else if (Instance != this)
            {
                LogManager.LogWarning(LogCategory.System, "GameSessionManager 중복 인스턴스 제거", this);
                Destroy(gameObject);
            }
        }
        
        public override void OnStartServer()
        {
            syncPlayerCount.Value = 1;
            syncIsGameActive.Value = false;
            
            // ✅ 서버 연결/해제 이벤트 자동 구독
            if (NetworkManager?.ServerManager)
            {
                NetworkManager.ServerManager.OnRemoteConnectionState += OnRemoteConnectionState;
                NetworkManager.ServerManager.OnServerConnectionState += OnServerConnectionState;
            }
            
            LogManager.Log(LogCategory.System, "GameSessionManager 서버 초기화 완료", this);
        }
        
        public override void OnStartClient()
        {
            syncPlayerCount.OnChange += OnPlayerCountChangedCallback;
            syncIsGameActive.OnChange += OnIsGameActiveChangedCallback;
            LogManager.Log(LogCategory.System, "GameSessionManager 클라이언트 초기화 완료", this);
        }
        
        public override void OnStopClient()
        {
            syncPlayerCount.OnChange -= OnPlayerCountChangedCallback;
            syncIsGameActive.OnChange -= OnIsGameActiveChangedCallback;
        }
        
        public override void OnStopServer()
        {
            // ✅ 이벤트 구독 해제
            if (NetworkManager?.ServerManager != null)
            {
                NetworkManager.ServerManager.OnRemoteConnectionState -= OnRemoteConnectionState;
                NetworkManager.ServerManager.OnServerConnectionState -= OnServerConnectionState;
            }
            
            LogManager.Log(LogCategory.System, "GameSessionManager 서버 정지", this);
        }
        
        /// <summary>
        /// 게임 세션 시작
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void StartGameSessionServerRpc()
        {
            if (syncPlayerCount.Value < minPlayersRequired)
            {
                LogManager.LogWarning(LogCategory.System, $"최소 {minPlayersRequired}명의 플레이어가 필요합니다", this);
                return;
            }
            
            syncIsGameActive.Value = true;
            LogManager.Log(LogCategory.System, $"게임 세션 시작 - 플레이어 수: {syncPlayerCount.Value}명", this);
        }
        
        /// <summary>
        /// 플레이어 수 업데이트 (수동 호출용)
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void UpdatePlayerCountServerRpc()
        {
            UpdatePlayerCountAutomatically();
        }
        
        /// <summary>
        /// 게임 종료 조건 체크
        /// </summary>
        private void CheckGameEndConditions()
        {
            if (syncPlayerCount.Value < minPlayersRequired)
            {
                LogManager.Log(LogCategory.System, $"최소 플레이어 수 미달로 게임 종료: {syncPlayerCount.Value}명", this);
                EndGameSessionServerRpc("플레이어 수 부족으로 게임이 종료됩니다.");
            }
        }
        
        /// <summary>
        /// 게임 세션 종료
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void EndGameSessionServerRpc(string reason = "게임이 종료되었습니다.")
        {
            if (!syncIsGameActive.Value) return;
            
            LogManager.Log(LogCategory.System, $"게임 세션 종료: {reason}", this);
            syncIsGameActive.Value = false;
            
            // 지연 후 게임 종료 알림
            StartCoroutine(EndGameWithDelay(reason));
        }
        
        /// <summary>
        /// 지연 후 게임 종료 처리
        /// </summary>
        private IEnumerator EndGameWithDelay(string reason)
        {
            yield return WaitForSecondsCache.Get(gameEndDelay);
            
            NotifyGameEndedClientRpc(reason);
        }
        
        /// <summary>
        /// 클라이언트에게 게임 종료 알림
        /// </summary>
        [ObserversRpc]
        private void NotifyGameEndedClientRpc(string reason)
        {
            LogManager.Log(LogCategory.System, $"게임 종료 알림: {reason}", this);
            OnGameEnded?.Invoke();
            OnGameEndedWithReason?.Invoke(reason);
        }
        
        /// <summary>
        /// 플레이어 수 변경 콜백
        /// </summary>
        private void OnPlayerCountChangedCallback(int previousValue, int newValue, bool asServer)
        {
            OnPlayerCountChanged?.Invoke(newValue,RoomManager.Instance.CustomMaxPlayers);
        }
        
        /// <summary>
        /// 게임 활성 상태 변경 콜백
        /// </summary>
        private void OnIsGameActiveChangedCallback(bool previousValue, bool newValue, bool asServer)
        {
            LogManager.Log(LogCategory.System, $"게임 활성 상태 변경: {previousValue} → {newValue}", this);
        }
        
        /// <summary>
        /// 수동으로 게임 종료 요청
        /// </summary>
        public void RequestEndGame(string reason = "수동으로 게임이 종료되었습니다.")
        {
            if (!IsHostInitialized) return;
            EndGameSessionServerRpc(reason);
        }
        
        // ✅ 클라이언트 연결 상태 변경 시 자동 호출
        private void OnRemoteConnectionState(NetworkConnection conn, RemoteConnectionStateArgs args)
        {
            if (!IsServerInitialized) return;
            
            switch (args.ConnectionState)
            {
                case RemoteConnectionState.Started:
                    LogManager.Log(LogCategory.System, $"클라이언트 {conn.ClientId} 연결됨", this);
                    break;
                case RemoteConnectionState.Stopped:
                    LogManager.Log(LogCategory.System, $"클라이언트 {conn.ClientId} 연결 해제됨", this);
                    break;
            }
            
            // 연결 상태 변경 시 플레이어 수 자동 업데이트
            UpdatePlayerCountAutomatically();
        }

        // ✅ 서버 연결 상태 변경 시 자동 호출
        private void OnServerConnectionState(ServerConnectionStateArgs args)
        {
            if (!IsServerInitialized) return;
            
            switch (args.ConnectionState)
            {
                case LocalConnectionState.Started:
                    LogManager.Log(LogCategory.System, "서버 시작됨", this);
                    break;
                case LocalConnectionState.Stopped:
                    LogManager.Log(LogCategory.System, "서버 정지됨", this);
                    break;
            }
            
            // 서버 상태 변경 시에도 플레이어 수 업데이트
            UpdatePlayerCountAutomatically();
        }

        /// <summary>
        /// 플레이어 수 자동 업데이트 (이벤트 기반)
        /// </summary>
        private void UpdatePlayerCountAutomatically()
        {
            if (!IsServerInitialized) return;
            
            int currentCount = base.NetworkManager.ServerManager.Clients.Count;
            
            if (syncPlayerCount.Value != currentCount)
            {
                LogManager.Log(LogCategory.System, $"플레이어 수 자동 변경: {syncPlayerCount.Value} → {currentCount}", this);
                syncPlayerCount.Value = currentCount;
                
                // 게임 진행 중이면 종료 조건 체크
                if (syncIsGameActive.Value)
                {
                    CheckGameEndConditions();
                }
            }
        }
        
        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
