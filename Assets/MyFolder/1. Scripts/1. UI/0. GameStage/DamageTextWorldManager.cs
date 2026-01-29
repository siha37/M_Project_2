using System.Collections.Generic;
using UnityEngine;

namespace MyFolder._1._Scripts._1._UI._0._GameStage
{
    public sealed class DamageTextWorldManager : MonoBehaviour
    {
        public enum DamageType{hit,critical,shield,heal}
        public static DamageTextWorldManager Instance { get; private set; }

        [Header("World Canvas Root")]
        [SerializeField] private Canvas worldCanvas; // World Space
        [SerializeField] private RectTransform worldCanvasRect;
        [SerializeField] private DamageTextWorldItem itemPrefab;
        [SerializeField] private int initialPool = 64;
        [SerializeField] private float maxSpawnDistance = 50f; // 카메라 기준 허용 거리
        [SerializeField] private float criticalDamageMultiplier = 2f;
        [Header("Play Settings")]
        [SerializeField] private float defaultDuration = 0.6f;
        [SerializeField] private float defaultRise = 1f;      // 월드 단위 상승
        [SerializeField] private float criticalScale = 1.2f;
        [SerializeField] private Vector3 positionOffset = new(0.05f, 0.3f, 0f);

        private readonly Queue<DamageTextWorldItem> pool = new(64);
        private Camera cachedMainCamera;

        private void Awake()
        {
            if (Instance && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (!worldCanvas) worldCanvas = GetComponentInChildren<Canvas>();
            if (worldCanvas) worldCanvas.renderMode = RenderMode.WorldSpace;
            if (!worldCanvasRect && worldCanvas) worldCanvasRect = worldCanvas.GetComponent<RectTransform>();

            cachedMainCamera = Camera.main;

            for (int i = 0; i < initialPool; i++)
            {
                Create();
            }
        }

        private DamageTextWorldItem Create()
        {
            var inst = Instantiate(itemPrefab, worldCanvasRect);
            inst.gameObject.SetActive(false);
            inst.Initialize(this);
            pool.Enqueue(inst);
            return inst;
        }

        public void Return(DamageTextWorldItem item)
        {
            item.gameObject.SetActive(false);
            pool.Enqueue(item);
        }

        public bool TrySpawnStamp(Vector3 worldPosition, float amount, DamageType type)
        {
            var cam = cachedMainCamera ? cachedMainCamera : (cachedMainCamera = Camera.main);
            if (!cam) return false;

            Transform camTransform = cam.transform;
            Vector3 vp = cam.WorldToViewportPoint(worldPosition);
            if (vp.z <= 0f || vp.x < 0f || vp.x > 1f || vp.y < 0f || vp.y > 1f)
                return false;

            if (maxSpawnDistance > 0f)
            {
                float dist = (camTransform.position - worldPosition).sqrMagnitude;
                if (dist > maxSpawnDistance * maxSpawnDistance) return false;
            }

            DamageTextWorldItem item = pool.Count > 0 ? pool.Dequeue() : Create();
            item.transform.position = worldPosition + positionOffset;
            // 스폰 시 1회 빌보드(고정)
            item.transform.forward = camTransform.forward;
            item.gameObject.SetActive(true);
            amount = Mathf.Round(amount);
            item.Play(type == DamageType.critical ? amount * criticalDamageMultiplier : amount, type, defaultDuration, defaultRise, criticalScale);
            return true;
        }
    }
}


