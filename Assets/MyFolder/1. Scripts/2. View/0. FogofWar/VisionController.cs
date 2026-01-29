using System.Collections;
using System.Collections.Generic;
using MyFolder._1._Scripts._0._Object._0._Agent._0._Player;
using MyFolder._1._Scripts._3._SingleTone;
using Spine.Unity;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace MyFolder._1._Scripts._2._View._0._FogofWar
{
    public class VisionController : MonoBehaviour
    {
        #region Setting

        [Header("Context")]
        [SerializeField] PlayerContext playerContext;
        
        [Header("Object Settings")] 
        [SerializeField] private Transform playerTransform;

        [SerializeField] private Light2D playerlight2D;


        [SerializeField] private float updateInterval = 0.1f;
        [SerializeField] private float hysteresis = 0.5f;

        [Header("Layer/Tag Settings")] [SerializeField]
        private LayerMask obstacleLayer;


        [Header("Cleanup Setting")] [SerializeField]
        private float cleanupInterval = 10f;

        [SerializeField] private float enterTTL = 15f;

        #endregion

        #region Private Fields

        private const string canvasTag = "AgentUICanvas";

        private CircleCollider2D circleCollider;
        private Dictionary<int, AgentEntry> agentCache = new Dictionary<int, AgentEntry>();
        private HashSet<int> agentsInTrigger = new HashSet<int>();

        private float updateTimer = 0f;
        private float cleanupTimer = 0f;
        private float visionRadius = 0f;
        private List<int> _toRemoveCache = new List<int>();
        #endregion

        #region Agent Entry Class

        private class AgentEntry
        {
            public GameObject gameObject;
            public Transform transform;
            public VisionObject visionObject;

            public bool isVisible;
            public float lastSeenTime;
            public float lastDistance;

            public AgentEntry(GameObject go)
            {
                gameObject = go;
                transform = go.transform;

                go.TryGetComponent(out visionObject);
                
                isVisible = false;
                lastSeenTime = Time.time;
                lastDistance = 0f;
            }

            public bool IsValid()
            {
                return gameObject && gameObject.activeInHierarchy;
            }
        }

        #endregion

        #region Initialization

        private void Start()
        {
            StartCoroutine(nameof(WaitforClient));
        }

        private IEnumerator WaitforClient()
        {
            while(!playerContext.Controller.IsClientInitialized)
                yield return new WaitForSeconds(0.1f);
            while(!playerContext.Status.DataLoaded)
                yield return new WaitForSeconds(0.1f);
                
            if (playerContext.Controller.IsOwner)
            {
                visionRadius = playerContext.Status.PlayerData.visionRadius;
                
                if (gameObject.TryGetComponent(out circleCollider))
                    circleCollider.radius = visionRadius + (hysteresis * 2);

                playerlight2D.pointLightOuterRadius = visionRadius;
            }
            else
            {
                playerlight2D.gameObject.SetActive(false);
                enabled = false;
            }
        }
        
        #endregion

        #region Update Loop

        private void Update()
        {
            updateTimer += Time.deltaTime;
            if (updateTimer >= updateInterval)
            {
                updateTimer = 0f;
                UpdateVisibility();
            }

            // 캐시 정리
            cleanupTimer += Time.deltaTime;
            if (cleanupTimer >= cleanupInterval)
            {
                cleanupTimer = 0f;
                CleanupCache();
            }
        }

        private void UpdateVisibility()
        {
            Vector2 playerPos = playerTransform.position;
            float currentTime = Time.time;

            foreach (int instnaceId in agentsInTrigger)
            {
                if (!agentCache.TryGetValue(instnaceId, out AgentEntry entry))
                    continue;
                if (!entry.IsValid())
                    continue;

                float distance = Vector2.Distance(playerPos, entry.transform.position);

                bool inRange = false;
                if (entry.isVisible)
                {
                    inRange = distance <= (visionRadius + hysteresis);
                }
                else
                {
                    inRange = distance <= (visionRadius - hysteresis);
                }

                bool shouldBeVisible = false;

                if (inRange)
                {
                    Vector2 direction = ((Vector2)entry.transform.position - playerPos).normalized;
                    RaycastHit2D hit = Physics2D.Raycast(
                        playerPos,
                        direction,
                        distance,
                        obstacleLayer
                    );
                    shouldBeVisible = !hit.collider;
                }

                if (entry.isVisible != shouldBeVisible)
                {
                    entry.isVisible = shouldBeVisible;
                    SetAgentVisibility(entry, shouldBeVisible);
                }

                entry.lastDistance = distance;
                if (shouldBeVisible)
                {
                    entry.lastSeenTime = currentTime;
                }
            }
        }

        #endregion

        #region Visibility Control

        private void SetAgentVisibility(AgentEntry entry, bool shouldBeVisible)
        {
            if (shouldBeVisible)
            {
                entry.visionObject.VisionOn();
            }
            else
            {
                entry.visionObject.VisionOff();
            }
        }

        #endregion


        #region Trigger Events

        private void OnTriggerEnter2D(Collider2D other)
        {
            // 자기 자신 제외
            if (other.gameObject == playerTransform.gameObject)
                return;

            int instanceId = other.gameObject.GetInstanceID();

            // 트리거 집합에 추가
            agentsInTrigger.Add(instanceId);

            // 캐시에 없으면 등록
            if (!agentCache.ContainsKey(instanceId))
            {
                AgentEntry entry = new AgentEntry(other.gameObject);
                agentCache[instanceId] = entry;
#if UNITY_EDITOR
                LogManager.Log(LogCategory.System,$"[VisionController] Agent 등록: {other.gameObject.name} (ID: {instanceId})");
#endif
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            int instanceId = other.gameObject.GetInstanceID();

            // 트리거 집합에서 제거
            agentsInTrigger.Remove(instanceId);

            // 즉시 숨김 처리
            if (agentCache.TryGetValue(instanceId, out AgentEntry entry))
            {
                if (entry.isVisible)
                {
                    entry.isVisible = false;
                    SetAgentVisibility(entry, false);
                }
            }
        }

        #endregion

        #region Cache Cleanup

        private void CleanupCache()
        {
            float currentTime = Time.time;
            _toRemoveCache.Clear();

            foreach (var kvp in agentCache)
            {
                AgentEntry entry = kvp.Value;

                bool shouldRemove = false;

                if (!entry.IsValid())
                {
                    shouldRemove = true;
                }
                else if (!agentsInTrigger.Contains(kvp.Key))
                {
                    if (currentTime - entry.lastSeenTime > enterTTL)
                    {
                        shouldRemove = true;
                    }
                }

                if (shouldRemove)
                {
                    _toRemoveCache.Add(kvp.Key);
                }
            }

            foreach (int id in _toRemoveCache)
            {
                agentCache.Remove(id);
            }
        }

        #endregion

        #region Cleanup

        private void OnDestroy()
        {
            StopAllCoroutines();
            // 모든 Agent 보이게 복원
            foreach (var entry in agentCache.Values)
            {
                if (entry.IsValid() && !entry.isVisible)
                {
                    SetAgentVisibility(entry, true);
                }
            }

            agentCache.Clear();
            agentsInTrigger.Clear();
        }

        #endregion

        #region Gizmos

        private void OnDrawGizmosSelected()
        {
            if (!playerTransform)
                playerTransform = transform;

            // 시야 범위 표시
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(playerTransform.position, visionRadius);

            // Hysteresis 범위 표시
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(playerTransform.position, visionRadius - hysteresis);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(playerTransform.position, visionRadius + hysteresis);
        }

        #endregion

    }
}