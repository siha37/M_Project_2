using MyFolder._1._Scripts._0._Object._0._Agent._0._Player;
using MyFolder._1._Scripts._3._SingleTone;
using Unity.Cinemachine;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private CinemachineCamera _camera;
    
    void Start()
    {
        if(_camera == null)
            _camera = FindFirstObjectByType<CinemachineCamera>();
        FindAndSetOwnerPlayer();
    }
    
    private void FindAndSetOwnerPlayer()
    {
        PlayerNetworkSync[] allPlayers = FindObjectsByType<PlayerNetworkSync>(FindObjectsSortMode.None);
        
        foreach (PlayerNetworkSync player in allPlayers)
        {
            if (player.IsOwner)
            {
                if (_camera != null)
                {
                    _camera.Follow = player.transform;
                    LogManager.Log(LogCategory.Camera, $"Cinemachine 타겟 설정: {player.gameObject.name}");
                }
                return;
            }
        }
        
        // 플레이어를 찾지 못했으면 1초 후 재시도
        Invoke("FindAndSetOwnerPlayer", 1);
    }
}
