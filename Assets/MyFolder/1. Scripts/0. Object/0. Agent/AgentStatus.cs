using System;
using MyFolder._1._Scripts._0._Object._4._Shooting;
using MyFolder._1._Scripts._7._PlayerRole;
using UnityEngine;

namespace MyFolder._1._Scripts._0._Object._0._Agent
{
    public class AgentStatus : Status
    {
        //Data
        protected ShootingData shooting_data = new();
        
        //동적 속성 값
        public float bulletCurrentCount;
        
        //Gettor
        public ShootingData GetShootingData => shooting_data;
        public ShootingData SetShootingData { set { shooting_data = value; } }
        public AgentData AgentData => data as AgentData;

        protected override void InitializeData()
        {
            // 기본 AgentData로 초기화
            if (data == null)
            {
                data = CreateDefaultAgentData();
            }
            
            // ShootingData 로드
            LoadShootingData();
        }
        
        protected virtual AgentData CreateDefaultAgentData()
        {
            return new AgentData();
        }
        
        protected virtual void LoadShootingData()
        {
            // 기본 ShootingData
            shooting_data = new ShootingData();
        }

        protected override void Start()
        {
            base.Start();
            bulletCurrentCount = shooting_data.magazineCapacity;
        
            // ✅ UI 초기화는 NetworkSync에서 처리
        }

        protected virtual void DataLoad()
        {
            // 호환성을 위해 유지하지만 더 이상 사용하지 않음
        }

        /// <summary>
        /// 피해 적용 및 죽음 처리
        /// </summary>
        /// <param name="damage"></param>
        /// <param name="hitDirection"></param>
        // ✅ UI 업데이트 완전 제거, 순수 데미지 계산만
        public override bool TakeDamage(float damage, Vector2 hitDirection = default)
        {
            if (isDead) return false;
        
            currentHp -= damage;
            currentHp = Mathf.Clamp(currentHp, 0, data.hp);
        
            if (currentHp <= 0)
            {
                isDead = true;
            }

            return false;
        }

        // ✅ UI 업데이트 완전 제거, 순수 탄약 계산만
        public virtual void UpdateBulletCount(float count)
        {
            bulletCurrentCount += count;
            bulletCurrentCount = Mathf.Clamp(bulletCurrentCount, 0, shooting_data.magazineCapacity);
        }

    }
}
