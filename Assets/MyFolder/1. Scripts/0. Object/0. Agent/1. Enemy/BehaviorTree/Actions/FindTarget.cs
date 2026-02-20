using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using MyFolder._1._Scripts._0._Object._0._Agent._0._Player;
using MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.Main;
using MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.Main.Components;
using MyFolder._1._Scripts._6._GlobalQuest;
using UnityEngine;

namespace MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.BehaviorTree.Actions
{
    [TaskCategory("Enemy")]
    [TaskDescription("가장 가까운 유효한 타겟을 탐색하여 Blackboard에 설정")]
    public class FindTarget : Action
    {
        [UnityEngine.Tooltip("탐색된 타겟이 저장될 변수")]
        public SharedGameObject CurrentTarget;

        private EnemyControll agent;

        public override void OnAwake()
        {
            agent = Owner.GetComponent<EnemyControll>();
        }

        public override TaskStatus OnUpdate()
        {
            if (agent == null)
                return TaskStatus.Failure;

            GameObject target = null;

            if (agent.IsQuestEnemy)
            {
                if (agent.QuestType == GlobalQuestType.Defense
                    && agent.DefencePriorityTarget)
                {
                    target = agent.DefencePriorityTarget.gameObject;
                }

                if (target == null)
                {
                    target = EnemyTargetManager.Instance.GetClosestQuestPlayer(
                        agent.transform.position, agent.QuestId);
                }
            }
            else
            {
                target = EnemyTargetManager.Instance.GetClosestNonQuestingPlayer(
                    agent.transform.position);
            }

            if (target != null && target.TryGetComponent(out PlayerNetworkSync playerSync))
            {
                if (playerSync.IsDead() || !playerSync.IsCanSee() || playerSync.IsQuesting())
                    target = null;
            }

            agent.SetTarget(target);
            CurrentTarget.Value = target;

            return target != null ? TaskStatus.Success : TaskStatus.Failure;
        }

        public override void OnReset()
        {
            CurrentTarget = null;
        }
    }
}
