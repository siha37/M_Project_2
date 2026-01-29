using System;
using MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.Main;
using MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.Main.Components;
using MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.States;
using MyFolder._1._Scripts._0._Object._4._Shooting;
using MyFolder._1._Scripts._3._SingleTone;
using MyFolder._1._Scripts._8999._Utility.Corutin;
using UnityEngine;

namespace MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.Status
{
    public class EnemyStatus : AgentStatus
    {
        [SerializeField] private EnemyControll enemyControll;

        private EnemyPercetion percetion;
        // 타입 안전한 프로퍼티
        public EnemyData EnemyData => data as EnemyData;
        [SerializeField] protected EnemyData eedata; // 임시
        [SerializeField] protected ShootingData ssdata; // 임시
        
        // 데이터 새로고침 이벤트
        public event Action OnDataRefreshed;
        protected override void InitializeData()
        {
            if (CanLoadData())
            {
                LoadEnemyData();
            }
            else
            {
                // 기본값으로 초기화
                data = CreateDefaultEnemyData();
                LoadDefaultShootingData();
                
                // 나중에 데이터 로딩 시도
                RegisterDataLoadCallbacks();
            }
        }

        protected override AgentData CreateDefaultAgentData()
        {
            return CreateDefaultEnemyData();
        }
        
        private EnemyData CreateDefaultEnemyData()
        {
            return new EnemyData(); // 기본 생성자 사용
        }
        
        private void LoadDefaultShootingData()
        {
            // 기본값으로 초기화된 ShootingData 인스턴스 생성
            shooting_data = new ShootingData();
        }


        private void LoadEnemyData()
        {
            if (_dataLoaded || _isLoadingData) return;
            
            _isLoadingData = true;
            
            try
            {
                // Enemy는 기본적으로 ID 1을 사용하거나, 컴포넌트에서 설정된 ID 사용
                ushort enemyId = GetDataId();
                EnemyData enemyData = GameDataManager.Instance.GetEnemyDataById(enemyId);
                
                if (enemyData != null)
                {
                    data = enemyData;
                    eedata = enemyData;
                        
                    // ShootingData 로드
                    var shootingData = GameDataManager.Instance.GetShootingDataById(enemyData.shootingDataId);
                    if (shootingData != null)
                    {
                        shooting_data = shootingData;
                        ssdata = shootingData;
                    }
                    
                    _dataLoaded = true;
                    
                    // 데이터 로딩 완료 이벤트 발생
                    OnDataRefreshed?.Invoke();
                    
                    LogManager.Log(LogCategory.Enemy, $"{gameObject.name} 적 데이터 로딩 완료 (ID: {enemyId})", this);
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError(LogCategory.Enemy, $"{gameObject.name} 적 데이터 로딩 실패: {ex.Message}", this);
            }
            finally
            {
                _isLoadingData = false;
            }
        }

        private void RegisterDataLoadCallbacks()
        {
            if (GameDataManager.Instance)
            {
                StartCoroutine(CheckDataPeriodically());
            }
        }

        public void AttakerTargeting(GameObject target)
        {
            if (!target)
                return;
            if(percetion==null) percetion = enemyControll.GetEnemyActiveComponent(typeof(EnemyPercetion)) as EnemyPercetion;
            percetion?.SetTarget(target);
        }
        private System.Collections.IEnumerator CheckDataPeriodically()
        {
            while (!_dataLoaded && !_isLoadingData)
            {
                yield return WaitForSecondsCache.Get(0.5f);
                
                if (CanLoadData())
                {
                    LoadEnemyData();
                    break;
                }
            }
        }

        protected override void DataLoad()
        {
            // 호환성을 위해 유지하지만 더 이상 사용하지 않음
        }
        
        public void OnDeathSequence()
        {
            OnRealDeath();
        }

        public override void OnRealDeath()
        {
            enemyControll.OnDeath();
        }

        /// <summary>
        /// 풀링을 위한 상태 리셋
        /// - HP 복구
        /// - 총알 복구
        /// - 상태 초기화
        /// - 이벤트 정리
        /// - 데이터 재로드 준비
        /// </summary>
        public void ResetStatus()
        {
            // 2. HP 복구 (현재 데이터 기준, 나중에 새 데이터 로드 시 다시 설정됨)
            if (data != null)
            {
                currentHp = data.hp;
            }

            // 3. 총알 복구 (현재 데이터 기준)
            if (shooting_data != null)
            {
                bulletCurrentCount = shooting_data.magazineCapacity;
            }

            // 4. 상태 초기화
            isDead = false;

            // 5. 이벤트 정리 (중복 구독 방지)
            if (OnDataRefreshed != null)
            {
                // 모든 구독 해제
                foreach (var d in OnDataRefreshed.GetInvocationList())
                {
                    OnDataRefreshed -= (Action)d;
                }
            }

            LogManager.Log(LogCategory.Enemy, $"{gameObject.name} Status 리셋 완료 (HP: {currentHp}/{data?.hp}, Bullet: {bulletCurrentCount}/{shooting_data?.magazineCapacity})", this);
        }

        /// <summary>
        /// 데이터를 강제로 재로드 (타입 변경 시 사용)
        /// SetDataId() 호출 후 반드시 이 메서드 호출 필요
        /// </summary>
        public void ForceReloadData()
        {
            // 데이터 로드 플래그 초기화
            _dataLoaded = false;
            _isLoadingData = false;

            // 데이터 로드 시도
            if (CanLoadData())
            {
                LoadEnemyData();
            }
            else
            {
                // GameDataManager 준비 안 됨 - 기본 데이터로 초기화
                data = CreateDefaultEnemyData();
                LoadDefaultShootingData();
                RegisterDataLoadCallbacks();
            }
        }
    }
}