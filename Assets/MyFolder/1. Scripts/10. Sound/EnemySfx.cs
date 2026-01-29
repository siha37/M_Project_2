using System;
using System.Collections.Generic;
using FishNet.Object;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;

namespace MyFolder._1._Scripts._10._Sound
{    
    public enum EnemySfxType : byte
    {
        EnemyShooting,
        EnemySpawned
    }


    public class EnemySfx : ObjectSfx<EnemySfxType>
    {
        [Header("FMOD Events (3D)")]
        [SerializeField] private EventReference enemyShooting;
        [SerializeField] private EventReference enemySpawned;
        
        [Header("Min Interval (seconds)")]
        [SerializeField] private float shootingInterval = 0.05f;       // 20Hz
        [SerializeField] private float hitInterval = 0.08f;
       
        public void Play(EnemySfxType type)
        {
            if (!this || !gameObject) return;
            if (!IsSpawned) return;
            
            int v = Convert.ToInt32(type);

            if (IsServerInitialized)
                RpcPlay(v);
            else
                ServerPlay(v);
        }

        [ServerRpc(RequireOwnership = false)]
        private void ServerPlay(int type)
        {
            if (!this || !gameObject) return;
            if (!IsSpawned) return;
            RpcPlay(type);
        }

        [ObserversRpc(BufferLast = false)]
        private void RpcPlay(int type)
        {
            if (!this || !gameObject) return;
            if (!IsSpawned) return;

            EnemySfxType t = (EnemySfxType)Enum.ToObject(typeof(EnemySfxType), type);
            
            if (!CanPlayNow(t))
                return;

            var ev = GetEvent(t);
            if (ev.IsNull == false)
                RuntimeManager.PlayOneShotAttached(ev, gameObject); // 3D 감쇠는 이벤트 설정값 사용
        }
        protected override float GetMinInterval(EnemySfxType type)
        {
            switch (type)
            {
                case EnemySfxType.EnemyShooting:     return shootingInterval;
                default:                               return 0.0f;
            }
        }

        protected override EventReference GetEvent(EnemySfxType type)
        {
            switch (type)
            {
                case EnemySfxType.EnemyShooting:     return enemyShooting;
                case EnemySfxType.EnemySpawned:      return enemySpawned;
                default:                               return default;
            }
        }
    }
}
