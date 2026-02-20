using MyFolder._1._Scripts._0._Object._0._Agent._0._Player;
using MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.Data;
using MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.Main.Components.Interface;
using MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.States;
using MyFolder._1._Scripts._3._SingleTone;
using UnityEngine;

namespace MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.Main.Components
{
    public class EnemyPercetion : IEnemyUpdateComponent
    {
        private EnemyConfig config;
        private EnemyControll agent;
        private EnemyMovement movement;
        private bool hasLineOfSight;
        private Vector3 lastSeenPosition;
        private float aiUpdateInterval;
        private float lastUpdateTime;
        private float currentUpdateInterval;
        private float lastDistanceCheckTime;
        private float distanceCheckInterval = 0.5f; // 거리 체크 주기
        private float lastVisionCheckTime;
        private float visionCheckInterval = 0.1f; // 시야 체크 주기
        private bool cachedLineOfSight;
        private bool retargeting_Able = true;
        private float retargeting_Cooltime = 4; 
        private float retargeting_Currnt_Cooltime = 0;
        private Vector2 lastFacingDirection = Vector2.right;
        
        public bool HasLineOfSight => hasLineOfSight;
        public Vector3 LastSeenPosition => lastSeenPosition;
        
        public void Init(EnemyControll agent)
        {
            this.agent = agent;
            config = agent.Config;
            movement = (EnemyMovement)agent.GetEnemyAllComponent(typeof(EnemyMovement));
            
            aiUpdateInterval = agent.Config.aiUpdateInterval;
            currentUpdateInterval = aiUpdateInterval;
            
            // 업데이트 시간 분산 - 각 적마다 다른 시작 오프셋 적용
            lastUpdateTime = Time.time + Random.Range(0f, currentUpdateInterval);
            lastDistanceCheckTime = Time.time + Random.Range(0f, distanceCheckInterval);
            lastVisionCheckTime = Time.time + Random.Range(0f, visionCheckInterval);
            
            agent.SetTarget(FindTarget());
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
            hasLineOfSight = false;
            lastSeenPosition = Vector3.zero;
            cachedLineOfSight = false;
            lastFacingDirection = Vector2.right;
    
            // 업데이트 시간 분산 재설정
            lastUpdateTime = Time.time + Random.Range(0f, currentUpdateInterval);
            lastDistanceCheckTime = Time.time + Random.Range(0f, distanceCheckInterval);
            lastVisionCheckTime = Time.time + Random.Range(0f, visionCheckInterval);
        }

        public void Update()
        {
            UpdateLODSystem();
            
            UpdatePlayerAliveCheck();
            
            UpdatePerception();

            UpdateAttackRange();

            ReTargetingCoolDown();
        }

        public void FixedUpdate()
        {
        }

        public void LateUpdate()
        {
        }

        public void SetTarget(GameObject target)
        {
            if(!retargeting_Able)
                return;
            retargeting_Able = false;
            agent.SetTarget(target);
        }
        
        /*==================Private ========================*/
        
        /// <summary>
        /// LOD 시스템 - 거리에 따라 업데이트 간격 조정
        /// </summary>
        private void UpdateLODSystem()
        {
            // 거리 체크 주기마다만 실행
            if (Time.time - lastDistanceCheckTime < distanceCheckInterval) return;
            
            lastDistanceCheckTime = Time.time;
            
            // 가장 가까운 플레이어와의 거리 계산
            float closestDistance = GetClosestPlayerDistance();
            
            // 거리에 따른 업데이트 간격 설정
            if (closestDistance <= config.closeRangeThreshold)
            {
                currentUpdateInterval = config.closeRangeUpdateInterval;
            }
            else if (closestDistance <= config.midRangeThreshold)
            {
                currentUpdateInterval = config.midRangeUpdateInterval;
            }
            else if (closestDistance <= config.farRangeThreshold)
            {
                currentUpdateInterval = config.farRangeUpdateInterval;
            }
            else
            {
                // 매우 먼 거리는 거의 업데이트하지 않음
                currentUpdateInterval = config.farRangeUpdateInterval * 2f;
            }
        }
        
        /// <summary>
        /// 가장 가까운 플레이어와의 거리 반환 (최적화된 버전)
        /// </summary>
        private float GetClosestPlayerDistance()
        {
            GameObject closestPlayer = EnemyTargetManager.Instance.GetClosestPlayer(agent.transform.position);
            
            if (closestPlayer)
            {
                return Vector2.Distance(agent.transform.position, closestPlayer.transform.position);
            }
            
            return float.MaxValue;
        }
        
