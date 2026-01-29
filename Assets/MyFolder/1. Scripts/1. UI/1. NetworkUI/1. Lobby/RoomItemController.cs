using MyFolder._1._Scripts._3._SingleTone;
using MyFolder._1._Scripts._4._Network;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace MyFolder._1._Scripts._1._UI._1._NetworkUI._1._Lobby
{
    public class RoomItemController : MonoBehaviour
    {
    
    
        [Header("UI References")]
        public TextMeshProUGUI roomNameText;
        public Button joinButton;
    
        [Header("Room Data")]
        public string roomId;
        public RoomInfo roomInfo;
    
        [Header("Visual Settings")]
        public Color normalColor = Color.white;
        public Color hoverColor = new Color(0.9f, 0.9f, 1f, 1f);
        public Color selectedColor = new Color(0.7f, 0.7f, 1f, 1f);
    
    
        private LobbyNetworkUIManager networkUIManager;
    
        void Start()
        {
            InitializeUI();
        }
        void InitializeUI()
        {
            if (joinButton)
            {
                joinButton.onClick.AddListener(OnJoinButtonClicked);
            }
        }
    
    
        public void SetRoomData(RoomInfo info,LobbyNetworkUIManager _networkUIManager)
        {
            roomInfo = info;
            roomId = info.roomId;
            networkUIManager = _networkUIManager;
            
            // 디버그: 방 정보 로그 출력
            LogManager.Log(LogCategory.UI, $"RoomItemController.SetRoomData - 방: {info.roomName}, 플레이어: {info.currentPlayers}/{info.maxPlayers}, 상태: {info.status}");
            
            UpdateUI();
        }
    
        void UpdateUI()
        {
            if (roomInfo == null) return;
        
            // 방 이름 업데이트
            if (roomNameText)
            {
                roomNameText.text = $"{roomInfo.roomName} ({roomInfo.currentPlayers}/{roomInfo.maxPlayers})";
            }
        
            // 참가 버튼 상태 업데이트 (단순화)
            if (joinButton)
            {
                // 단순한 로직: 플레이어 수만 체크
                bool canJoin = roomInfo.currentPlayers < roomInfo.maxPlayers;
                joinButton.interactable = canJoin;
            
                var buttonText = joinButton.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText)
                {
                    if (roomInfo.currentPlayers >= roomInfo.maxPlayers)
                    {
                        buttonText.text = "만원";
                    }
                    else
                    {
                        buttonText.text = "참가";
                    }
                }
            }
        }
    
        void OnJoinButtonClicked()
        {
            if (string.IsNullOrEmpty(roomId))
            {
                LogManager.LogWarning(LogCategory.UI, "방 ID가 없습니다!");
                return;
            }
            networkUIManager.OnJoinRoomButtonClicked(roomId);
        }

        
        void OnDestroy()
        {
            if (joinButton)
            {
                joinButton.onClick.RemoveListener(OnJoinButtonClicked);
            }
        }
    }
} 