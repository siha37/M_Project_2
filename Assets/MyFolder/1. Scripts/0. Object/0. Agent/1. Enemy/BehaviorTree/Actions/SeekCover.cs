using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.Main;
using MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.Main.Components;
using UnityEngine;
using UnityEngine.AI;

namespace MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.BehaviorTree.Actions
{
    [TaskCategory("Enemy")]
    [TaskDescription("엄폐물을 찾아 이동")]
    public class SeekCover : Action
    {
        [UnityEngine.Tooltip("엄폐 탐색 최대 거리")]
        public SharedFloat MaxCoverDistance = 10f;

        [UnityEngine.Tooltip("엄폐물 레이어")]
        public LayerMask ObstacleLayer;

        [UnityEngine.Tooltip("레이캐스트 횟수")]
        public SharedInt MaxRaycasts = 12;

        [UnityEngine.Tooltip("엄폐 오프셋 (벽에서 떨어질 거리)")]
        public SharedFloat CoverOffset = 1f;

        [UnityEngine.Tooltip("도착 판정 거리")]
        public SharedFloat ArriveDistance = 0.5f;

        [UnityEngine.Tooltip("엄폐 후 쿨다운 시간 (초)")]
        public SharedFloat CoverCooldown = 5f;

        [UnityEngine.Tooltip("쿨다운 종료 시각 (Blackboard, NeedCover와 공유)")]
        public SharedFloat CoverCooldownEndTime;

        public SharedGameObject CurrentTarget;

        private EnemyControll agent;
        private EnemyMovement movement;
        private Vector3 coverPoint;
        private bool foundCover;
        private float originalStoppingDistance;

        public override void OnAwake()
        {
            agent = Owner.GetComponent<EnemyControll>();
        }

        public override void OnStart()
        {
            movement = agent.GetEnemyAllComponent(typeof(EnemyMovement)) as EnemyMovement;
            foundCover = false;

            if (movement == null || CurrentTarget.Value == null) return;

            foundCover = FindCoverPoint(out coverPoint);

            if (foundCover)
            {
                originalStoppingDistance = agent.Status.EnemyData.stoppingDistance;
                movement.SetSpeed(agent.Status.EnemyData.speed);
                movement.SetStoppingDistance(0.3f);
                movement.ManualControl = true;
                movement.OnMove = true;
                movement.MoveTo(coverPoint);
            }
        }

        public override TaskStatus OnUpdate()
        {
            if (!foundCover || movement == null)
                return TaskStatus.Failure;

            float dist = Vector2.Distance(agent.transform.position, coverPoint);
            if (dist <= ArriveDistance.Value)
            {
                if (CoverCooldownEndTime != null)
                    CoverCooldownEndTime.Value = Time.time + CoverCooldown.Value;
                return TaskStatus.Success;
            }

            return TaskStatus.Running;
        }

        public override void OnEnd()
        {
            if (movement != null)
            {
                movement.SetStoppingDistance(originalStoppingDistance);
                movement.ManualControl = false;
                movement.OnMove = false;
            }
        }

        /// <summary>
        /// 2D 레이캐스트로 타겟 반대편의 엄폐 지점을 탐색
        /// </summary>
        private bool FindCoverPoint(out Vector3 point)
        {
            point = Vector3.zero;
            Vector2 agentPos = agent.transform.position;
            Vector2 targetPos = CurrentTarget.Value.transform.position;
            Vector2 awayDir = (agentPos - targetPos).normalized;

            float angleStep = 360f / MaxRaycasts.Value;

            for (int i = 0; i < MaxRaycasts.Value; i++)
            {
                float angle = i * angleStep;
                Vector2 dir = Quaternion.Euler(0, 0, angle) * awayDir;

                RaycastHit2D hit = Physics2D.Raycast(agentPos, dir, MaxCoverDistance.Value, ObstacleLayer);
                if (hit.collider != null)
                {
                    Vector2 candidate = hit.point + hit.normal * CoverOffset.Value;

                    if (!IsVisibleFromTarget(candidate, targetPos)
                        && NavMesh.SamplePosition(candidate, out NavMeshHit navHit, 2f, NavMesh.AllAreas))
                    {
                        point = navHit.position;
                        return true;
                    }
                }
            }

            return false;
        }

        private bool IsVisibleFromTarget(Vector2 position, Vector2 targetPos)
        {
            Vector2 dir = (position - targetPos).normalized;
            float dist = Vector2.Distance(position, targetPos);
            RaycastHit2D hit = Physics2D.Raycast(targetPos, dir, dist, ObstacleLayer);
            return hit.collider == null;
        }

        public override void OnReset()
        {
            MaxCoverDistance = 10f;
            MaxRaycasts = 12;
            CoverOffset = 1f;
            ArriveDistance = 0.5f;
            CurrentTarget = null;
        }
    }
}
