using FishNet.Object;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;
using STOP_MODE = FMOD.Studio.STOP_MODE;

namespace MyFolder._1._Scripts._10._Sound
{
    public class GameSystemSound : NetworkBehaviour
    {
        private static GameSystemSound instance;
        
        public static GameSystemSound Instance
        {
            get
            {
                if (!instance)
                {
                    instance = FindFirstObjectByType<GameSystemSound>();

                    if (!instance)
                    {
                        GameObject obj = new GameObject();
                        obj.name = nameof(GameSystemSound);
                        instance = obj.AddComponent<GameSystemSound>();
                    }
                }
                return instance;
            }
        }

        public enum SSFXType {Q_CREATE,Q_SUCCESS,Q_FAIL,T_LIMIT,P_DEADALERT,ENV_OWL,ENV_OWLFAR,ENV_LEAF,ENV_Twig,ENV_Pigeon,ENV_Gravel,ENV_Cricket}
        
        [Header("UI")]
        [SerializeField] private EventReference SYS_Quest_Create;
        [SerializeField] private EventReference SYS_Quest_Success;
        [SerializeField] private EventReference SYS_Quest_Fail;
        [SerializeField] private EventReference SYS_TimeLimit;
        [SerializeField] private EventReference SYS_Player_DeadAlert;
        
        [Header("env 3D")]
        [SerializeField] private EventReference Env_Owl;
        [SerializeField] private EventReference Env_Leaf;
        [SerializeField] private EventReference ENV_OWLFAR;
        [SerializeField] private EventReference ENV_Twig;
        [SerializeField] private EventReference ENV_Pigeon;
        [SerializeField] private EventReference ENV_Cricket;
        [SerializeField] private EventReference ENV_Gravel;
        
        public EventInstance SYS_TimeLimit_Instance;

        public void Quest_CreateSFX()
        {
            OnSystemSfx_OnPlay(SSFXType.Q_CREATE);
        }

        public void Quest_SuccessSFX()
        {
            OnSystemSfx_OnPlay(SSFXType.Q_SUCCESS);
        }

        public void Quest_FailSFX()
        {
            OnSystemSfx_OnPlay(SSFXType.Q_FAIL);
        }

        public void Quest_TimeLimitSFX_Start()
        {
            OnSystemSfx_OnPlay(SSFXType.T_LIMIT);
        }

        public void Quest_TimeLimitSFX_End()
        {
            OnSystemSfx_OnStop(SSFXType.T_LIMIT);
        }

        public void Player_DeadAlertSFX()
        {
            OnSystemSfx_OnPlay(SSFXType.P_DEADALERT);
        }

        public void Player_OwlSFX(GameObject obj)
        {
            OnSystemSfx_OnLocalPlay3D(SSFXType.ENV_OWL,obj);
        }

        public void Player_OwlFarSFX(GameObject obj)
        {
            OnSystemSfx_OnLocalPlay3D(SSFXType.ENV_OWLFAR,obj);
        }
        
        public void Player_Default_SFX3D(SSFXType type,GameObject obj)
        {
            OnSystemSfx_OnLocalPlay3D(type,obj);
        }

        public void Player_Default_SFX(SSFXType type)
        {
            OnSystemSfx_OnLocalPlay(type);
        }
        private void OnSystemSfx_OnLocalPlay3D(SSFXType sfxType,GameObject obj = null)
        {
            EventReference reference = GetReference(sfxType);
            RuntimeManager.PlayOneShotAttached(reference, obj); 
        }

        private void OnSystemSfx_OnLocalPlay(SSFXType sfxType)
        {
            EventReference reference = GetReference(sfxType);
            RuntimeManager.PlayOneShot(reference);
        }
        private void OnSystemSfx_OnPlay(SSFXType type)
        {
            if (IsServerInitialized)
            {
                OnSystemSfx_OnPlay_ObserversRPC(type);
            }
            else
            {
                OnSystemSfx_OnPlay_ServerRPC(type);
            }
        }
        

        [ServerRpc]
        private void OnSystemSfx_OnPlay_ServerRPC(SSFXType type)
        {
            OnSystemSfx_OnPlay_ObserversRPC(type);
        }

        [ObserversRpc]
        private void OnSystemSfx_OnPlay_ObserversRPC(SSFXType type)
        {
            EventReference reference = GetReference(type);
            switch (type)
            {
                case SSFXType.Q_CREATE:
                case SSFXType.Q_SUCCESS:
                case SSFXType.Q_FAIL:
                case SSFXType.P_DEADALERT:
                    RuntimeManager.PlayOneShot(reference);
                    break;
                case SSFXType.T_LIMIT:
                    if (SYS_TimeLimit_Instance.isValid())
                    {
                        SYS_TimeLimit_Instance.stop(STOP_MODE.IMMEDIATE);
                        SYS_TimeLimit_Instance.release();
                    }
                    SYS_TimeLimit_Instance = RuntimeManager.CreateInstance(reference);
                    SYS_TimeLimit_Instance.start();
                    break;
            }
        }


        private void OnSystemSfx_OnStop(SSFXType type)
        {
            if (IsServerInitialized)
            {
                OnSystemSfx_OnStop_ObserversRPC(type);
            }
            else
            {
                OnSystemSfx_OnStop_ServerRPC(type);
            }
        }
        
        [ServerRpc]
        private void OnSystemSfx_OnStop_ServerRPC(SSFXType type)
        {
            OnSystemSfx_OnStop_ObserversRPC(type);
        }

        [ObserversRpc]
        private void OnSystemSfx_OnStop_ObserversRPC(SSFXType type)
        {   
            switch (type)
            {
                case SSFXType.T_LIMIT:
                    // ✅ 유효성 검사 추가
                    if (SYS_TimeLimit_Instance.isValid())
                    {
                        SYS_TimeLimit_Instance.stop(STOP_MODE.IMMEDIATE);
                        SYS_TimeLimit_Instance.release();
                    }
                    break;
            }
        }
        
        private EventReference GetReference(SSFXType type)
        {
            switch (type)
            {
                case SSFXType.Q_CREATE:
                    return SYS_Quest_Create;
                case SSFXType.Q_SUCCESS:
                    return SYS_Quest_Success;
                case SSFXType.Q_FAIL:
                    return SYS_Quest_Fail;
                case SSFXType.T_LIMIT:
                    return SYS_TimeLimit;
                case SSFXType.P_DEADALERT:
                    return SYS_Player_DeadAlert;
                case SSFXType.ENV_OWL:
                    return Env_Owl;
                case SSFXType.ENV_OWLFAR:
                    return ENV_OWLFAR;
                case SSFXType.ENV_LEAF:
                    return Env_Leaf;
                case SSFXType.ENV_Twig:
                    return ENV_Twig;
                case SSFXType.ENV_Cricket:
                    return ENV_Cricket;
                case SSFXType.ENV_Pigeon:
                    return ENV_Pigeon;
                case SSFXType.ENV_Gravel:
                    return ENV_Gravel;
            }

            return SYS_Quest_Create;
        }
        
        // ✅ 추가: 컴포넌트 파괴 시 정리
        private void OnDestroy()
        {
            if (SYS_TimeLimit_Instance.isValid())
            {
                SYS_TimeLimit_Instance.stop(STOP_MODE.IMMEDIATE);
                SYS_TimeLimit_Instance.release();
            }
        }
    }
}