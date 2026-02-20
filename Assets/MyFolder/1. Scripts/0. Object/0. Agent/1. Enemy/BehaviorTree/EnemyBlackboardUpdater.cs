using BehaviorDesigner.Runtime;
using MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.Main;
using MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.Main.Components;
using UnityEngine;

namespace MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.BehaviorTree
{
    /// <summary>
    /// EnemyPercetion의 계산 결과를 Behavior Designer Shared Variables(블랙보드)에 기록한다.
    /// EnemyControll과 같은 GameObject에 부착하여 사용.
    /// </summary>
    public class EnemyBlackboardUpdater : MonoBehaviour
    {
        private BehaviorDesigner.Runtime.BehaviorTree behaviorTree;
        private EnemyControll agent;
        private EnemyPercetion perception;

        private SharedGameObject sharedCurrentTarget;
        private SharedBool sharedHasLineOfSight;
        private SharedFloat sharedDistanceToTarget;
        private SharedFloat sharedAttackRange;
        private SharedFloat sharedHealthRatio;
        private SharedFloat sharedCoverCooldownEndTime;

        private float updateInterval = 0.1f;
        private float lastUpdateTime;

        public void Initialize(EnemyControll enemyControll)
        {
            agent = enemyControll;
            behaviorTree = GetComponent<BehaviorDesigner.Runtime.BehaviorTree>();
            perception = agent.GetEnemyAllComponent(typeof(EnemyPercetion)) as EnemyPercetion;

            if (!behaviorTree) return;

            CacheVariables();
        }

        private void CacheVariables()
        {
            sharedCurrentTarget = (SharedGameObject)behaviorTree.GetVariable("CurrentTarget");
            sharedHasLineOfSight = (SharedBool)behaviorTree.GetVariable("HasLineOfSight");
            sharedDistanceToTarget = (SharedFloat)behaviorTree.GetVariable("DistanceToTarget");
            sharedAttackRange = (SharedFloat)behaviorTree.GetVariable("AttackRange");
            sharedHealthRatio = (SharedFloat)behaviorTree.GetVariable("HealthRatio");
            sharedCoverCooldownEndTime = (SharedFloat)behaviorTree.GetVariable("CoverCooldownEndTime");
        }

        private void Update()
        {
            if (!behaviorTree || !agent) return;
            if (Time.time - lastUpdateTime < updateInterval) return;

            lastUpdateTime = Time.time;
            UpdateBlackboard();
        }

        private void UpdateBlackboard()
        {
            if (sharedCurrentTarget != null)
                sharedCurrentTarget.Value = agent.CurrentTarget;

            if (sharedHasLineOfSight != null && perception != null)
                sharedHasLineOfSight.Value = perception.HasLineOfSight;

            if (sharedDistanceToTarget != null && agent.CurrentTarget)
                sharedDistanceToTarget.Value = Vector2.Distance(
                    agent.transform.position, agent.CurrentTarget.transform.position);
            else if (sharedDistanceToTarget != null)
                sharedDistanceToTarget.Value = float.MaxValue;

            if (sharedAttackRange != null && agent.Status?.EnemyData != null)
                sharedAttackRange.Value = agent.Status.EnemyData.attackRange;

            if (sharedHealthRatio != null && agent.Status)
            {
                float maxHp = agent.Status.EnemyData?.hp ?? 1f;
                sharedHealthRatio.Value = maxHp > 0 ? agent.Status.currentHp / maxHp : 0f;
            }
        }

        public void ResetBlackboard()
        {
            if (!behaviorTree) return;

            CacheVariables();

            if (sharedCurrentTarget != null) sharedCurrentTarget.Value = null;
            if (sharedHasLineOfSight != null) sharedHasLineOfSight.Value = false;
            if (sharedDistanceToTarget != null) sharedDistanceToTarget.Value = float.MaxValue;
            if (sharedAttackRange != null) sharedAttackRange.Value = 5f;
            if (sharedHealthRatio != null) sharedHealthRatio.Value = 1f;
            if (sharedCoverCooldownEndTime != null) sharedCoverCooldownEndTime.Value = 0f;
        }
    }
}
