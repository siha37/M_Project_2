using System.Collections;
using UnityEngine;
using UnityEngine.VFX;

namespace MyFolder._1._Scripts._12._Pool
{
    /// <summary>
    /// 충돌 이펙트 등 파티클 전용 풀.
    /// </summary>
    public class ParticleEffectPool : GameObjectPool
    {
        [SerializeField] private bool autoReturnOnStop = true;
        [SerializeField] private float fallbackLifetime = 2f;

        /// <summary>
        /// 위치와 회전을 설정한 뒤 파티클을 재생한다.
        /// </summary>
        public void PlayAt(Vector3 position, Quaternion rotation)
        {
            GameObject obj = Get(position, rotation);
            HandleAutoReturn(obj);
        }

        /// <summary>
        /// 위치에서 정방향(Vector2.right) 기준 회전값을 자동 계산해 재생한다.
        /// </summary>
        public void PlayAt(Vector3 position, Vector2 direction)
        {
            Quaternion rotation = Quaternion.identity;
            if (direction.sqrMagnitude > 0.001f)
            {
                rotation = Quaternion.LookRotation(Vector3.forward, Vector3.Cross(Vector3.forward, direction.normalized));
            }

            PlayAt(position, rotation);
        }

        protected override void OnBeforeUse(GameObject obj)
        {
            if (!obj.TryGetComponent(out VisualEffect ps))
            {
                return;
            }
            ps.Stop();
            ps.Play();
        }

        protected override void OnBeforeRelease(GameObject obj)
        {
            if (!obj.TryGetComponent(out VisualEffect ps))
            {
                return;
            }

            ps.Stop();
        }

        private void HandleAutoReturn(GameObject obj)
        {
            if (!autoReturnOnStop)
            {
                StartCoroutine(ReturnAfterDelay(obj, fallbackLifetime));
                return;
            }
            StartCoroutine(ReturnAfterDelay(obj, fallbackLifetime));
        }


        private IEnumerator ReturnAfterDelay(GameObject obj, float delay)
        {
            yield return new WaitForSeconds(delay);
            Release(obj);
        }
    }
}

