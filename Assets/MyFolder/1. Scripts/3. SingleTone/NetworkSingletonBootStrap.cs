using FishNet;
using FishNet.Object;
using FishNet.Transporting;
using MyFolder._1._Scripts._3._SingleTone.GameSetting;
using Unity.VisualScripting;
using UnityEngine;

namespace MyFolder._1._Scripts._3._SingleTone
{
    public class NetworkSingletonBootstrap : NetworkBehaviour
    {
        [SerializeField] private NetworkObject singletonPrefab; // GameSettingManager + KeepAlive + NetworkObject가 붙은 프리팹
        [SerializeField] private NetworkObject playerRoleManager;
        [SerializeField] private NetworkObject gameDataManager;
        [SerializeField] private NetworkObject playerSettingsManager;
        [SerializeField] private NetworkObject networkFlowManager;
        [SerializeField] private NetworkObject sessionManager;

        public override void OnStartServer()
        {
            SpawnOnce();   
        }

        private void SpawnOnce()
        {
            if (!GameSettingManager.Instance)
            {
                var nob = Instantiate(singletonPrefab);
                nob.SetIsGlobal(true);
                InstanceFinder.ServerManager.Spawn(nob);
                nob.gameObject.SetActive(true);
            }
            
            if (!GameDataManager.Instance)
            {
                var nob = Instantiate(gameDataManager);
                nob.SetIsGlobal(true);
                InstanceFinder.ServerManager.Spawn(nob);
                nob.gameObject.SetActive(true);
            }
            if (!PlayerSettingManager.Instance)
            {
                var nob = Instantiate(playerSettingsManager);
                nob.SetIsGlobal(true);
                InstanceFinder.ServerManager.Spawn(nob);
                nob.gameObject.SetActive(true);
            }
            
            if (!PlayerRoleManager.Instance)
            {
                var nob = Instantiate(playerRoleManager);
                nob.SetIsGlobal(true);
                InstanceFinder.ServerManager.Spawn(nob);
                nob.gameObject.SetActive(true);
            }

            if (!GameSessionManager.Instance)
            {
                var nob = Instantiate(sessionManager);
                nob.SetIsGlobal(true);
                InstanceFinder.ServerManager.Spawn(nob);
                nob.gameObject.SetActive(true);
            }

            if (!FindAnyObjectByType<NetworkFlowManager>())
            {
                NetworkObject nob;
                if (networkFlowManager)
                {
                    nob = Instantiate(networkFlowManager);
                }
                else
                {
                    var go = new GameObject("NetworkFlowManager");
                    nob = go.AddComponent<NetworkObject>();
                    go.AddComponent<NetworkFlowManager>();
                }
                nob.SetIsGlobal(true);
                InstanceFinder.ServerManager.Spawn(nob);
                nob.gameObject.SetActive(true);
            }
        }
    }
}
