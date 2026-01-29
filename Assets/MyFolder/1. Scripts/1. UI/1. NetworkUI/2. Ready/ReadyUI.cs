using System.Collections.Generic;
using FishNet;
using MyFolder._1._Scripts._3._SingleTone;
using MyFolder._1._Scripts._4._Network;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MyFolder._1._Scripts._1._UI._1._NetworkUI._2._Ready
{
    public class ReadyUI : MonoBehaviour
    {
        [SerializeField] private Image maxPlayerSlider;
        [SerializeField] private TextMeshProUGUI playerCountText;

        [SerializeField] private TextMeshProUGUI CurrentCharacterNameText;
        [SerializeField] private List<Button> Character_buttons;
        
        [SerializeField] private Button DisconnectButton;
        void Start()
        {
            Invoke(nameof(FindGameManager),2f);
            OnClick_CharacterButton_Setting();
            DisconnectButton.onClick.AddListener(Disconnect);
        }
        
        void FindGameManager()
        {
            if(GameSessionManager.Instance)
            {
                GameSessionManager.Instance.OnPlayerCountChanged += UpdatePlayerCount;
                UpdatePlayerCount(GameSessionManager.Instance.PlayerCount,RoomManager.Instance.CustomMaxPlayers);
            }
            else
            {
                Invoke(nameof(FindGameManager),2f);
            }
        }
        
        private void UpdatePlayerCount(int count,int max)
        {
            playerCountText.text = $"플레이어: {count}/{max}";
            maxPlayerSlider.fillAmount = (float)count/max;
        }
        
        private void OnDestroy()
        {
            if (GameSessionManager.Instance)
            {
                GameSessionManager.Instance.OnPlayerCountChanged -= UpdatePlayerCount;
            }
        }

        
        
        private void OnClick_CharacterButton_Setting()
        {
            ushort id = 1;
            if (GameDataManager.Instance)
                CurrentCharacterNameText.text = GameDataManager.Instance.GetPlayerDataById(1).name;
            foreach (Button button in Character_buttons)
            {
                var id1 = id;
                button.onClick.AddListener(() => { SetPlayerDataId(id1); });
                button.transform.GetChild(1).TryGetComponent(out TextMeshProUGUI text);
                if(GameDataManager.Instance)
                {
                    text.text = GameDataManager.Instance.GetPlayerDataById(id1).name;
                }
                id++;
            }
        }
        
        /// <summary>
        /// 오너의 data index 변경
        /// </summary>
        public void SetPlayerDataId(ushort playerDataId)
        {
            if (InstanceFinder.NetworkManager?.ClientManager && PlayerSettingManager.Instance && PlayerSettingManager.Instance.IsSettingsReady)
            {
                LogManager.Log(LogCategory.Player,"Data Id Changed",this);
                int clientid = InstanceFinder.NetworkManager.ClientManager.Connection.ClientId;
                PlayerSettingManager.Instance.SetPlayerDataIdServerRpc(clientid,playerDataId);
                
                if (GameDataManager.Instance)
                    CurrentCharacterNameText.text = GameDataManager.Instance.GetPlayerDataById(playerDataId).name;
            }
            else
            {
                LogManager.LogWarning(LogCategory.Player,"Data Id 변경 불가",this);
            }
        }

        private void Disconnect()
        {
            GameNetworkManager.Instance.Disconnect();
        }
    }
}