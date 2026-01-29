using System.Collections.Generic;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;

namespace MyFolder._1._Scripts._10._Sound.Impact
{
    /// <summary>
    /// FMOD 기반 탄환 임팩트 사운드 단일 클래스.
    /// - 태그/레이어 매핑으로 이벤트 선택
    /// - 거리 컬링, 글로벌/이벤트별 보이스 리밋, 쿨다운
    /// - PlayOneShotAttached 또는 Instance 경로(파라미터 적용)
    /// - 경량 앵커 풀 포함(히트 포인트/팔로우 대상에 부착)
    /// </summary>
    public sealed class BulletImpactAudio : MonoBehaviour
    {
        [System.Serializable]
        private struct TagEvent
        {
            public string tag;
            public EventReference ev;
        }

        [System.Serializable]
        private struct LayerEvent
        {
            public int layer;
            public EventReference ev;
        }

        private static BulletImpactAudio instance;

        [Header("Tag → Event Mapping")]
        [SerializeField] private TagEvent[] tagEvents = new TagEvent[0];

        [Header("Layer → Event Mapping")]
        [SerializeField] private LayerEvent[] layerEvents = new LayerEvent[0];

        [Header("Voice Limits & Cooldowns")]
        [SerializeField] private int globalMaxPerFrame = 16;
        [SerializeField] private float globalCooldown = 0.006f;
        [SerializeField] private float sameEventCooldown = 0.02f;

        private readonly Dictionary<string, float> lastPlayTimesByEvent = new Dictionary<string, float>(64);
        private float lastGlobalTime;
        private int playedThisFrame;

        private void Awake()
        {
            if (instance && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
        }

        private void LateUpdate()
        {
            playedThisFrame = 0;
            AudioAnchorPool.RuntimeTick();
        }

        /// <summary>
        /// 외부 진입점(간소화): 좌표/레이어/태그 정보만으로 임팩트 재생.
        /// 2D 충돌 등 RaycastHit 이 없는 경로에서 사용.
        /// </summary>
        public static void PlayImpactAt(Vector3 point, int targetLayer, string targetTag)
        {
            EnsureInstance();

            var ev = instance.SelectEventByTagLayer(targetTag, targetLayer);
            if (ev.IsNull)
                return;
            
            if (!instance.TryAcquireBudget(ev))
                return;

            var anchor = AudioAnchorPool.Get(point, null);

            RuntimeManager.PlayOneShotAttached(ev, anchor.gameObject);

            AudioAnchorPool.RecycleAfter(anchor, 0.25f);
        }

       
        private EventReference SelectEventByTagLayer(string tag, int layer)
        {
            if (!string.IsNullOrEmpty(tag) && tagEvents is { Length: > 0 })
            {
                for (int i = 0; i < tagEvents.Length; i++)
                {
                    if (tag == tagEvents[i].tag)
                        return tagEvents[i].ev;
                }
            }

            if (layerEvents is { Length: > 0 })
            {
                for (int i = 0; i < layerEvents.Length; i++)
                {
                    if (layer == layerEvents[i].layer)
                        return layerEvents[i].ev;
                }
            }

            return default;
        }

        private bool TryAcquireBudget(EventReference ev)
        {
            if (playedThisFrame >= globalMaxPerFrame)
                return false;

            float now = Time.time;
            if ((now - lastGlobalTime) < globalCooldown)
                return false;

            string key = ev.Guid.ToString();
            if (lastPlayTimesByEvent.TryGetValue(key, out float last))
            {
                if ((now - last) < sameEventCooldown)
                    return false;
            }

            lastPlayTimesByEvent[key] = now;
            lastGlobalTime = now;
            playedThisFrame++;
            return true;
        }

        private static void EnsureInstance()
        {
            if (instance)
                return;

            var go = new GameObject("BulletImpactAudio");
            instance = go.AddComponent<BulletImpactAudio>();
            DontDestroyOnLoad(go);
        }

        // ------------------------------------------------------------
        // 경량 오디오 앵커 풀 + 러너
        // ------------------------------------------------------------
        private sealed class AudioAnchor
        {
            public GameObject gameObject;
            public Transform transform;
            public float recycleAt;
            public Transform follow;
        }

        private static class AudioAnchorPool
        {
            private static readonly Stack<AudioAnchor> pool = new Stack<AudioAnchor>(64);
            private static readonly List<AudioAnchor> actives = new List<AudioAnchor>(128);

            public static AudioAnchor Get(Vector3 position, Transform follow)
            {
                AudioAnchor a;
                
                // 풀에서 꺼낸 오브젝트가 유효한지 확인
                do
                {
                    a = pool.Count > 0 ? pool.Pop() : null;
                    
                    // 풀이 비었거나, 꺼낸 오브젝트가 파괴되었으면 새로 생성
                    if (a == null || a.gameObject == null)
                    {
                        a = Create();
                        break;
                    }
                }
                while (a.gameObject == null); // gameObject가 파괴된 경우 다시 시도
                
                a.transform.position = position;
                a.follow = follow;
                a.recycleAt = 0f;
                actives.Add(a);
                return a;
            }

            public static void RecycleAfter(AudioAnchor a, float seconds)
            {
                a.recycleAt = Time.time + seconds;
            }

            public static void RuntimeTick()
            {
                if (actives.Count == 0)
                    return;

                float now = Time.time;
                for (int i = actives.Count - 1; i >= 0; --i)
                {
                    AudioAnchor a = actives[i];

                    // gameObject가 파괴된 경우 리스트에서 제거 (풀에는 넣지 않음)
                    if (a.gameObject == null)
                    {
                        actives.RemoveAt(i);
                        continue;
                    }

                    if (a.follow != null)
                        a.transform.position = a.follow.position;

                    if (a.recycleAt > 0f && now >= a.recycleAt)
                    {
                        a.follow = null;
                        pool.Push(a);
                        actives.RemoveAt(i);
                    }
                }
            }

            private static AudioAnchor Create()
            {
                var go = new GameObject("FMOD_ImpactAnchor");
                go.hideFlags = HideFlags.HideAndDontSave;
                var a = new AudioAnchor
                {
                    gameObject = go,
                    transform = go.transform
                };
                return a;
            }
        }
    }
}


