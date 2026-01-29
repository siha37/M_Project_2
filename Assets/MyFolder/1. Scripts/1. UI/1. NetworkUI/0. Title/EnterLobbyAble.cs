using System;
using MyFolder._1._Scripts._0._System.Bootstrap;
using MyFolder._1._Scripts._4._Network;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MyFolder._1._Scripts._1._UI._1._NetworkUI._0._Title
{
    public class EnterLobbyAble : MonoBehaviour
    {
        [SerializeField] Button enterLobbyButton;
        [SerializeField] TextMeshProUGUI enterLobbyButtonText;
        
        [SerializeField] NetworkStateManager NetworkManager;
        [SerializeField] PreTitleCsvBootstrap preTitleCsvBootstrap;

        public void Update()
        {
            if(!NetworkManager)
            {
                NetworkManager = NetworkStateManager.Instance;
                enterLobbyButton.interactable = false;
                return;
            }
            enterLobbyButtonText.text = NetworkManager.CurrentState.ToString();
            if (NetworkManager.CurrentState == NetworkState.Connected && preTitleCsvBootstrap.downloadOnFinish)
            {
                enterLobbyButton.interactable = true;
            }
            else
            {
                enterLobbyButton.interactable = false;
            }
        }
    }
}
