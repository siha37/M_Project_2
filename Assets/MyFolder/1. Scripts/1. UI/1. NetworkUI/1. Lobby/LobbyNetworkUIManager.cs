using System;
using System.Collections;
using System.Collections.Generic;
using FishNet;
using FishNet.Managing;
using FishNet.Managing.Scened;
using MyFolder._1._Scripts._3._SingleTone;
using MyFolder._1._Scripts._4._Network;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SceneManager = UnityEngine.SceneManagement.SceneManager;

namespace MyFolder._1._Scripts._1._UI._1._NetworkUI._1._Lobby
{
    public class LobbyNetworkUIManager : MonoBehaviour
    {
        private const string nextScene = "LoadingRoom";

        [Header("방 생성")]
        [SerializeField] private TMP_InputField RoomNameInput;
        [SerializeField] private TMP_Dropdown RoomMaxPlayersInput;
        [SerializeField] private Toggle IsPrivateToggle;
        [SerializeField] private Button CreateRoomButton;

        [Header("방 목록")]
        [SerializeField] private Button RoomListRefreshButton;

        [Header("방 ID로 참가")]
        [SerializeField] private TMP_InputField RoomIdInput;
        [SerializeField] private Button JoinByIdButton;
        
        [Header("방 목록 오브젝트")]
        [SerializeField] private Transform RoomListContainer;
        [SerializeField] private RoomItemController RoomItemController;

        
        RoomManager roomManager;

        void Start()
        {
            roomManager = RoomManager.Instance;
            if (roomManager)
            {
                roomManager.OnRoomListReceived += OnRoomListRefresh;
                
                CreateRoomButton.onClick.AddListener(CreateRoomButtonClick);
                
                RoomListRefreshButton.onClick.AddListener(RoomListRefreshButtonClick);
                
                JoinByIdButton.onClick.AddListener(JoinByIdButtonClick);
                
            }            
        }

        private async void CreateRoomButtonClick()
        {
            string roomName = RoomNameInput.text;

            // 방 이름 유효성 검사
            if (string.IsNullOrWhiteSpace(roomName))
            {
                AlertManager.Instance.ShowWarning("방 정보가 비정상입니다.");
                return;
            }


            int maxPlayers = RoomMaxPlayersInput.value + ServicesConfig.DEFAULT_ROOM_MIN_PLAYERS;
            maxPlayers = Mathf.Clamp(maxPlayers, ServicesConfig.DEFAULT_ROOM_MIN_PLAYERS, ServicesConfig.DEFAULT_ROOM_MAX_PLAYERS);
            var (success, roomId) = await roomManager.CreateRoomAsync(roomName, maxPlayers, IsPrivateToggle.isOn);
            if (success)
            {
                StartCoroutine(nameof(WaitFlowManager));
            }
            else
            {
                AlertManager.Instance.ShowError("방 생성에 실패했습니다. 다시 시도해주세요.");
            }
        }

        private async void JoinByIdButtonClick()
        {
            if (string.IsNullOrWhiteSpace(RoomIdInput.text))
            {
                AlertManager.Instance.ShowWarning("방 코드를 입력해주세요.");
                return;
            }

            var roomInfo = await RoomManager.Instance.GetRoomInfoByJoinCodeAsync(RoomIdInput.text);
            if (roomInfo != null)
            {
                if (!roomInfo.isJoinable)
                {
                    AlertManager.Instance.ShowError("방이 준비되지 않았습니다.");
                    return;
                }
            }
            else
            {
                AlertManager.Instance.ShowError("방 정보가 없습니다.");
            }

            bool joinSuccess = await roomManager.JoinPrivateRoomByCodeAsync(RoomIdInput.text);
            if (joinSuccess)
            {
                StartCoroutine(nameof(WaitFlowManager));
            }
            else
            {
                AlertManager.Instance.ShowError("방 참가에 실패했습니다.\n방 코드를 확인해주세요.");
            }
        }

        private async void RoomListRefreshButtonClick()
        {
            await roomManager.RefreshRoomListAsync();
        }

        public void OnDestroy()
        {
            
            roomManager.OnRoomListReceived -= OnRoomListRefresh;
        }

        private void OnRoomListRefresh(List<RoomInfo> roomList)
        {
            // ✅ 기존 방 목록 아이템들 모두 제거
            for (int i = RoomListContainer.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = RoomListContainer.transform.GetChild(i);
                Destroy(child.gameObject);
            }

            for (int i = 0; i < roomList.Count; i++)
            {
                //비공개 룸 목록에 미표기
                if(roomList[i].isPrivate)
                    continue;
                RoomItemController controller  =  Instantiate(RoomItemController, RoomListContainer);
                controller.SetRoomData(roomList[i],this);
            }
        }

        public async void OnJoinRoomButtonClicked(string roomId)
        {
            // 1. 최신 룸 정보 조회
            var roomInfo = await roomManager.GetRoomInfoAsync(roomId);
            
            if (roomInfo == null)
            {
                AlertManager.Instance.ShowError("방 정보를 찾을 수 없습니다.");
                return;
            }

            if (!roomInfo.isJoinable)
            {
                AlertManager.Instance.ShowError("방이 준비되지 않았습니다.");
                return;
            }
            
            // 2. 참가 가능 여부 체크
            if (roomInfo.currentPlayers >= roomInfo.maxPlayers)
            {
                // 정보 오차 존재 룸 정보 강제 새로고침
                await roomManager.RefreshRoomListAsync();
                AlertManager.Instance.ShowWarning(
                    $"방 '{roomInfo.roomName}'이 가득 찼습니다.\n({roomInfo.currentPlayers}/{roomInfo.maxPlayers})"
                );
                return;
            }
            
            // 3. 참가 시도
            LogManager.Log(LogCategory.Lobby, $"룸 '{roomInfo.roomName}' 참가 시도... ({roomInfo.currentPlayers}/{roomInfo.maxPlayers})");
            
            bool success = await roomManager.JoinRoomAsync(roomId);
            
            if (success)
            {
                StartCoroutine(nameof(WaitFlowManager));
            }
            else
            {
                AlertManager.Instance.ShowError("방 참가에 실패했습니다.");
            }
        }

        private IEnumerator WaitFlowManager()
        {
            while(!NetworkFlowManager.Instance)
                yield return new WaitForSeconds(0.1f);
            NetworkFlowManager.Instance.LoadSceneForClient(InstanceFinder.ClientManager.Connection,nextScene);
        } 
    }
}