using System.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using MyFolder._1._Scripts._3._SingleTone;
using MyFolder._1._Scripts._9._Vivox;
using Steamworks;
using UnityEngine;

namespace MyFolder._1._Scripts._1._UI._1._NetworkUI._2._Ready
{
    public class PlayerListUI : MonoBehaviour
    {
        private Dictionary<ulong, Texture2D> steamAvatars = new();
        private GameSessionManager sessionManager;
        
        [SerializeField] private GameObject playerListContainer;
        [SerializeField] private PlayerContextInfo playerListPrefab;
        [SerializeField] private Texture2D defaultProfileImage; // 기본 프로필 이미지
        private Dictionary<int, PlayerContextInfo> playerContexts = new();
        
        private void Start()
        {
            // 플레이어 설정 변경 이벤트 구독 (중복 제거)
            PlayerSettingManager.OnPlayerSettingsChanged += OnPlayerSettingsChanged;
            PlayerSettingManager.OnPlayerDisconnected += PlayerDisconnection;

            VivoxManager.Instance.RosterAdded +=VivoxRosterAdded;
            VivoxManager.Instance.RosterRemoved +=VivoxRosterAdded;
            
            // 초기 플레이어 목록 로드
            RefreshAllPlayerList();
            VivoxRosterUpdateInit();
        }

        #region Vivox

        private void VivoxRosterUpdateInit()
        {
            List<RosterItem> rosterItems = VivoxManager.Instance.GetRostersAsync();
            
            if(rosterItems == null || rosterItems.Count == 0)
                return;
            foreach (RosterItem item in rosterItems)
            {
                PlayerContextInfo info = playerContexts.FirstOrDefault(p => p.Value.DisplayName == item.Participant.DisplayName).Value;
                if(info != default)                
                    info.SetVivoxRoster(item);
            }
        }
        private void VivoxRosterAdded(RosterItem rosterItems)
        {
            PlayerContextInfo info = playerContexts
                .FirstOrDefault(p => p.Value.DisplayName == rosterItems.Participant.DisplayName).Value;
            if(info != default)
                info.SetVivoxRoster(rosterItems);
        }

        private void VivoxRosterRemoved(RosterItem rosterItems)
        {
            PlayerContextInfo info = playerContexts
                .FirstOrDefault(p => p.Value.DisplayName == rosterItems.Participant.DisplayName).Value;
            info.RemoveVivoxRoster();
        }
        #endregion
        
        private void RefreshAllPlayerList()
        {
            if (PlayerSettingManager.Instance)
            {
                var settings = PlayerSettingManager.Instance.GetAllPlayerSettings();
                foreach (KeyValuePair<int, PlayerSettingManager.PlayerSettings> keyValuePair in settings)
                {
                    OnPlayerSettingsChanged(keyValuePair.Key);
                }   
            }
        }

        private void OnPlayerSettingsChanged(int clientId)
        {
            var settings = PlayerSettingManager.Instance.GetPlayerSettings(clientId);
            if (settings == null) return;

            // UI 컨텍스트 생성 또는 가져오기
            PlayerContextInfo context;
            if (playerContexts.ContainsKey(clientId))
            {
                context = playerContexts[clientId];
            }
            else
            {
                context = Instantiate(playerListPrefab, playerListContainer.transform);
                playerContexts[clientId] = context;
            }

            // 플레이어 정보 업데이트
            if(PlayerSettingManager.Instance.GetLocalPlayerSettings().clientId == clientId)
                context.SetOwner();
            context.SetClientId(clientId);
            context.SetName(settings.playerName);
            
            // 스팀 아바타 로드 시도
            if (settings.steamId != 0)
            {
                LoadSteamAvatar(settings.steamId, clientId);
            }
            else
            {
                // Steam ID 없으면 기본 이미지 사용
                context.SetProfile(defaultProfileImage);
            }
        }
        
