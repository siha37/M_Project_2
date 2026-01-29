using System.Collections.Generic;
using FishNet;
using FishNet.Connection;
using FishNet.Managing.Scened;
using FishNet.Object;

namespace MyFolder._1._Scripts._3._SingleTone
{
    public class NetworkFlowManager : NetworkBehaviour
    {
        public static NetworkFlowManager Instance { get; private set; }
        private void Awake() => Instance = this;

        public override void OnStopServer()
        {
        }



        [ServerRpc(RequireOwnership = false)]
        public void LoadSceneForClient(NetworkConnection conn = null,string sceneName = null)
        {
            // Ensure server is started before processing.
            if (!InstanceFinder.ServerManager || !InstanceFinder.NetworkManager.IsServerStarted)
                return;

            // Prefer the caller connection; resolve host edge if null.
            if (conn == null)
            {
                var serverMgr = InstanceFinder.ServerManager;
                var clientMgr = InstanceFinder.ClientManager;
                if (serverMgr && clientMgr && clientMgr.Connection != null)
                {
                    serverMgr.Clients.TryGetValue(clientMgr.Connection.ClientId, out conn);
                }
            }

            if (conn == null || !conn.IsValid)
                return;

            var data = new SceneLoadData(new List<string> { sceneName })
            {
                ReplaceScenes = ReplaceOption.All
            };
            InstanceFinder.SceneManager.LoadConnectionScenes(conn, data);
        }
    }
}
