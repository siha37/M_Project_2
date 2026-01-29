using System;
using System.Collections.Generic;
using MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.Main;
using MyFolder._1._Scripts._3._SingleTone;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;

namespace MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.States
{
    public class EnemyStateMachine : MonoBehaviour
    {
        private EnemyControll agent;
        
        private bool StateReady = false;
        
        private Dictionary<Type,IEnemyState> States = new Dictionary<Type, IEnemyState>();
        private IEnemyState CurrentState;
        private IEnemyState PreviousState;
        
        [SerializeField] string CurrentStateName;
        [SerializeField] string PreviousStateName;
        
        public Action<IEnemyState,IEnemyState> StateChangeCallback;

        public void Init()
        {
            TryGetComponent(out agent);
            StateInit();
            StateChange(typeof(EnemyPatrolState));
            StateReady = true;
        }

        private void Update()
        {
            CurrentState?.Update();
        }

        private void StateInit()
        {    
            States = new Dictionary<Type, IEnemyState>();
    
            // ✅ 수동으로 상태 등록 (가장 안전함)
            RegisterState(new EnemyPatrolState());
            RegisterState(new EnemyAttackState());
            RegisterState(new EnemyMoveState());
        }

        private void RegisterState(IEnemyState state)
        {
            try
            {
                States[state.GetType()] = state;
                if(agent)
                    state.Init(agent);
                LogManager.Log(LogCategory.Enemy, $"상태 등록: {state.GetType().Name}");
            }
            catch (System.Exception e)
            {
                LogManager.LogError(LogCategory.Enemy, $"상태 등록 실패: {state.GetType().Name} - {e.Message}");
            }
        }
        public bool StateChange(Type stateType)
        {
            if (!StateReady || !States.TryGetValue(stateType, out var state))
            {
                LogManager.LogWarning(LogCategory.Enemy, $"상태 변경 실패: {stateType?.Name}");
                return false;
            }

            if (state == null)
            {
                LogManager.LogError(LogCategory.Enemy, $"상태가 null입니다: {stateType?.Name}");
                return false;
            }
    
            if (CurrentState != null && !CurrentState.CanStateChange(state))
            {
                LogManager.LogWarning(LogCategory.Enemy, $"상태 변경이 허용되지 않습니다: {CurrentState.GetName()} -> {state.GetName()}");
                return false;
            }

            PreviousState = CurrentState;
            PreviousStateName = PreviousState?.GetName();
            CurrentState = state;
            CurrentStateName = CurrentState.GetName();
            PreviousState?.OnStateExit();
            CurrentState.OnStateEnter();
            StateChangeCallback?.Invoke(PreviousState, CurrentState);
            return true;
        }

        public string GetStateName()
        {
            return CurrentState?.GetName();
        }

        public bool CompareState(Type type)
        {
            return CurrentState?.GetType() == type;
        }

        /// <summary>
        /// 풀링을 위한 상태 머신 리셋
        /// - 현재 상태 종료
        /// - 초기 상태(Patrol)로 전환
        /// </summary>
        public void ResetStateMachine()
        {
            LogManager.Log(LogCategory.Enemy, "상태 머신 리셋 시작");

            // 1. 현재 상태 종료
            if (CurrentState != null)
            {
                CurrentState.OnStateExit();
            }

            // 2. 상태 변수 초기화
            PreviousState = null;
            PreviousStateName = null;
            StateReady = false;

            // 3. States 딕셔너리는 유지 (재사용)
            // 각 상태 객체도 그대로 재사용

            // 4. 초기 상태(Patrol)로 전환
            StateChange(typeof(EnemyPatrolState));
            StateReady = true;

            LogManager.Log(LogCategory.Enemy, "상태 머신 리셋 완료");
        }
    }
}