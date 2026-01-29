using System;
using System.Collections.Generic;
using MyFolder._1._Scripts._0._Object._5._ModifiableStat;
using Newtonsoft.Json;

namespace MyFolder._1._Scripts._0._Object._0._Agent._0._Player
{
    [Serializable]
    public class PlayerData : AgentData
    {
        // 스탯 값
        private string Name;
        private float Defence;
        private float DefenceRecoverDelay;
        private float DefenceRecoverAmountForFrame;
        private int Revival;
        private ushort SkinID;
        private float VisionRadius;
        private float CamouflageHoldingTime;
        private float CamouflageCooldown;
        private float HealAmount;

        // 추가분
        private ModifiableStat<float> _defenceStat;
        private ModifiableStat<int> _revivalStat;
        
        //생성자
        public PlayerData()
        {
        }

        [JsonConstructor]
        public PlayerData([JsonProperty("TypeId")] ushort typeId,
            [JsonProperty("Hp")] float _hp, 
            [JsonProperty("Speed")] float _speed,
            [JsonProperty("AttackSpeed")] float _attackSpeed,
            [JsonProperty("ShootingDataID")] ushort _shootingDataId, 
            [JsonProperty("Name")] string _name, 
            [JsonProperty("Defence")] float defence,
            [JsonProperty("DefenceRecoverDelay")] float defenceRecoverDelay,
            [JsonProperty("DefenceRecoverAmountForFrame")] float defenceRecoverAmountForFrame,
            [JsonProperty("Revival")] int _revival,
            [JsonProperty("SkinID")] ushort skinID,
            [JsonProperty("VisionRadius")] float VisionRadius,
            [JsonProperty("CamouflageHoldingTime")] float CamouflageHoldingTime,
            [JsonProperty("CamouflageCooldown")] float CamouflageCooldown,
            [JsonProperty("HealAmount")] float HealAmount
            ) : base(typeId,_hp, _speed,_attackSpeed, _shootingDataId)
        {
            Name = _name;
            Defence = defence;
            DefenceRecoverDelay = defenceRecoverDelay;
            DefenceRecoverAmountForFrame = defenceRecoverAmountForFrame;
            Revival = _revival;
            SkinID = skinID;
            this.VisionRadius = VisionRadius;
            this.CamouflageHoldingTime = CamouflageHoldingTime;
            this.CamouflageCooldown = CamouflageCooldown;
            this.HealAmount = HealAmount;
            _defenceStat = new ModifiableStat<float>(defence);
            _revivalStat = new ModifiableStat<int>(_revival);
        }
        
        
        // Armo 수정자 관리
        public void AddDefenceModifier(StatModifier<float> modifier) => _defenceStat?.AddModifier(modifier);
        public void RemoveDefenceModifier(string id) => _defenceStat?.RemoveModifier(id);
        public void ClearDefenceModifiers() => _defenceStat?.ClearModifiers();
        public List<StatModifier<float>> GetDefenceModifiers() => _defenceStat?.GetModifiers() ?? new List<StatModifier<float>>();
        
        
        // Revival 수정자 관리
        public void AddRevivalModifier(StatModifier<int> modifier) => _revivalStat?.AddModifier(modifier);
        public void RemoveRevivalModifier(string id) => _revivalStat?.RemoveModifier(id);
        public void ClearRevivalModifiers() => _revivalStat?.ClearModifiers();
        public List<StatModifier<int>> GetRevivalModifiers() => _revivalStat?.GetModifiers() ?? new List<StatModifier<int>>();
        
        //Gettor
        public string name => Name;
        public float defence => _defenceStat?.CurrentValue ?? 100f;
        public float BaseDefence => _defenceStat?.BaseValue ?? 100f;

        public float defenceRecoverDelay => DefenceRecoverDelay;
        public float defenceRecoverAmountForFrame => DefenceRecoverAmountForFrame;
        
        public int revival => _revivalStat?.CurrentValue ?? 100;
        public int Baserevival => _revivalStat?.BaseValue ?? 100;
        public ushort skinId => SkinID;
        public float visionRadius => VisionRadius;
        public float camouflageHoldingTime => CamouflageHoldingTime;
        public float camouflageCooldown => CamouflageCooldown;
        public float healAmount => HealAmount;
    }
}