using System;
using System.Collections.Generic;
using FishNet.Managing.Scened;
using MyFolder._1._Scripts._3._SingleTone;
using MyFolder._1._Scripts._7._PlayerRole;
using TMPro;
using UnityEngine;

namespace MyFolder._1._Scripts._0._System.End
{
    public class EndWinPlayerListup : MonoBehaviour
    {
        [SerializeField] private GameObject _playerNamePrefab;
        
        // 우승한 역할 타입
        [SerializeField] private PlayerRoleType winningRole;
        
        // 우승 플레이어 이름 목록
        private List<string> winningPlayerNames = new List<string>();
        
        
        private void Start()
        {
            // 우승 플레이어 목록 가져오기
            LoadWinningPlayers();
            ListUp();
        }
        
        /// <summary>
        /// 우승 역할에 해당하는 모든 플레이어 이름 가져오기
        /// </summary>
        private void LoadWinningPlayers()
        {
            winningPlayerNames.Clear();
            
            if (!PlayerSettingManager.Instance)
            {
                LogManager.LogWarning(LogCategory.System, 
                    "PlayerSettingManager를 찾을 수 없습니다.", this);
                return;
            }
            
            // 모든 플레이어 설정 가져오기
            var allPlayerSettings = PlayerSettingManager.Instance.GetAllPlayerSettings();
            
            foreach (var settings in allPlayerSettings)
            {
                // 우승 역할과 일치하는 플레이어만 추가
                if (settings.Value.role == winningRole)
                {
                    string playerName = settings.Value.playerName;
                    
                    // 이름이 비어있으면 기본값 사용
                    if (string.IsNullOrEmpty(playerName))
                    {
                        playerName = $"Player {settings.Value.clientId}";
                    }
                    
                    winningPlayerNames.Add(playerName);
                }
            }
        }

        private void ListUp()
        {
            winningPlayerNames.ForEach(p =>
            {
                GameObject obj = Instantiate(_playerNamePrefab, transform);
                obj.transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().text = p;
            });
        }
    }
}
