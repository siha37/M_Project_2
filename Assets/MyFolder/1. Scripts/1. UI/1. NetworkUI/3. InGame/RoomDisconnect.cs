using MyFolder._1._Scripts._4._Network;
using UnityEngine;

namespace MyFolder._1._Scripts._1._UI._1._NetworkUI._3._InGame
{
    public class RoomDisconnect : MonoBehaviour
    {
        public void Disconnect()
        {
            GameNetworkManager.Instance.Disconnect();
        }
    }
}
