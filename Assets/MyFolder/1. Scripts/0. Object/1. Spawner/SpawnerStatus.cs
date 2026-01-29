using System;
using System.Collections;
using FishNet.Managing;
using FishNet.Object;
using MyFolder._1._Scripts._0._Object._0._Agent;
using MyFolder._1._Scripts._10._Sound;
using MyFolder._1._Scripts._3._SingleTone;
using MyFolder._1._Scripts._8999._Utility.Corutin;
using UnityEngine;

namespace MyFolder._1._Scripts._0._Object._1._Spawner
{
    public class SpawnerStatus : AgentStatus
    {
        private bool onInvisible = false;
        
        // 스포너는 특별한 데이터 타입이 없다면 기본 AgentData 사용
        public SpawnerData SpawnerData => data as SpawnerData;

        [SerializeField] private SpawnerSfx sfx; 

        
        
        protected override void InitializeData()
        {
        }

        // 스포너 생성자가 호출
        public void InitializeData(ushort spawnerID)
        {
            SetDataId(spawnerID);
            
            if (CanLoadData())
            {
                LoadSpawnerData();
            }
            else
            {
                // 기본값으로 초기화
                data = CreateDefaultAgentData();
                
                // 나중에 데이터 로딩 시도
                RegisterDataLoadCallbacks();
            }
            
        }

        private void LoadSpawnerData()
        {
            if (_dataLoaded || _isLoadingData) return;
            
            _isLoadingData = true;
            
            try
            {
                // 스포너 전용 데이터가 있다면 로드, 없다면 기본 AgentData 사용
                var spawnerData = GameDataManager.Instance.GetSpawnerDataById(GetDataId());
                
                if (spawnerData != null)
                {
                    data = spawnerData;
                    if (TryGetComponent(out NetworkEnemySpawner spawner)) spawner.Initialize(spawnerData);
                    _dataLoaded = true;
              
                    currentHp = data.hp;
                    LogManager.Log(LogCategory.Spawner, $"{gameObject.name} 스포너 데이터 로딩 완료", this);
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError(LogCategory.Spawner, $"{gameObject.name} 스포너 데이터 로딩 실패: {ex.Message}", this);
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

        private System.Collections.IEnumerator CheckDataPeriodically()
        {
            while (!_dataLoaded && !_isLoadingData)
            {
                yield return WaitForSecondsCache.Get(0.5f);
                
                if (CanLoadData())
                {
                    LoadSpawnerData();
                    break;
                }
            }
        }

        protected override void Start()
        {
            base.Start();
        }

        public override bool TakeDamage(float damage, Vector2 hitDirection = default)
        {
            if (isDead) return false;
            if (onInvisible) damage = 0;
            
            base.TakeDamage(damage, hitDirection);
        
            if (currentHp <= 0)
            {
                isDead = true;
                sfx.Play(SpawnerSfxType.Broken);
            }

            return false;
        }
        
        
        public void OnDeathEffectAnim()
        {
            StartCoroutine(nameof(DeathSequence));
        }
        
        
        /// <summary>
        /// 기절 상태 - 색상만 변경
        /// </summary>
        /// <returns></returns>
        protected override IEnumerator DeathSequence()
        {
            if (TryGetComponent(out NetworkEnemySpawner spawner)) { spawner.RemoveSpawner(); }
            sfx.Play(SpawnerSfxType.Broken);
            yield return base.DeathSequence();
        }

        public void OnInvisible()
        {
            onInvisible = true;
        }

        public void OffInvisible()
        {
            onInvisible = false;
        }
        
        public override void OnRealDeath()
        {
            FishNet.InstanceFinder.NetworkManager.ServerManager.Despawn(this.gameObject);
            base.OnRealDeath();
        }
    }
}
