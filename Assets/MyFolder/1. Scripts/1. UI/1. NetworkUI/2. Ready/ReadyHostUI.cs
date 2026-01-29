using MyFolder._1._Scripts._3._SingleTone;
using MyFolder._1._Scripts._3._SingleTone.GameSetting;
using MyFolder._1._Scripts._4._Network;
using UnityEngine;
using UnityEngine.UI;

namespace MyFolder._1._Scripts._1._UI._1._NetworkUI._2._Ready
{
    public class ReadyHostUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button startGameButton;

        [Header("Setting")] 
        [SerializeField] private GameSettings currentSettings;
        void Start()
        {
            if (!GameNetworkManager.Instance.IsHost())
            {
                gameObject.SetActive(false);
                return;
            }

            Invoke(nameof(FindGameManager),0.5f);
            InitialzeUI();
        }
    
        void InitialzeUI()
        {
            startGameButton.onClick.AddListener(OnStartGameClicked);

            UpdateUIFromSettings();
        }

        void FindGameManager()
        {
            if(GameSessionManager.Instance)
            {
                GameSessionManager.Instance.OnPlayerCountChanged += UpdatePlayerCount;
            }
        }
    
        private void OnStartGameClicked()
        {
            if (GameSettingManager.Instance)
                GameSettingManager.Instance.RequestStartGame();
        }

    
        private void UpdateUIFromSettings()
        {
            GameSettingManager.Instance?.GetCurrentSettings();
        }

        private void UpdatePlayerCount(int count,int max)
        {
            startGameButton.interactable = count >= 2;
        }

        private void OnDestroy()
        {
            if (GameSessionManager.Instance)
            {
                GameSessionManager.Instance.OnPlayerCountChanged -= UpdatePlayerCount;
            }
        }
    }
}