        /// <summary>
        /// 인지 시스템 업데이트 (최적화된 버전)
        /// </summary>
        private void UpdatePerception()
        {
            // 시야 체크 간격마다만 실행
            if (Time.time - lastVisionCheckTime >= visionCheckInterval)
            {
                // 현재 타겟에 대한 시야 확인
                bool currentLineOfSight = false;
            
                if (agent.CurrentTarget)
                {
                    currentLineOfSight = LineOfSight(agent.CurrentTarget.transform);
                
                    if (currentLineOfSight)
                    {
                        lastSeenPosition = agent.CurrentTarget.transform.position;
                    }
                }
                
                cachedLineOfSight = currentLineOfSight;
                lastVisionCheckTime = Time.time;
            
                // 시야 상태 변경 확인
                if (!hasLineOfSight.Equals(cachedLineOfSight))
                {
                    hasLineOfSight = cachedLineOfSight;
                }
            }
        }

        /// <summary>
        /// 공격 가능 거리 판단 (결과만 내부에 저장, BT의 Blackboard가 읽어감)
        /// </summary>
        private void UpdateAttackRange()
        {
            // BT가 Blackboard(HasLineOfSight, DistanceToTarget 등)를 통해 의사결정하므로
            // 여기서는 추가 상태 전환 로직 없이, Perception 값만 유지한다.
        }

        private void UpdatePlayerAliveCheck()
        {
            if (Time.time - lastUpdateTime >= currentUpdateInterval)
            {
                if (agent.CurrentTarget && agent.CurrentTarget.TryGetComponent(out PlayerNetworkSync playersync))
                {
                    if (playersync.IsDead() || !playersync.IsCanSee() || playersync.IsQuesting())
                    {
                        agent.SetTarget(FindTarget());
                    }
                }
                else
                {
                    agent.SetTarget(FindTarget());
                }
                lastUpdateTime = Time.time;
            }
        }

        private GameObject FindTarget()
        {
            GameObject target = null;
            if (agent.IsQuestEnemy)
            {
                if (agent.QuestType == MyFolder._1._Scripts._6._GlobalQuest.GlobalQuestType.Defense && agent.DefencePriorityTarget)
                {
                    target = agent.DefencePriorityTarget.gameObject;
                }
                if (!target)
                {
                    target = EnemyTargetManager.Instance.GetClosestQuestPlayer(agent.transform.position, agent.QuestId);
                }
            }
            else
            {
                target = EnemyTargetManager.Instance.GetClosestNonQuestingPlayer(agent.transform.position);
            }

            return target;
        }
        
        /// <summary>
        /// 특정 타겟에 대한 시야 확인
        /// </summary>
        private bool LineOfSight(Transform target)
        {
            if (!target) return false;
        
            // 거리 확인 (2D 거리 계산)
            float distance = Vector2.Distance(agent.transform.position, target.position);
            if (distance > agent.Status.EnemyData.detectionRange) return false;
        
            // 시야각 확인
            if (!IsInFieldOfView(target.position)) return false;
        
            // 장애물 확인
            return !IsObstructed(target.position);
        }
    
        
        /// <summary>
        /// 특정 위치가 시야각 내에 있는지 확인
        /// 기준 방향: 적의 실제 이동 방향 (정지 시 마지막 이동 방향 유지)
        /// </summary>
        private bool IsInFieldOfView(Vector3 position)
        {
            Vector2 directionToTarget = ((Vector2)position - (Vector2)agent.transform.position).normalized;
        
            Vector2 baseDirection;
            if (movement != null && ((Vector2)movement.moveDirection).sqrMagnitude > 0.01f)
            {
                baseDirection = ((Vector2)movement.moveDirection).normalized;
                lastFacingDirection = baseDirection;
            }
            else
            {
                baseDirection = lastFacingDirection;
            }
        
            float angle = Vector2.Angle(baseDirection, directionToTarget);
        
            return angle <= agent.Status.EnemyData.fieldOfViewAngle * 0.5f;
        }
        
        /// <summary>
        /// 특정 위치가 장애물에 가려져 있는지 확인
        /// </summary>
        private bool IsObstructed(Vector3 position)
        {
        
            Vector2 direction = ((Vector2)position - (Vector2)agent.transform.position).normalized;
            float distance = Vector2.Distance(agent.transform.position, position);
        
            // 레이캐스트로 장애물 확인 (2D)
            RaycastHit2D hit = Physics2D.Raycast(agent.transform.position, direction, distance, config.obstacleLayer);
            if (hit.collider)
            {
                return true; // 장애물이 있음
            }
        
            return false; // 장애물이 없음
        }

        /// <summary>
        /// 어그로 제약
        /// </summary>
        private void ReTargetingCoolDown()
        {
            if (!retargeting_Able)
            {
                if (retargeting_Currnt_Cooltime >= retargeting_Cooltime)
                {
                    retargeting_Able = true;
                    return;
                }
                retargeting_Currnt_Cooltime += Time.deltaTime;
            }
        }
    }
}