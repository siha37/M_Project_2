using System;
using UnityEngine;

namespace MyFolder._1._Scripts._3._SingleTone
{
    public class GameShutdownHandler : SingleTone<GameShutdownHandler>
    {
        public static event Action OnShutdown;

        private void OnApplicationQuit()
        {
            OnShutdown?.Invoke();
        }

        private void OnDestroy()
        {
            OnShutdown?.Invoke();
        }
    }
}
