using System;
using System.Collections.Generic;
using FishNet.Component.Transforming;
using FishNet.Object;
using MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.Data;
using MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.Main.Components;
using MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.Main.Components.Interface;
using MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.States;
using MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.Status;
using MyFolder._1._Scripts._0._Object._1._Spawner;
using MyFolder._1._Scripts._3._SingleTone;
using Spine.Unity;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.Main
{
    /// <summary>
    /// 적 AI 컨트롤러
    /// - FishNet 네트워크 동기화
    /// - 컴포넌트 기반 아키텍처 (Movement, Combat, Perception)
    /// - 상태 머신 관리
    /// - 오브젝트 풀링 지원 (FishNet 내장)
    /// </summary>
    public class EnemyControll : NetworkBehaviour
    {
        #region Fields - Components

        private EnemyStateMachine stateMachine;
        private EnemyStatus status;
        private EnemyNetworkSync networkSync;
        private NavMeshAgent navagent;
        private NetworkEnemySpawner networkEnemySpawner;
        private NetworkQuestEnemySpawner networkQuestEnemySpawner;
        [SerializeField] private SkeletonAnimation skeletonAnimation;
        [SerializeField] private EnemyAnimationSet enemyAnimationSet;

        #endregion

        #region Fields - Component System

        private Dictionary<Type, IEnemyComponent> AllComponents = new Dictionary<Type, IEnemyComponent>();
        private Dictionary<Type, IEnemyComponent> EnemyComponents = new Dictionary<Type, IEnemyComponent>();
        private List<IEnemyUpdateComponent> UpdateComponents = new List<IEnemyUpdateComponent>();

        #endregion

        #region Fields - Serialized

        [Header("===Config Setting===")]
        [SerializeField] private EnemyConfig config;

        [Header("===Object Setting===")]
        [SerializeField] private Transform shotPivot;
        [SerializeField] private Transform shotPoint;

        [Header("===Debug Setting===")]
        [SerializeField] private bool debugGizmos = true;
        [SerializeField] private bool debugLog = true;

        [Header("===Runtime Data===")]
        [SerializeField] private GameObject currentTarget;

        [Header("===Quest Meta===")]
        [SerializeField] private bool isQuestEnemy;
        [SerializeField] private int questId = -1;
        [SerializeField] private MyFolder._1._Scripts._6._GlobalQuest.GlobalQuestType questType;
        [SerializeField] private Transform defencePriorityTarget;

        #endregion

        #region Fields - Private

        private bool isFirstInitialization = true;

        #endregion

        #region Properties

        public EnemyConfig Config => config;
        public GameObject CurrentTarget => currentTarget;
        public Vector3 TargetDirection => !currentTarget ? Vector3.zero : (currentTarget.transform.position - transform.position).normalized;
        public EnemyStateMachine StateMachine => stateMachine;
        public EnemyNetworkSync NetworkSync => networkSync;
        public EnemyStatus Status => status;    
        public Transform ShotPivot => shotPivot;
        public Transform ShotPoint => shotPoint;
        public SkeletonAnimation SkeletonAnimation => skeletonAnimation;
        public EnemyAnimationSet EnemyAnimationSet => enemyAnimationSet;

        public bool IsQuestEnemy => isQuestEnemy && networkQuestEnemySpawner;
        public int QuestId => questId;
        public MyFolder._1._Scripts._6._GlobalQuest.GlobalQuestType QuestType => questType;
        public Transform DefencePriorityTarget => defencePriorityTarget;

        #endregion

        #region Lifecycle - Network

        /// <summary>
        /// 서버에서 시작 시 호출
        /// - 처음 생성: Init() 실행
        /// - 풀에서 재사용: ResetForReuse() 실행
        /// </summary>
        public override void OnStartServer()
        {
            if (isFirstInitialization)
            {
                // 처음 생성 시
                Init();
                Status.OnDataRefreshed += WaitData;
                isFirstInitialization = false;
            }
            else
            {
                // 풀에서 재사용 시 (FishNet이 자동으로 OnStartServer 호출)
                ResetForReuse();
            }
        }

        /// <summary>
        /// 서버에서 중지 시 호출 (풀로 반환될 때)
        /// </summary>
        public override void OnStopServer()
        {
            base.OnStopServer();
            CleanupForPool();
        }

        /// <summary>
        /// 클라이언트에서 시작 시 호출
        /// </summary>
        public override void OnStartClient()
        {
            if (IsServerInitialized) return;

            // 처음 생성 시
            if (isFirstInitialization)
            {
                Init();
                ClientComponentInit();
                stateMachine.enabled = false;
                navagent.enabled = false;
                transform.rotation = Quaternion.identity;
                isFirstInitialization = false;
            }
            // 풀에서 재사용 시
            else
            {
                ResetForClientReuse();
            }
        }

        /// <summary>
        /// 클라이언트에서 중지 시 호출 (풀로 반환될 때)
        /// </summary>
        public override void OnStopClient()
        {
            base.OnStopClient();
            
            if (IsServerInitialized) return;  // 서버면 OnStopServer에서 처리
            
            CleanupForClientPool();
        }

        #endregion

        #region Lifecycle - Unity

        private void Update()
        {
            foreach (IEnemyUpdateComponent updateComponent in UpdateComponents)
            {
                updateComponent.Update();
            }
        }

        private void FixedUpdate()
        {
            foreach (IEnemyUpdateComponent updateComponent in UpdateComponents)
            {
                updateComponent.FixedUpdate();
            }
        }

        private void LateUpdate()
        {
            foreach (IEnemyUpdateComponent updateComponent in UpdateComponents)
            {
                updateComponent.LateUpdate();
            }
        }

        private void OnDestroy()
        {
            // 풀링 중에는 오브젝트가 완전히 파괴되지 않으므로
            // NetworkObject가 아직 스폰 상태라면 풀링 중이라고 판단
            NetworkObject nob = GetComponent<NetworkObject>();
            if (nob && (nob.IsServerInitialized || nob.IsClientInitialized))
            {
                Log("풀링 중 - OnDestroy 스킵");
                return;
            }

            // 완전히 파괴될 때만 실행
            if (networkQuestEnemySpawner)
                networkQuestEnemySpawner.QuestSpawnerStop -= OffQuestMeta;

            TryGetComponent(out NetworkTransform tf);
            tf?.OnStopNetwork();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// 초기 컴포넌트 참조 설정
        /// </summary>
        private void Init()
        {
            if (!config)
            {
                LogError("Config 파일이 없습니다", this);
                return;
            }

            TryGetComponent(out stateMachine);
            TryGetComponent(out status);
            TryGetComponent(out networkSync);
            TryGetComponent(out navagent);
        }

        /// <summary>
        /// 데이터 로딩 대기 후 초기화
        /// </summary>
        private void WaitData()
        {
            ComponentInit();
            EventSub();
            StateInit();
            Status.OnDataRefreshed -= WaitData;
        }

        /// <summary>
        /// 이벤트 구독
        /// </summary>
        private void EventSub()
        {
            stateMachine.StateChangeCallback += StateChage;
        }

        #endregion

        #region Component Management

        /// <summary>
        /// 서버용 컴포넌트 초기화
        /// </summary>
        private void ComponentInit()
        {
            ComponentAdd(new EnemyMovement());
            ComponentAdd(new EnemyCombat());
            ComponentAdd(new EnemyPercetion());
            ComponentAdd(new EnemySkeletonAnimation());
            ComponentActivate(new EnemyPercetion());
            ComponentActivate(new EnemyCombat());
            ComponentActivate(new EnemyMovement());
            ComponentActivate(new EnemySkeletonAnimation());
        }

        /// <summary>
        /// 클라이언트용 컴포넌트 초기화
        /// </summary>
        private void ClientComponentInit()
        {
            ComponentAdd(new EnemyCombat());
            ComponentAdd(new EnemySkeletonAnimation());
            ComponentDisactivate(new EnemyCombat());
            ComponentActivate(new EnemySkeletonAnimation());
        }

        /// <summary>
        /// 컴포넌트 추가
        /// </summary>
        private void ComponentAdd(IEnemyComponent com)
        {
            AllComponents.Add(com.GetType(), com);
            com.Init(this);
        }

        /// <summary>
        /// 컴포넌트 활성화
        /// </summary>
        public void ComponentActivate<T>(T component) where T : IEnemyComponent
        {
            if (AllComponents.TryGetValue(component.GetType(), out var com))
            {
                EnemyComponents.Add(component.GetType(), com);
                if (com is IEnemyUpdateComponent update_com)
                    UpdateComponents.Add(update_com);
            }
        }

        /// <summary>
        /// 컴포넌트 비활성화
        /// </summary>
        public void ComponentDisactivate<T>(T component) where T : IEnemyComponent
        {
            if (EnemyComponents.TryGetValue(typeof(T), out var com))
            {
                EnemyComponents.Remove(typeof(T));
                if (component is IEnemyUpdateComponent update_com)
                    UpdateComponents.Remove(update_com);
                component.OnDisable();
            }
        }

        /// <summary>
        /// 활성 컴포넌트 가져오기
        /// </summary>
        public IEnemyComponent GetEnemyActiveComponent(Type type)
        {
            if (EnemyComponents.ContainsKey(type))
                return EnemyComponents[type];
            return null;
        }

        /// <summary>
        /// 전체 컴포넌트 가져오기
        /// </summary>
        public IEnemyComponent GetEnemyAllComponent(Type type)
        {
            if (AllComponents.ContainsKey(type))
                return AllComponents[type];
            return null;
        }

        #endregion

        #region State Management

        /// <summary>
        /// 상태 머신 초기화
        /// </summary>
        private void StateInit()
        {
            StateMachine.Init();
        }

        /// <summary>
        /// 상태 변경 시 컴포넌트로 콜백
        /// </summary>
        public void StateChage(IEnemyState oldState, IEnemyState newState)
        {
            foreach (KeyValuePair<Type, IEnemyComponent> component in EnemyComponents)
            {
                component.Value.ChangedState(oldState, newState);
            }
        }

        #endregion

        #region Public Methods - Configuration

        /// <summary>
        /// 일반 적 스포너 설정
        /// </summary>
        public void SetNetworkSpawnerObject(NetworkEnemySpawner enemySpawner)
        {
            networkQuestEnemySpawner = null;
            networkEnemySpawner = enemySpawner;
        }

        /// <summary>
        /// 퀘스트 적 스포너 설정
        /// </summary>
        public void SetNetowrkQuestSpawnerObject(NetworkQuestEnemySpawner spawner)
        {
            networkEnemySpawner = null;
            networkQuestEnemySpawner = spawner;
            if (networkQuestEnemySpawner)
                networkQuestEnemySpawner.QuestSpawnerStop += OffQuestMeta;
        }

        /// <summary>
        /// 타겟 설정
        /// </summary>
        public void SetTarget(GameObject target)
        {
            currentTarget = target;
            EnemyMovement movement = (EnemyMovement)GetEnemyAllComponent(typeof(EnemyMovement));

            if (currentTarget)
                movement.Resume();
            else
                movement.Stop();
        }

        /// <summary>
        /// 퀘스트 메타 데이터 설정
        /// </summary>
        public void SetQuestMeta(bool isQuest, int Id, MyFolder._1._Scripts._6._GlobalQuest.GlobalQuestType type, Transform defenceTarget)
        {
            isQuestEnemy = isQuest;
            questId = Id;
            questType = type;
            defencePriorityTarget = defenceTarget;
        }

        #endregion

        #region Public Methods - Death

        /// <summary>
        /// 적 사망 처리
        /// - 스포너 알림
        /// - 풀로 반환 또는 파괴 (Despawn Type에 따라)
        /// </summary>
        public void OnDeath()
        {
            NetworkObject nobj = GetComponent<NetworkObject>();
            if (nobj)
            {
                // 스포너에 알림
                if (networkEnemySpawner)
                {
                    networkEnemySpawner.OnEnemyDestroyed(transform.position);
                }
                if (networkQuestEnemySpawner)
                {
                    networkQuestEnemySpawner.OnChildEnemyDestroyed();
                }
                nobj.Despawn();
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 퀘스트 메타 데이터 비활성화
        /// </summary>
        private void OffQuestMeta()
        {
            isQuestEnemy = false;
        }

        #endregion

        #region FishNet Pooling

        /// <summary>
        /// 풀 재사용 시 데이터 로드 완료 콜백
        /// NetworkSync와 UI를 데이터 기반으로 초기화
        /// </summary>
        private void OnPoolReuseDataRefreshed()
        {
            Log("풀 재사용 - 데이터 로드 완료, NetworkSync 및 UI 초기화 시작");

            // 1. 이벤트 구독 해제 (한 번만 실행)
            if (status)
                status.OnDataRefreshed -= OnPoolReuseDataRefreshed;

            // 2. NetworkSync 초기화 (데이터 로드 완료 후)
            if (networkSync)
            {
                networkSync.ResetSync(); // ✅ 6단계: 데이터 완료 후 초기화
            }

            // 3. 컴포넌트들 데이터 재초기화 (이미 생성되어 있으므로 Init은 스킵)
            // StatInit은 이미 ResetForReuse에서 호출됨

            Log("풀 재사용 - 모든 초기화 완료");
        }

        /// <summary>
        /// 풀에서 재사용 시 초기화
        /// OnStartServer에서 자동 호출 (두 번째 스폰부터)
        /// </summary>
        private void ResetForReuse()
        {
            Log("풀에서 재사용 - 초기화 시작");

            // 1. 이벤트 정리 (중복 구독 방지)
            CleanupEvents();
            
             // ✅ NetworkTransform 재활성화
            if (TryGetComponent<FishNet.Component.Transforming.NetworkTransform>(out var networkTransform))
            {
                networkTransform.enabled = true;
            }
            
            // 7. 이벤트 재구독 (데이터 로드 완료 대기용)
            EventSub();
            if (status)
            {
                // 데이터 로드 완료 시 NetworkSync와 UI 초기화
                status.OnDataRefreshed += OnPoolReuseDataRefreshed;
            }

            // 8. NetworkSync는 데이터 로드 완료 후 초기화 (OnPoolReuseDataRefreshed에서 처리)
            status.ForceReloadData();
            
            
            // 6. Status 재초기화
            if (status)
            {
                status.ResetStatus(); // ✅ 5단계: 구현 완료
            }
            
            networkSync.ResetSync();
            
            Log("풀에서 재사용 - 기본 초기화 완료 (데이터 로드 대기 중)");
        }

        /// <summary>
        /// 풀로 반환 시 정리
        /// OnStopServer에서 자동 호출
        /// </summary>
        private void CleanupForPool()
        {
            Log("풀로 반환 - 정리 시작");

            OffQuestMeta();
            questId = -1;
            questType = default;
            defencePriorityTarget = null;
            
            // 1. 이벤트 구독 해제
            CleanupEvents();

            
            // 4. 타겟 제거
            currentTarget = null;
            
            networkSync.DieResetSync();

            
            // ✅ 3. NetworkTransform 비활성화 (씬 전환 시 NullReference 방지)
            if (TryGetComponent<FishNet.Component.Transforming.NetworkTransform>(out var networkTransform))
            {
                networkTransform.enabled = false;
            }

            Log("풀로 반환 - 정리 완료");
        }

        /// <summary>
        /// 모든 이벤트 구독 해제 (메모리 누수 방지)
        /// </summary>
        private void CleanupEvents()
        {
            // Status 이벤트
            if (status)
            {
                try { status.OnDataRefreshed -= WaitData; }
                catch { }
                try { status.OnDataRefreshed -= OnPoolReuseDataRefreshed; }
                catch { }
            }

            // StateMachine 이벤트
            if (stateMachine)
            {
                try { stateMachine.StateChangeCallback -= StateChage; }
                catch { }
            }

            // QuestSpawner 이벤트
            if (networkQuestEnemySpawner)
            {
                try { networkQuestEnemySpawner.QuestSpawnerStop -= OffQuestMeta; }
                catch { }
            }
        }

        /// <summary>
        /// 클라이언트에서 풀 재사용 시 초기화
        /// OnStartClient에서 호출 (두 번째 스폰부터)
        /// </summary>
        private void ResetForClientReuse()
        {
            Log("클라이언트 풀에서 재사용 - 초기화 시작");

            // 1. 타겟 초기화
            currentTarget = null;

            // 2. 컴포넌트 재활성화 (클라이언트는 최소한의 컴포넌트만)
            foreach (var component in AllComponents.Values)
            {
                component.OnEnable();
            }

            // 3. 상태 머신 및 NavMesh는 비활성화 유지
            if (stateMachine)
                stateMachine.enabled = false;

            if (navagent)
                navagent.enabled = false;

            Log("클라이언트 풀에서 재사용 - 초기화 완료");
        }

        /// <summary>
        /// 클라이언트에서 풀로 반환 시 정리
        /// OnStopClient에서 호출
        /// </summary>
        private void CleanupForClientPool()
        {
            Log("클라이언트 풀로 반환 - 정리 시작");

            // 1. 컴포넌트 비활성화
            foreach (var component in AllComponents.Values)
            {
                component.OnDisable();
            }

            // 2. 타겟 제거
            currentTarget = null;

            Log("클라이언트 풀로 반환 - 정리 완료");
        }
        
        #endregion

        #region Debug & Logging

        private void Log(string message, Object context = null)
        {
            if (debugLog)
                LogManager.Log(LogCategory.Enemy, message, context);
        }

        private void LogError(string message, Object context = null)
        {
            if (debugLog)
                LogManager.LogError(LogCategory.Enemy, message, context);
        }

        #endregion

        #region Editor Gizmos

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!debugGizmos) return;
            DrawNavMeshDestination();
        }

        /// <summary>
        /// NavMeshAgent의 목적지와 경로를 기즈모로 표시
        /// </summary>
        private void DrawNavMeshDestination()
        {
            if (!navagent) return;

            if (navagent.hasPath)
            {
                Vector3 destination = navagent.destination;

                // 목적지 구체 (빨간색)
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(destination, 0.8f);
                Gizmos.DrawSphere(destination, 0.3f);

                // 현재 위치에서 목적지까지 직선 (주황색)
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, destination);

                // NavMeshAgent 경로 표시 (파란선)
                Gizmos.color = Color.magenta;
                Vector3[] npathCorners = navagent.path.corners;
                for (int i = 0; i < npathCorners.Length - 1; i++)
                {
                    Gizmos.DrawLine(npathCorners[i], npathCorners[i + 1]);
                    Gizmos.DrawWireSphere(npathCorners[i], 0.2f);
                }

                // 거리 및 상태 정보 표시
                float distance = Vector3.Distance(transform.position, destination);
                string info = $"NavMesh 목적지\n" +
                              $"거리: {distance:F1}m\n" +
                              $"남은거리: {navagent.remainingDistance:F1}m\n" +
                              $"속도: {navagent.velocity.magnitude:F1}m/s\n" +
                              $"정지거리: {navagent.stoppingDistance:F1}m\n" +
                              $"isStopped: {navagent.isStopped}\n" +
                              $"pathPending: {navagent.pathPending}";

                Handles.Label(destination + Vector3.up * 1.5f, info);

                // 현재 속도 벡터 표시 (녹색 화살표)
                if (navagent.velocity.magnitude > 0.1f)
                {
                    Gizmos.color = Color.green;
                    Vector3 velocityEnd = transform.position + navagent.velocity;
                    Gizmos.DrawRay(transform.position, navagent.velocity);
                    Gizmos.DrawWireSphere(velocityEnd, 0.1f);
                }

                // 정지 거리 원 표시 (회색)
                Gizmos.color = Color.gray;
                Gizmos.DrawWireSphere(destination, navagent.stoppingDistance);
            }
            else
            {
                Handles.Label(transform.position + Vector3.up * 2f, "NavMesh: 경로 없음");
            }
        }

        private void OnValidate()
        {
            if (!debugGizmos) return;
        }
#endif

        #endregion
    }
}
