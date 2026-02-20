using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.Main;
using MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.Main.Components;
using UnityEngine;
using UnityEngine.AI;

namespace MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.BehaviorTree.Actions
{
    [TaskCategory("Enemy")]
    [TaskDescription("사격하면서 타겟 주변을 기동 (스트레이프)")]
    public class EnemyStrafeAttack : Action
    {
        [UnityEngine.Tooltip("현재 타겟")]
        public SharedGameObject CurrentTarget;

        [UnityEngine.Tooltip("기동 반경 (타겟으로부터의 유지 거리)")]
        public SharedFloat StrafeRadius = 5f;

        [UnityEngine.Tooltip("웨이포인트 도착 판정 거리")]
        public SharedFloat ArriveThreshold = 0.8f;

        [UnityEngine.Tooltip("방향 전환 최소 간격 (초)")]
        public SharedFloat DirectionChangeMinInterval = 1.5f;

        [UnityEngine.Tooltip("방향 전환 최대 간격 (초)")]
        public SharedFloat DirectionChangeMaxInterval = 3.5f;

        private EnemyControll agent;
        private EnemyCombat combat;
        private EnemyMovement movement;

        private Vector3 currentWaypoint;
        private int strafeSign = 1;
        private float nextDirectionChangeTime;
        private float strafeAngleStep = 40f;
        private float originalStoppingDistance;

        public override void OnAwake()
        {
            agent = Owner.GetComponent<EnemyControll>();
        }

        public override void OnStart()
        {
            combat = agent.GetEnemyAllComponent(typeof(EnemyCombat)) as EnemyCombat;
            movement = agent.GetEnemyAllComponent(typeof(EnemyMovement)) as EnemyMovement;

            if (combat != null)
                combat.AttackOn();

            if (movement != null)
            {
                originalStoppingDistance = agent.Status.EnemyData.stoppingDistance;
                movement.SetSpeed(agent.Status.EnemyData.speed);
                movement.SetStoppingDistance(0.3f);
                movement.ManualControl = true;
                movement.OnMove = true;
            }

            strafeSign = Random.value > 0.5f ? 1 : -1;
            ScheduleDirectionChange();
            PickNextWaypoint();
        }

        public override TaskStatus OnUpdate()
        {
            if (!CurrentTarget.Value || movement == null)
                return TaskStatus.Failure;

            float distToTarget = Vector2.Distance(agent.transform.position, CurrentTarget.Value.transform.position);
            if (distToTarget > agent.Status.EnemyData.attackRange)
                return TaskStatus.Failure;

            if (Time.time >= nextDirectionChangeTime)
            {
                strafeSign *= -1;
                ScheduleDirectionChange();
            }

            float distToWaypoint = Vector2.Distance(agent.transform.position, currentWaypoint);
            if (distToWaypoint <= ArriveThreshold.Value)
                PickNextWaypoint();

            movement.MoveTo(currentWaypoint);
            return TaskStatus.Running;
        }

        public override void OnEnd()
        {
            if (combat != null)
                combat.AttackOff();

            if (movement != null)
            {
                movement.SetStoppingDistance(originalStoppingDistance);
                movement.ManualControl = false;
                movement.OnMove = false;
            }
        }

        private void PickNextWaypoint()
        {
            Vector2 agentPos = agent.transform.position;
            Vector2 targetPos = CurrentTarget.Value.transform.position;
            Vector2 toAgent = (agentPos - targetPos).normalized;

            float currentAngle = Mathf.Atan2(toAgent.y, toAgent.x) * Mathf.Rad2Deg;
            float nextAngle = currentAngle + strafeAngleStep * strafeSign;
            float rad = nextAngle * Mathf.Deg2Rad;

            float radius = StrafeRadius.Value + Random.Range(-1f, 1f);
            Vector2 candidate = targetPos + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * radius;

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 3f, NavMesh.AllAreas))
                currentWaypoint = hit.position;
            else
                currentWaypoint = agent.transform.position;
        }

        private void ScheduleDirectionChange()
        {
            nextDirectionChangeTime = Time.time +
                Random.Range(DirectionChangeMinInterval.Value, DirectionChangeMaxInterval.Value);
        }

        public override void OnReset()
        {
            CurrentTarget = null;
            StrafeRadius = 5f;
            ArriveThreshold = 0.8f;
            DirectionChangeMinInterval = 1.5f;
            DirectionChangeMaxInterval = 3.5f;
        }
    }
}