        private void LoadSteamAvatar(ulong steamId, int clientId)
        {
            // 이미 캐시된 아바타가 있으면 즉시 적용
            if (steamAvatars.TryGetValue(steamId, out Texture2D cachedAvatar))
            {
                UpdatePlayerProfile(clientId, cachedAvatar);
                return;
            }
            
            // Steam이 초기화되지 않았으면 기본 이미지 사용
            if (!SteamManager.Initialized)
            {
                UpdatePlayerProfile(clientId, defaultProfileImage);
                return;
            }
            
            CSteamID steamID = new CSteamID(steamId);
            
            // 아바타 이미지 핸들 가져오기
            int avatarHandle = SteamFriends.GetMediumFriendAvatar(steamID);
            
            if (avatarHandle > 0)
            {
                // 아바타 이미지 로드
                StartCoroutine(LoadAvatarCoroutine(avatarHandle, steamId, clientId));
            }
            else
            {
                // 아바타 핸들이 유효하지 않으면 기본 이미지 사용
                UpdatePlayerProfile(clientId, defaultProfileImage);
            }
        }
        
        private IEnumerator LoadAvatarCoroutine(int avatarHandle, ulong steamId, int clientId)
        {
            yield return new WaitForEndOfFrame();
            
            // 아바타 이미지 데이터 가져오기
            uint width, height;
            if (SteamUtils.GetImageSize(avatarHandle, out width, out height))
            {
                byte[] imageBuffer = new byte[width * height * 4];
                if (SteamUtils.GetImageRGBA(avatarHandle, imageBuffer, (int)(width * height * 4)))
                {
                    // 이미지 데이터를 수직으로 뒤집기
                    byte[] flippedBuffer = FlipImageVertically(imageBuffer, (int)width, (int)height);
                    
                    // Texture2D 생성
                    Texture2D avatarTexture = new Texture2D((int)width, (int)height, TextureFormat.RGBA32, false);
                    avatarTexture.LoadRawTextureData(flippedBuffer);
                    avatarTexture.Apply();

                    // 캐시에 저장
                    steamAvatars[steamId] = avatarTexture;
                    
                    // UI 업데이트
                    UpdatePlayerProfile(clientId, avatarTexture);
                }
                else
                {
                    // 이미지 데이터 가져오기 실패 시 기본 이미지
                    UpdatePlayerProfile(clientId, defaultProfileImage);
                }
            }
            else
            {
                // 이미지 크기 가져오기 실패 시 기본 이미지
                UpdatePlayerProfile(clientId, defaultProfileImage);
            }
        }

        private void UpdatePlayerProfile(int clientId, Texture2D profileImage)
        {
            if (playerContexts.TryGetValue(clientId, out PlayerContextInfo context))
            {
                context.SetProfile(profileImage);
            }
        }

        private void PlayerDisconnection(int clientId)
        {
            if (playerContexts.TryGetValue(clientId, out PlayerContextInfo context))
            {
                // UI GameObject 파괴
                Destroy(context.gameObject);
                playerContexts.Remove(clientId);
            }
        }
        
        private void OnDestroy()
        {
            // 이벤트 구독 해제
            PlayerSettingManager.OnPlayerSettingsChanged -= OnPlayerSettingsChanged;
            PlayerSettingManager.OnPlayerDisconnected -= PlayerDisconnection;
            
            // Texture2D 메모리 정리
            foreach (var avatar in steamAvatars.Values)
            {
                if (avatar != null)
                {
                    Destroy(avatar);
                }
            }
            steamAvatars.Clear();
        }

        // 이미지를 수직으로 뒤집는 메서드
        private byte[] FlipImageVertically(byte[] originalData, int width, int height)
        {
            byte[] flippedData = new byte[originalData.Length];
            int bytesPerPixel = 4; // RGBA32
    
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int originalIndex = (y * width + x) * bytesPerPixel;
                    int flippedIndex = ((height - 1 - y) * width + x) * bytesPerPixel;
            
                    // RGBA 값 복사
                    flippedData[flippedIndex] = originalData[originalIndex];         // R
                    flippedData[flippedIndex + 1] = originalData[originalIndex + 1]; // G
                    flippedData[flippedIndex + 2] = originalData[originalIndex + 2]; // B
                    flippedData[flippedIndex + 3] = originalData[originalIndex + 3]; // A
                }
            }
    
            return flippedData;
        }

    }
}