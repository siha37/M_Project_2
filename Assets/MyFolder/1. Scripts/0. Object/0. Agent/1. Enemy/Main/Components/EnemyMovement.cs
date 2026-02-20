using MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.Main.Components.Interface;
using MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.States;
using MyFolder._1._Scripts._3._SingleTone;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.Main.Components
{
    public class EnemyMovement : IEnemyUpdateComponent
    {
        #region Variables
        
        // Components
        private NavMeshAgent navMeshAgent;
        private EnemyControll agent;
        private Transform agentTf;
        
        // Control
        public bool OnMove = false;
        public bool ManualControl = false;
        
        // Timing
        private float lastPathUpdateTime;
        private float pathUpdateInterval;
        
        public Vector3 moveDirection = Vector3.zero;
        
        #endregion
        
        #region Init
        
        public void Init(EnemyControll agent)
        {
            this.agent = agent;
            agentTf = agent.transform;
            moveDirection = Vector3.zero;
            
            if (!agent.TryGetComponent(out navMeshAgent))
                LogManager.LogError(LogCategory.Enemy, $"The agent is set to navMeshAgent : {navMeshAgent}");
            else
            {
                navMeshAgent.updateRotation = false;
                agentTf.rotation = Quaternion.identity;
                navMeshAgent.updateUpAxis = false;
                navMeshAgent.updatePosition = true;
                this.agent.Status.OnDataRefreshed += StatInit_DataWait;
            }
            
            StatInit_DataWait();
        }
        
        public void OnEnable()
        {
            if (agent)
                Init(agent);
        }
        
        public void OnDisable() { 
            if (this.agent)
                this.agent.Status.OnDataRefreshed -= StatInit_DataWait;
        }
        
        public void OnDestroy()
        {
            if (this.agent)
                this.agent.Status.OnDataRefreshed -= StatInit_DataWait;
        }
        
        public void ChangedState(IEnemyState oldstate, IEnemyState newstate) { }
        public void Reset()
        {
            OnMove = false;
            ManualControl = false;
            lastPathUpdateTime = 0;
            pathUpdateInterval = agent.Config.aiUpdateInterval;
            
            
            if (navMeshAgent && navMeshAgent.enabled && navMeshAgent.isOnNavMesh)
            {
                navMeshAgent.ResetPath();
                navMeshAgent.velocity = Vector3.zero;
                navMeshAgent.isStopped = false;
            }
            else if (navMeshAgent)
            {
                navMeshAgent.velocity = Vector3.zero;
            }
        }

        public void StatInit_DataWait()
        {
            if (!agent) return;

            if (!ManualControl)
            {
                SetSpeed(this.agent.Status.EnemyData.speed);
                SetStoppingDistance(this.agent.Status.EnemyData.stoppingDistance);
            }
            
            lastPathUpdateTime = Time.time + Random.Range(0f, agent.Config.aiUpdateInterval * 0.5f);
            pathUpdateInterval = agent.Config.aiUpdateInterval;
        }
        
        #endregion
        
        #region Update
        
        public void Update()
        {
            if (navMeshAgent && navMeshAgent.hasPath)
                moveDirection = navMeshAgent.velocity;
            if (OnMove && !ManualControl && agent.CurrentTarget && Time.time - lastPathUpdateTime >= pathUpdateInterval)
            {
                MoveTo(agent.CurrentTarget.transform.position);
                lastPathUpdateTime = Time.time;
            }
            else if (!OnMove)
            {
                MoveStop();
            }
        }
        
        public void FixedUpdate()
        {
            if (!OnMove)
            {
                MoveStop();
                return;
            }
            
            ValidateNavMeshPosition();
        }
        
        public void LateUpdate() { }
        
        #endregion
        
        #region Movement
        
        /// <summary>
        /// NavMesh 위치 유효성 검사 및 보정
        /// </summary>
        private void ValidateNavMeshPosition()
        {
            if (!NavMesh.SamplePosition(agentTf.position, out NavMeshHit currentHit, navMeshAgent.height * 2f, NavMesh.AllAreas))
            {
                if (NavMesh.SamplePosition(agentTf.position, out NavMeshHit nearestHit, 10f, NavMesh.AllAreas))
                {
                    agentTf.position = nearestHit.position;
                    
                    if (OnMove && !ManualControl && agent.CurrentTarget)
                        MoveTo(agent.CurrentTarget.transform.position);
                }
            }
        }
        
        /// <summary>
        /// 이동 정지
        /// </summary>
        private void MoveStop()
        {
            if (navMeshAgent && navMeshAgent.isOnNavMesh)
            {
                navMeshAgent.ResetPath();
                navMeshAgent.velocity = Vector3.zero;
                moveDirection = Vector3.zero;
            }
        }
        
        #endregion
        
        #region Settings
        
        /// <summary>
        /// 목적지 설정
        /// </summary>
        public void MoveTo(Vector3 destination)
        {
            if (!agent || !navMeshAgent)
                return;
            
            if (!navMeshAgent.isOnNavMesh)
                return;
            
            if (!NavMesh.SamplePosition(destination, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                return;
            
            navMeshAgent.SetDestination(hit.position);
        }
        
        /// <summary>
        /// 이동 속도 설정
        /// </summary>
        public void SetSpeed(float speed)
        {
            if (navMeshAgent)
                navMeshAgent.speed = speed;
        }
        
        /// <summary>
        /// 멈추는 거리 설정
        /// </summary>
        public void SetStoppingDistance(float stopDistance)
        {
            if (navMeshAgent)
                navMeshAgent.stoppingDistance = stopDistance;
        }
        
        #endregion
        
        #region Controller
        
        /// <summary>
        /// 이동 정지
        /// </summary>
        public void Stop()
        {
            MoveStop();
            OnMove = false;
        }
        
        /// <summary>
        /// 이동 재개
        /// </summary>
        public void Resume()
        {
            OnMove = true;
        }
        
        #endregion
    }
}

