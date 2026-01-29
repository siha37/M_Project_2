using System;
using FishNet.Object;
using FMODUnity;
using UnityEngine;

namespace MyFolder._1._Scripts._10._Sound
{
    public enum QuestObjSfxType : byte
    {
        Broken,
        Create
    }
    
    public class QuestObjSfx : ObjectSfx<QuestObjSfxType>
    {
        [Header("FMOD Events (3D)")]
        [SerializeField] private EventReference Create;
        [SerializeField] private EventReference Broken;
        
        public override void OnStartServer()
        {
            Play(QuestObjSfxType.Create);
        }
        
        public void Play(QuestObjSfxType type)
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

            QuestObjSfxType t = (QuestObjSfxType)Enum.ToObject(typeof(QuestObjSfxType), type);
            
            if (!CanPlayNow(t))
                return;

            var ev = GetEvent(t);
            if (ev.IsNull == false)
                RuntimeManager.PlayOneShotAttached(ev, gameObject); // 3D 감쇠는 이벤트 설정값 사용
        }
        protected override float GetMinInterval(QuestObjSfxType type)
        {
            return 0f;
        }

        protected override EventReference GetEvent(QuestObjSfxType type)
        {
            switch (type)
            {
                case QuestObjSfxType.Broken: return Broken;
                case QuestObjSfxType.Create: return Create;
                default:                     return default;   
            }
        }
    }
}