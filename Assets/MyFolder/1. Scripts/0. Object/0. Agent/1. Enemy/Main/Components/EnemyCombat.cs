using MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.Data;
using MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.Main.Components.Interface;
using MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.States;
using UnityEngine;

namespace MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.Main.Components
{
    public class EnemyCombat: IEnemyUpdateComponent
    {
        private EnemyConfig config;
        private EnemyControll agent;
        private bool AttackAble = false;
        private bool IsReloading = false;
        private float lastAttackTime = 0;
        private float shotAngle;
        private float finalAngle;
        
        public bool CanShot => AttackAble && !IsReloading && agent?.Status?.bulletCurrentCount > 0;
        
        public void Init(EnemyControll agent)
        {
            this.agent = agent;
            config = agent.Config;
            agent.NetworkSync.ReloadCompleteEvent = ReloadComplete;
        }

        public void OnEnable()
        {
            if(agent)
                Init(agent);
        }

        public void OnDisable()
        {
        }

        public void OnDestroy()
        {
            
        }

        public void ChangedState(IEnemyState oldstate, IEnemyState newstate)
        {
        }

        public void Reset()
        {
            AttackAble = false;
            IsReloading = false;
            lastAttackTime = 0;
            shotAngle = 0;
            finalAngle = 0;
        }

        public void Update()
        {
            if(!agent) return;
            if(!agent.CurrentTarget) return;

            ShotAngleUpdate();
            FireUpdate();
        }

        public void FixedUpdate()
        {
        }

        public void LateUpdate()
        {
        }

        public void AttackOn()
        {
            AttackAble = true;
        }

        public void AttackOff()
        {
            AttackAble = false;
        }

        /*==================Private ========================*/

        private void ShotAngleUpdate()
        {
            Vector2 direction = (agent.CurrentTarget.transform.position - agent.transform.position).normalized;
            shotAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            // 조준 정밀도 적용 (약간의 오차 추가)
            float aimPrecision = agent.Status.EnemyData.aimPrecision;
            float aimError = Random.Range(-aimPrecision * 90f, aimPrecision * 90f);
            finalAngle = shotAngle + aimError;
            ShotObjectAngleUpdate(shotAngle);
            agent.NetworkSync?.RequestUpdateLookAngleForEnemy(shotAngle);
        }

        public void ShotObjectAngleUpdate(float angle)
        {
            agent.ShotPivot.rotation = Quaternion.Euler(0,0,angle);
        }

        private void FireUpdate()
        {
            if (!CanShot) return;
            if (Time.time - lastAttackTime >= agent.Status.GetShootingData.shotDelay)
            {
                lastAttackTime = Time.time;
                if (agent.NetworkSync)
                {
                    int count = 0;
                    count = agent.Status.bulletCurrentCount - agent.Status.GetShootingData.burstCount < 0
                            ? (int)agent.Status.bulletCurrentCount
                            : agent.Status.GetShootingData.burstCount;
                     
                    for (int i = 0; i < count; i++)
                    {
                        float newAngle = ShotAngleRange(finalAngle);
                        agent.NetworkSync.ShootEnemyBullet(newAngle, agent.ShotPoint.position);   
                    }
                    if (agent.Status.bulletCurrentCount <= 0)
                        Reloading();
                }
            }
        }
        
        

        private float ShotAngleRange(float angle)
        {
            float aimPrecision = agent.Status.GetShootingData.shotAngle;
            float aimError = Random.Range(-aimPrecision, aimPrecision);
            return angle+aimError;
        }

        private void Reloading()
        {
            IsReloading = true;
            agent.NetworkSync.RequestReload();
        }
        private void ReloadComplete()
        {
            IsReloading = false;
        }
    }
}