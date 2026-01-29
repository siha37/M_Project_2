using System;
using MyFolder._1._Scripts._3._SingleTone;
using UnityEngine;

namespace MyFolder._1._Scripts._4._Network
{
    /// <summary>
    /// 네트워크 상태를 중앙 집중식으로 관리
    /// </summary>
    public class NetworkStateManager : SingleTone<NetworkStateManager>
    {
        [Header("네트워크 상태")]
        [SerializeField] private NetworkState currentState = NetworkState.Disconnected;
        [SerializeField] private bool debugMode = true;
        
        // 공통 상태 정보
        public NetworkState CurrentState => currentState;
        public string CurrentUserId { get; private set; }
        public string CurrentLobbyId { get; private set; }
        public bool IsHost { get; private set; }
        public string LastError { get; private set; }
        
        // 상태 변경 이벤트
        public event Action<NetworkState, NetworkState> OnStateChanged;
        public event Action<string> OnErrorOccurred;
        public event Action<string> OnUserIdChanged;
        public event Action<string, bool> OnLobbyChanged; // lobbyId, isHost
        
        /// <summary>
        /// 상태 변경 (모든 관리자가 이 메서드를 통해 상태 변경)
        /// </summary>
        public void ChangeState(NetworkState newState, string context = "")
        {
            var oldState = currentState;
            currentState = newState;
            
            if (debugMode)
            {
                LogManager.Log(LogCategory.Network, 
                    $"상태 변경: {oldState} → {newState} ({context})", this);
            }
            
            OnStateChanged?.Invoke(oldState, newState);
        }
        
        public void SetUserId(string userId)
        {
            CurrentUserId = userId;
            OnUserIdChanged?.Invoke(userId);
        }
        
        public void SetLobbyInfo(string lobbyId, bool isHost)
        {
            CurrentLobbyId = lobbyId;
            IsHost = isHost;
            OnLobbyChanged?.Invoke(lobbyId, isHost);
        }
        
        public void SetError(string error)
        {
            LastError = error;
            LogManager.LogError(LogCategory.Network,error);
            OnErrorOccurred?.Invoke(error);
        }
        
        public void ClearLobby()
        {
            CurrentLobbyId = null;
            IsHost = false;
            OnLobbyChanged?.Invoke(null, false);
        }
    }
    
    public enum NetworkState
    {
        Disconnected,       // 완전 연결 해제
        Authenticating,     // 인증 중
        Connected,          // 기본 연결됨
        CreatingLobby,      // 로비 생성 중
        JoiningLobby,       // 로비 참가 중
        InLobby,           // 로비 안에 있음
        StartingGame,       // 게임 시작 중
        InGame             // 게임 중
    }
}