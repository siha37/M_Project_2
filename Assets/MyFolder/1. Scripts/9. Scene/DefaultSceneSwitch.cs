using FishNet;
using FishNet.Managing.Scened;
using UnityEngine;
using SceneManager = UnityEngine.SceneManagement.SceneManager;

namespace MyFolder._1._Scripts._9._Scene
{
    public class DefaultSceneSwitch : MonoBehaviour
    {
        [SerializeField] private string sceneName;

        
        
        public void OnClick()
        {
            SceneManager.LoadScene(sceneName);
        }
        public void OnClickOnServer()
        {
            if (InstanceFinder.IsHostStarted)
            {
                SceneLoadData data = new SceneLoadData(sceneName)
                {
                    ReplaceScenes = ReplaceOption.All
                };
                InstanceFinder.SceneManager.LoadGlobalScenes(data);
            }
        }
    }
}
