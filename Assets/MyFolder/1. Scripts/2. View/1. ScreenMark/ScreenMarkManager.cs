using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;
using UnityEngine.UI;

namespace MyFolder._1._Scripts._2._View._1._ScreenMark
{
    public class ScreenMarkManager : NetworkBehaviour
    {
        public static ScreenMarkManager instance;
        
        public static ScreenMarkManager Instance
        {
            get
            {
                if (!instance)
                {
                    instance = FindFirstObjectByType<ScreenMarkManager>();
                }
                return instance;
            }
        }
        public Camera mainCamera;
        public RectTransform markerPrefab;  // 마커 프리팹 (UI)
        public RectTransform markerContainer;  // 마커들을 담을 UI 패널 (Canvas 내)

        [SerializeField]
        private List<TrackedObject> trackedObjects = new();  // 인스펙터에서 설정 가능
        
        private Dictionary<NetworkObject, RectTransform> markers = new();

        public delegate void delegate_void();

        public delegate_void SafeNpcFinding;
        public delegate_void QuestObjectFinding;

        private List<NetworkObject> _toRemoveCache = new List<NetworkObject>();
        
        void Start()
        {
            mainCamera = Camera.main;
        }

        void LateUpdate()
        {
            // 삭제된 오브젝트 자동 정리
            trackedObjects.RemoveAll(item => !item.target);

            foreach (var obj in trackedObjects)
            {
                if (!obj.target) continue;

                if (!markers.ContainsKey(obj.target))
                {
                    RectTransform newMarker = Instantiate(markerPrefab, markerContainer);
                    markers[obj.target] = newMarker;

                    // 마커 색상 적용
                    newMarker.TryGetComponent(out Image markerImage);
                    if (markerImage)
                    {
                        markerImage.color = obj.markerColor();
                    }
                }

                UpdateMarker(obj.target.transform, markers[obj.target]);
            }

            // 삭제된 마커 정리
            _toRemoveCache.Clear();
            foreach (var pair in markers)
            {
                if (!trackedObjects.Exists(o => o.target == pair.Key) || !pair.Key)
                {
                    Destroy(pair.Value.gameObject);
                    _toRemoveCache.Add(pair.Key);
                }
            }
            foreach (var key in _toRemoveCache)
            {
                markers.Remove(key);
            }
        }

        bool UpdateMarker(Transform target, RectTransform markerUI)
        {
            Vector3 screenPoint = mainCamera.WorldToViewportPoint(target.position);
            bool isOffScreen = screenPoint.x < 0 || screenPoint.x > 1 || screenPoint.y < 0 || screenPoint.y > 1;

            if (isOffScreen)
            {
                markerUI.gameObject.SetActive(true);

                // 월드 좌표 -> 스크린 좌표 변환
                Vector3 screenPos = mainCamera.WorldToScreenPoint(target.position);
                
                // 방향 벡터 계산
                Vector3 direction = (screenPos - new Vector3(Screen.width / 2f, Screen.height / 2f)).normalized;

                // 마커 위치를 화면 테두리로 제한
                Vector3 clampedPosition = new Vector3(
                    Mathf.Clamp(screenPos.x, 300, Screen.width - 300),
                    Mathf.Clamp(screenPos.y, 300, Screen.height - 300),
                    0
                );
                markerUI.position = clampedPosition;

                // 마커 회전 (방향 표시)
                //float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                //markerUI.rotation = Quaternion.Euler(0, 0, angle - 90);
                return true;
            }
            else
            {
                markerUI.gameObject.SetActive(false);
                return false;
            }
        }

        public void AddTarget(NetworkObject target,TrackedObject.TrackedObjectType type)
        {
            if (!trackedObjects.Exists(obj => obj.target == target))
            {
                trackedObjects.Add(new TrackedObject { target = target, type = type });
                AddTargetRPC(target, type);
            }
        }


        public void RemoveTarget(NetworkObject target)
        {
            trackedObjects.RemoveAll(obj => obj.target == target);
            if (markers.ContainsKey(target))
            {
                RemoveTargetRPC(target);
                Destroy(markers[target].gameObject);
                markers.Remove(target);
            }
        }
        
        [ObserversRpc]
        public void AddTargetRPC(NetworkObject target,TrackedObject.TrackedObjectType type)
        {
            if(IsServerInitialized)
                return;
            if (!trackedObjects.Exists(obj => obj.target == target))
            {
                trackedObjects.Add(new TrackedObject { target = target, type = type});
            }
        }

        [ObserversRpc]
        public void RemoveTargetRPC(NetworkObject target)
        {
            if(IsServerInitialized)
                return;
            trackedObjects.RemoveAll(obj => obj.target == target);
            if (target && markers != null && markers.ContainsKey(target))
            {
                Destroy(markers[target].gameObject);
                markers.Remove(target);
            }
        }
    }
    
    [System.Serializable]
    public class TrackedObject
    {
        public enum TrackedObjectType
        {
            None=0,
            Extermination,
            Defense,
            Survival,
            Player
        }
        public NetworkObject target;  // 추적할 오브젝트
        public TrackedObjectType type = TrackedObjectType.None;

        public Color markerColor()
        {
            switch (type)
            {
                case TrackedObjectType.Extermination:
                    return GlobalColors.ExterminationColor;
                case TrackedObjectType.Defense:
                    return GlobalColors.DefenseColor;
                case TrackedObjectType.Survival:
                    return GlobalColors.SurvivalColor;
                case TrackedObjectType.Player:
                    return GlobalColors.PlayerColor;
            }
            return Color.magenta;
        }
        public bool firstFinding = true;
    }


    public static class GlobalColors
    {
        public static readonly Color ExterminationColor = new Color(1,0.240566f,0.240566f);
        public static readonly Color DefenseColor = new Color(0.2392157f, 0.68009f, 1);
        public static readonly Color SurvivalColor = new Color(0.6055725f, 1, 0.5990566f);
        public static readonly Color PlayerColor = new Color(1f, 0.5f, 0f);
    }
}