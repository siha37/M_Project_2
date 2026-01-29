using System;
using System.Collections.Generic;
using MyFolder._1._Scripts._4._Network;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MyFolder._1._Scripts._1._UI._1._NetworkUI._2._Ready
{
    public class RoomCodeUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI roomCodeText;
    
        [SerializeField] private Button IDCopyButton;

        private string roomCode;
        public void Start()
        {
            roomCodeText.text = RoomManager.Instance.GetCurrentRoom().LobbyCode;
            roomCode = RoomManager.Instance.GetCurrentRoom().LobbyCode;
            IDCopyButton.onClick.AddListener(IdCopyButtonOnClick);
        }

        private void IdCopyButtonOnClick()
        {
            GUIUtility.systemCopyBuffer = roomCode;
        }
    }
}
