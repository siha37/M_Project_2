using FishNet.Managing.Client;
using FishNet.Managing.Server;
using UnityEngine;

namespace MyFolder._1._Scripts._0._System
{
    public class FramRateInit : MonoBehaviour
    {
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Awake()
        {
            Application.targetFrameRate = 60;
        }
    }
}
