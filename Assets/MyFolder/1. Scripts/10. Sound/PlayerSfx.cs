using System;
using FMODUnity;
using FMOD.Studio;
using FishNet.Object;
using System.Collections.Generic;
using MyFolder._1._Scripts._0._Object._0._Agent._0._Player;
using MyFolder._1._Scripts._0._Object._0._Agent._0._Player._0._Component;
using UnityEngine;
using STOP_MODE = FMOD.Studio.STOP_MODE;

namespace MyFolder._1._Scripts._10._Sound
{
    public enum PlayerSfxType : byte
    {
        PlayerShooting,
        PlayerDie,
        PlayerWalk,
        PlayerDefence,
        PlayerHit,
        PlayerCriticalHit,
        PlayerCamouflage,
        PlayerAlive
    }

    public sealed class PlayerSfx : ObjectSfx<PlayerSfxType>
    {
        [SerializeField] private PlayerContext context;


        [Header("FMOD Events (3D)")]
        [SerializeField] private EventReference playerShooting;
        [SerializeField] private EventReference playerDie;
        [SerializeField] private EventReference playerWalk;
        [SerializeField] private EventReference playerDefence;
        [SerializeField] private EventReference playerHit;
        [SerializeField] private EventReference playerCriticalHit;
        [SerializeField] private EventReference playerCamouflage;
        [SerializeField] private EventReference playerAlive;

        [Header("Min Interval (seconds)")]
        [SerializeField] private float shootingInterval = 0.05f;       // 20Hz
        [SerializeField] private float hitInterval = 0.08f;
        [SerializeField] private float critInterval = 0.10f;
        [SerializeField] private float defenceInterval = 0.10f;
        [SerializeField] private float walkInterval = 0.25f;

        private readonly Dictionary<PlayerSfxType, float> lastTimes = new Dictionary<PlayerSfxType, float>(8);
        private EventInstance walkInstance;
        
        private PlayerCamouflageComponent camouflage;
        
        private bool isWalkPlaying;
        public void Play(PlayerSfxType type)
        {
            if (!this || !gameObject) return;
            if (!IsSpawned) return;
            if(camouflage == null)
                camouflage = context.Component.GetPComponent<PlayerCamouflageComponent>() as PlayerCamouflageComponent;
            if(PlayerSfxType.PlayerHit != type && camouflage is { IsDisguised: true })
                return;
            
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
            
            
            PlayerSfxType t = (PlayerSfxType)Enum.ToObject(typeof(PlayerSfxType), type);
            
            if (t == PlayerSfxType.PlayerWalk)
                return; // 걷기는 루프 인스턴스로 처리

            if (!CanPlayNow(t))
                return;

            var ev = GetEvent(t);
            if (ev.IsNull == false)
                RuntimeManager.PlayOneShotAttached(ev, gameObject); // 3D 감쇠는 이벤트 설정값 사용
        }

        public void SetWalking(bool walking,float WalkType = 0)
        {
            if(camouflage == null)
                camouflage = context.Component.GetPComponent<PlayerCamouflageComponent>() as PlayerCamouflageComponent;
            if(camouflage is { IsDisguised: true })
                return;
            if (IsServerInitialized)
                RpcSetWalking(walking,WalkType);
            else
                ServerSetWalking(walking,WalkType);
        }

        [ServerRpc(RequireOwnership = false)]
        private void ServerSetWalking(bool walking,float WalkType)
        {
            RpcSetWalking(walking,WalkType);
        }

        [ObserversRpc(BufferLast = false)]
        private void RpcSetWalking(bool walking,float WalkType)
        {
            if (walking)
            {
                if (isWalkPlaying)
                    return;
                if(!walkInstance.isValid())
                    walkInstance = RuntimeManager.CreateInstance(playerWalk);
                walkInstance.setParameterByName("WalkType", WalkType);
                RuntimeManager.AttachInstanceToGameObject(walkInstance, transform);
                walkInstance.start();
                isWalkPlaying = true;
            }
            else
            {
                if (!isWalkPlaying)
                    return;

                walkInstance.stop(STOP_MODE.ALLOWFADEOUT);
                walkInstance.release();
                isWalkPlaying = false;
            }
        }

        protected override float GetMinInterval(PlayerSfxType type)
        {
            switch (type)
            {
                case PlayerSfxType.PlayerShooting:     return shootingInterval;
                case PlayerSfxType.PlayerHit:          return hitInterval;
                case PlayerSfxType.PlayerCriticalHit:  return critInterval;
                case PlayerSfxType.PlayerDefence:      return defenceInterval;
                case PlayerSfxType.PlayerWalk:         return walkInterval;
                default:                               return 0.0f;
            }
        }

        protected override EventReference GetEvent(PlayerSfxType type)
        {
            switch (type)
            {
                case PlayerSfxType.PlayerShooting:     return playerShooting;
                case PlayerSfxType.PlayerDie:          return playerDie;
                case PlayerSfxType.PlayerWalk:         return playerWalk;
                case PlayerSfxType.PlayerDefence:      return playerDefence;
                case PlayerSfxType.PlayerHit:          return playerHit;
                case PlayerSfxType.PlayerCriticalHit:  return playerCriticalHit;
                case PlayerSfxType.PlayerCamouflage:   return playerCamouflage;
                case PlayerSfxType.PlayerAlive:        return playerAlive;
                default:                               return default;
            }
        }

        private void OnDestroy()
        {
            if (isWalkPlaying)
            {
                walkInstance.stop(STOP_MODE.IMMEDIATE);
                walkInstance.release();
            }
        }
    }
}

