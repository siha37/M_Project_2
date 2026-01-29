using System;
using FishNet.Object;
using FMODUnity;
using UnityEngine;

namespace MyFolder._1._Scripts._10._Sound
{
    
    public enum SpawnerSfxType : byte
    {
        Spanwed,
        Broken,
        Create
    }
    public class SpawnerSfx : ObjectSfx<SpawnerSfxType>
    {
        [Header("FMOD Events (3D)")]
        [SerializeField] private EventReference Spawned;
        [SerializeField] private EventReference Broken;
        [SerializeField] private EventReference Create;

        public override void OnStartServer()
        {
            Play(SpawnerSfxType.Create);
        }

        public void Play(SpawnerSfxType type)
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

            SpawnerSfxType t = (SpawnerSfxType)Enum.ToObject(typeof(SpawnerSfxType), type);
            
            if (!CanPlayNow(t))
                return;

            var ev = GetEvent(t);
            if (ev.IsNull == false)
                RuntimeManager.PlayOneShotAttached(ev, gameObject); // 3D 감쇠는 이벤트 설정값 사용
        }
        protected override float GetMinInterval(SpawnerSfxType type)
        {
            return 0;
        }

        protected override EventReference GetEvent(SpawnerSfxType type)
        {
            switch (type)
            { 
                case SpawnerSfxType.Spanwed:         return Spawned;
                case SpawnerSfxType.Broken:          return Broken;
                case SpawnerSfxType.Create:          return Create;
                default:                             return default;
            }
        }
    }
}