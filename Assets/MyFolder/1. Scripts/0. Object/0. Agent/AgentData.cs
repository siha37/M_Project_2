using System;
using System.Collections.Generic;
using MyFolder._1._Scripts._0._Object._5._ModifiableStat;
using Newtonsoft.Json;
using UnityEngine;

namespace MyFolder._1._Scripts._0._Object._0._Agent
{
    [Serializable]
    public class AgentData : ObjectData
    {
        // 스탯 값
        [SerializeField] protected float Speed = 5f;
        [SerializeField] protected float AttackSpeed = 5f;
        [SerializeField] protected ushort ShootingDataID;

        // 추가분
        [SerializeField] protected ModifiableStat<float> _speedStat;
        [SerializeField] protected ModifiableStat<float> _attackSpeedStat;
        
        //생성자
        public AgentData(){}

        
        [JsonConstructor]
        public AgentData([JsonProperty("TypeId")] ushort typeId,
            [JsonProperty("Hp")] float _hp,
            [JsonProperty("Speed")] float _speed,
            [JsonProperty("AttackSpeed")] float _attackSpeed,
            [JsonProperty("ShootingDataID")] ushort _shootingDataId) : base(typeId,_hp)
        {
            Speed = _speed;
            AttackSpeed = _attackSpeed;
            ShootingDataID = _shootingDataId;
            _speedStat = new ModifiableStat<float>(_speed);
            _attackSpeedStat = new ModifiableStat<float>(_attackSpeed);
            IsData = true;
        }
        
        // SPEED 수정자 관리
        public void AddSpeedModifier(StatModifier<float> modifier) => _speedStat?.AddModifier(modifier);
        public void RemoveSpeedModifier(string id) => _speedStat?.RemoveModifier(id);
        public void ClearSpeedModifiers() => _speedStat?.ClearModifiers();
        public List<StatModifier<float>> GetSpeedModifiers() => _speedStat?.GetModifiers() ?? new List<StatModifier<float>>();
        
        //ATTACLSPEED 수정자 관리
        public void AddAttackSpeedModifier(StatModifier<float> modifier) => _attackSpeedStat?.AddModifier(modifier);
        public void RemoveAttackSpeedModifier(string id) => _attackSpeedStat?.RemoveModifier(id);
        public void ClearAttackSpeedModifiers() => _attackSpeedStat?.ClearModifiers();
        public List<StatModifier<float>> GetAttackSpeedModifiers() => _attackSpeedStat?.GetModifiers() ?? new List<StatModifier<float>>();
        
        //Gettor
        public float speed => _speedStat?.CurrentValue ?? 100f;
        public float attackSpeed => _attackSpeedStat?.CurrentValue ?? 100f;
        public ushort shootingDataId => ShootingDataID;
    }
}