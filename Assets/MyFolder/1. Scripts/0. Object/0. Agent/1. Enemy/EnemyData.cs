using System;
using System.Collections.Generic;
using MyFolder._1._Scripts._0._Object._5._ModifiableStat;
using Newtonsoft.Json;
using UnityEngine;

namespace MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy
{
    
    [Serializable]
    public class EnemyData : AgentData
    {   
        [Header("=== 탐지 설정 ===")]
        [Tooltip("적이 플레이어를 탐지할 수 있는 최대 거리")]
        [SerializeField] protected float DetectionRange = 10f;
    
    
        [Tooltip("시야각 (도)")]
        [SerializeField] protected float FieldOfViewAngle = 90f;
    
    
    
        
        // ✅ ModifiableStat 변수들
        [SerializeField] private ModifiableStat<float> _attackRangeStat;
        [SerializeField] private ModifiableStat<float> _stoppingDistanceStat;
        [SerializeField] private ModifiableStat<float> _aimPrecisionStat;

        //생성자
        public EnemyData(){}
        
        [JsonConstructor]
        public EnemyData([JsonProperty("TypeId")] ushort typeId,
            [JsonProperty("Hp")] float _hp,
            [JsonProperty("Speed")] float _speed,
            [JsonProperty("AttackSpeed")] float _attackSpeed,
            [JsonProperty("ShootingDataID")] ushort _shootingDataId,
            [JsonProperty("DetectionRange")]float _detectionRange,
            [JsonProperty("AttackRange")]float _attackRange,
            [JsonProperty("FieldOfViewAngle")]float _fieldOfViewAngle,
            [JsonProperty("StoppingDistance")]float _stoppingDistance,
            [JsonProperty("AimPrecision")]float _aimPrecision) : base(typeId,_hp,_speed,_attackSpeed,_shootingDataId)
        {
            DetectionRange = _detectionRange;
            FieldOfViewAngle = _fieldOfViewAngle;
            
            // ModifiableStat 초기화
            _attackRangeStat = new ModifiableStat<float>(_attackRange);
            _stoppingDistanceStat = new ModifiableStat<float>(_stoppingDistance);
            _aimPrecisionStat = new ModifiableStat<float>(_aimPrecision);
            IsData = true;
        }

        // ✅ AttackRange 수정자 관리
        public void AddAttackRangeModifier(StatModifier<float> modifier) => _attackRangeStat?.AddModifier(modifier);
        public void RemoveAttackRangeModifier(string id) => _attackRangeStat?.RemoveModifier(id);
        public void ClearAttackRangeModifiers() => _attackRangeStat?.ClearModifiers();
        public List<StatModifier<float>> GetAttackRangeModifiers() => _attackRangeStat?.GetModifiers() ?? new List<StatModifier<float>>();

        // ✅ StoppingDistance 수정자 관리
        public void AddStoppingDistanceModifier(StatModifier<float> modifier) => _stoppingDistanceStat?.AddModifier(modifier);
        public void RemoveStoppingDistanceModifier(string id) => _stoppingDistanceStat?.RemoveModifier(id);
        public void ClearStoppingDistanceModifiers() => _stoppingDistanceStat?.ClearModifiers();
        public List<StatModifier<float>> GetStoppingDistanceModifiers() => _stoppingDistanceStat?.GetModifiers() ?? new List<StatModifier<float>>();

        // ✅ AimPrecision 수정자 관리
        public void AddAimPrecisionModifier(StatModifier<float> modifier) => _aimPrecisionStat?.AddModifier(modifier);
        public void RemoveAimPrecisionModifier(string id) => _aimPrecisionStat?.RemoveModifier(id);
        public void ClearAimPrecisionModifiers() => _aimPrecisionStat?.ClearModifiers();
        public List<StatModifier<float>> GetAimPrecisionModifiers() => _aimPrecisionStat?.GetModifiers() ?? new List<StatModifier<float>>();

        // ✅ Gettor
        public float attackRange => _attackRangeStat?.CurrentValue ?? 5f;
        public float BaseAttackRange => _attackRangeStat?.BaseValue ?? 5f;
        
        public float stoppingDistance => _stoppingDistanceStat?.CurrentValue ?? 5f;
        public float BaseStoppingDistance => _stoppingDistanceStat?.BaseValue ?? 5f;
        
        public float aimPrecision => _aimPrecisionStat?.CurrentValue ?? 0.1f;
        public float BaseAimPrecision => _aimPrecisionStat?.BaseValue ?? 0.1f;
        
        // ✅ DetectionRange Gettor
        public float detectionRange => DetectionRange;
        
        
        // ✅ FieldOfViewAngle Gettor
        public float fieldOfViewAngle => FieldOfViewAngle;
        
    }
}