using System;
using System.Collections.Generic;
using MyFolder._1._Scripts._0._Object._5._ModifiableStat;
using Newtonsoft.Json;
using UnityEngine;

namespace MyFolder._1._Scripts._0._Object
{
    [Serializable]
    public class ObjectData
    {
        // 스탯 값
        [SerializeField] protected ushort TypeId;
        [SerializeField] protected float Hp = 100f;
        
        // 추가분
        [SerializeField] protected ModifiableStat<float> _hpStat;
        

        public bool IsData = false;
        //생성자
        public ObjectData(){}
        [JsonConstructor]
        public ObjectData([JsonProperty("TypeId")] ushort typeId,[JsonProperty("Hp")] float _hp)
        {
            TypeId = typeId;
            Hp = _hp;
            _hpStat = new ModifiableStat<float>(Hp);
            IsData = true;
        }
        
        // HP 수정자 관리
        public void AddHpModifier(StatModifier<float> modifier) => _hpStat?.AddModifier(modifier);
        public void RemoveHpModifier(string id) => _hpStat?.RemoveModifier(id);
        public void ClearHpModifiers() => _hpStat?.ClearModifiers();
        public List<StatModifier<float>> GetHpModifiers() => _hpStat?.GetModifiers() ?? new List<StatModifier<float>>();
        
        //Gettor
        public ushort typeId => TypeId;
        
        public float hp => _hpStat?.CurrentValue ?? 100f;
    }
}