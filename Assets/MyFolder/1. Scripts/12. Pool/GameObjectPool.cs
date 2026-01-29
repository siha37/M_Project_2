using System.Collections.Generic;
using UnityEngine;

namespace MyFolder._1._Scripts._12._Pool
{
    /// <summary>
    /// 로컬 환경에서 사용할 수 있는 범용 GameObject 풀.
    /// 파티클, 이펙트, 데칼 등 가벼운 오브젝트를 효율적으로 재사용할 수 있다.
    /// </summary>
    public class GameObjectPool : MonoBehaviour
    {
        [Header("Pool Settings")]
        [SerializeField] private GameObject prefab;
        [SerializeField] private Transform poolRoot;
        [SerializeField] private int initialSize = 10;
        [SerializeField] private int expandSize = 5;
        [SerializeField] private int maxSize = 200;
        [SerializeField] private bool allowExpand = true;

        private readonly Queue<GameObject> pooledObjects = new Queue<GameObject>();
        private readonly HashSet<GameObject> activeObjects = new HashSet<GameObject>();
        private int totalCreated;

        public bool IsInitialized { get; private set; }

        private void Awake()
        {
            if (!IsInitialized)
            {
                InitializePool();
            }
        }

        /// <summary>
        /// 풀을 초기화하고 지정된 개수만큼 예열한다.
        /// </summary>
        public void InitializePool()
        {
            if (IsInitialized)
            {
                return;
            }

            if (!prefab)
            {
                Debug.LogError($"{name}: GameObjectPool prefab이 설정되지 않았습니다.", this);
                return;
            }

            poolRoot = poolRoot ? poolRoot : transform;

            WarmUp(initialSize);
            IsInitialized = true;
        }

        /// <summary>
        /// 지정한 개수만큼 인스턴스를 생성하여 풀에 적재한다.
        /// </summary>
        public void WarmUp(int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (maxSize > 0 && totalCreated >= maxSize)
                {
                    break;
                }

                pooledObjects.Enqueue(CreateInstance());
            }
        }

        /// <summary>
        /// 풀에서 오브젝트를 꺼내 위치/회전을 설정하고 반환한다.
        /// </summary>
        public GameObject Get(Vector3 position, Quaternion rotation, Transform parent = null)
        {
            GameObject obj = GetInternal();
            if (!parent)
                parent = poolRoot;
            obj.transform.SetParent(parent, false);
            obj.transform.SetPositionAndRotation(position, rotation);
            obj.SetActive(true);

            OnBeforeUse(obj);
            return obj;
        }

        /// <summary>
        /// Transform 설정 없이 오브젝트를 반환한다.
        /// </summary>
        public GameObject Get()
        {
            GameObject obj = GetInternal();
            obj.SetActive(true);

            OnBeforeUse(obj);
            return obj;
        }

        /// <summary>
        /// 사용이 끝난 오브젝트를 풀에 반환한다.
        /// </summary>
        public void Release(GameObject obj)
        {
            if (!obj || !activeObjects.Contains(obj))
            {
                return;
            }

            OnBeforeRelease(obj);

            obj.SetActive(false);
            obj.transform.SetParent(poolRoot, false);

            activeObjects.Remove(obj);
            pooledObjects.Enqueue(obj);
        }

        private GameObject GetInternal()
        {
            if (pooledObjects.Count == 0)
            {
                if (!allowExpand)
                {
                    Debug.LogWarning($"{name}: 풀에 여유가 없습니다.", this);
                }
                else
                {
                    WarmUp(expandSize);
                }
            }

            if (pooledObjects.Count == 0)
            {
                // 확장 실패 시 마지막 수단으로 즉시 생성
                pooledObjects.Enqueue(CreateInstance());
            }

            GameObject obj = pooledObjects.Dequeue();
            activeObjects.Add(obj);
            return obj;
        }

        private GameObject CreateInstance()
        {
            GameObject obj = Instantiate(prefab, poolRoot);
            obj.SetActive(false);
            totalCreated++;
            OnObjectCreated(obj);
            return obj;
        }

        protected virtual void OnObjectCreated(GameObject obj)
        {
        }

        protected virtual void OnBeforeUse(GameObject obj)
        {
        }

        protected virtual void OnBeforeRelease(GameObject obj)
        {
        }
    }
}

