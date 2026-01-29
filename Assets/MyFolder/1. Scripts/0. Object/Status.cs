using System.Collections;
using FishNet.Object;
using MyFolder._1._Scripts._3._SingleTone;
using UnityEngine;

namespace MyFolder._1._Scripts._0._Object
{
    public abstract class Status : MonoBehaviour
    {
        protected ObjectData data;
        
        public float currentHp;
        public bool isDead = false;

        // Lazy Loading 관련
        protected bool _dataLoaded = false;

        public bool DataLoaded => _dataLoaded;

        protected bool _isLoadingData = false;
        
        public bool IsDead => isDead;
        public ObjectData Data => data;
        public ObjectData SetData { set { data = value; } }

        private ushort DataId = 1;
        
        public ushort SetDataId(ushort id) => DataId = id; 
        
        // 데이터 초기화를 위한 추상 메서드
        protected abstract void InitializeData();
        
        protected virtual void Start()
        {
            // 데이터 초기화 먼저
            InitializeData();
            
            // 체력 설정
            if (data != null)
            {
                currentHp = data.hp;
            }
        }

        /// <summary>
        /// 기본 피해 적용 함수
        /// </summary>
        /// <param name="damage"></param>
        /// <param name="hitDirection"></param>
        public virtual bool TakeDamage(float damage, Vector2 hitDirection = default)
        {
            if (isDead) return false;
        
            currentHp -= damage;
            return false;
        }


        protected bool CanLoadData()
        {
            return GameDataManager.Instance && 
                   GameDataManager.Instance.IsDataInitialized;
        }

        protected virtual ushort GetDataId()
        {
            return DataId;
        }
        /// <summary>
        /// 기절 상태 - 색상만 변경
        /// </summary>
        /// <returns></returns>
        protected virtual IEnumerator DeathSequence()
        {
            isDead = true;

            // 사망 처리
            OnRealDeath();

            yield return null;
        }

        /// <summary>
        /// 오브젝트 삭제
        /// </summary>
        public virtual void OnRealDeath()
        {
            // 기본 사망 처리 - 오브젝트 제거
            Destroy(gameObject);
        }
    }
}
