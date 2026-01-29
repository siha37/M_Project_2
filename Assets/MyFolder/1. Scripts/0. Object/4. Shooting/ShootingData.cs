using System;
using System.Collections.Generic;
using MyFolder._1._Scripts._0._Object._5._ModifiableStat;
using Newtonsoft.Json;
using UnityEngine;

namespace MyFolder._1._Scripts._0._Object._4._Shooting
{
    [Serializable]
    public class ShootingData
    {
        // 스탯 값
        [SerializeField] private ushort TypeId;
        [SerializeField] private float ShotDelay;
        [SerializeField] private int BurstCount;
        [SerializeField] private float ReloadTime;
        [SerializeField] private int MagazineCapacity;
        [SerializeField] private bool FullAuto;
        [SerializeField] private float ShotAngle;
        [SerializeField] private float BulletSpeed;
        [SerializeField] private float BulletSize;
        [SerializeField] private float LifeCycle;
        [SerializeField] private float BulletDamage;
        [SerializeField] private int PiercingCount;
        
        //증가분
        [SerializeField] private ModifiableNegativeStat<float> _shotDelayState;
        [SerializeField] private ModifiableStat<int> _burstCountState;
        [SerializeField] private ModifiableNegativeStat<float> _reloadTimeState;
        [SerializeField] private ModifiableStat<int> _magazineCapacityState;
        [SerializeField] private ModifiableStat<float> _shotAngleState;
        [SerializeField] private ModifiableStat<float> _bulletSpeedState;
        [SerializeField] private ModifiableStat<float> _bulletSizeState;
        [SerializeField] private ModifiableStat<float> _liteCycleState;
        [SerializeField] private ModifiableStat<float> _bulletDamageState;
        [SerializeField] private ModifiableStat<int> _piercingCountState;
        
        //생성자
        public ShootingData()
        {
            MagazineCapacity = 10;
            ShotDelay = 0.1f;
            ReloadTime = 2f;
            BulletSpeed = 15f;
            BulletDamage = 10f;
            LifeCycle = 5f;
        }

        [JsonConstructor]
        public ShootingData(
            [JsonProperty("TypeId")] ushort _typeId,
            [JsonProperty("ShotDelay")]float _shotDelay,
            [JsonProperty("BurstCount")]int _burstCount,
            [JsonProperty("ReloadTime")]float _reloadTime,
            [JsonProperty("MagazineCapacity")]int _magazineCapacity,
            [JsonProperty("FullAuto")]bool _fullAuto,
            [JsonProperty("ShotAngle")]float _shotAngle,
            [JsonProperty("BulletSpeed")]float _bulletSpeed,
            [JsonProperty("BulletSize")]float _bulletSize,
            [JsonProperty("LifeCycle")]float _lifeCycle,
            [JsonProperty("BulletDamage")]float _bulletDamage,
            [JsonProperty("PiercingCount")]int _piercingCount)
        {
            TypeId = _typeId;
            ShotDelay = _shotDelay;
            BurstCount = _burstCount;
            ReloadTime = _reloadTime;
            MagazineCapacity = _magazineCapacity;
            FullAuto = _fullAuto;
            ShotAngle = _shotAngle;
            BulletSpeed = _bulletSpeed;
            BulletSize = _bulletSize;
            LifeCycle = _lifeCycle;
            BulletDamage = _bulletDamage;
            PiercingCount = _piercingCount;
            
            _shotDelayState = new ModifiableNegativeStat<float>(_shotDelay);
            _burstCountState = new ModifiableStat<int>(_burstCount);
            _reloadTimeState = new ModifiableNegativeStat<float>(_reloadTime);
            _magazineCapacityState = new ModifiableStat<int>(_magazineCapacity);
            _shotAngleState = new ModifiableStat<float>(_shotAngle);
            _bulletSpeedState = new ModifiableStat<float>(_bulletSpeed);
            _bulletSizeState = new ModifiableStat<float>(_bulletSize);
            _liteCycleState = new ModifiableStat<float>(_lifeCycle);
            _bulletDamageState = new ModifiableStat<float>(_bulletDamage);
            _piercingCountState = new ModifiableStat<int>(_piercingCount);
        }
        
        // 프로퍼티들 - 수정된 값 반환
        public ushort typeId => TypeId;
        public float shotDelay => _shotDelayState?.CurrentValue ?? ShotDelay;
        public float BaseShotDelay => ShotDelay;
        
        public int burstCount => _burstCountState?.CurrentValue ?? BurstCount;
        public int BaseBurstCount => BurstCount;
        public float reloadTime => _reloadTimeState?.CurrentValue ?? ReloadTime;
        public float BaseReloadTime => ReloadTime;
        public int magazineCapacity => _magazineCapacityState?.CurrentValue ?? MagazineCapacity;
        public int BaseMagazineCapacity => MagazineCapacity;
        
        public bool fullAuto => FullAuto;
        
        public float shotAngle => _shotAngleState?.CurrentValue ?? ShotAngle;
        public float BaseShotAngle => ShotAngle;
        public float bulletSpeed => _bulletSpeedState?.CurrentValue ?? BulletSpeed;
        public float BaseBulletSpeed => BulletSpeed;
        
        public float bulletSize => _bulletSizeState?.CurrentValue ?? BulletSize;
        public float BaseBulletSize => BulletSize;
        public float lifeCycle => _liteCycleState?.CurrentValue ?? LifeCycle;
        public float BaseBlifeCycle => LifeCycle;
        public float bulletDamage => _bulletDamageState?.CurrentValue ?? BulletDamage;
        public float BaseBbulletDamage => BulletDamage;
        
        public int piercingCount => _piercingCountState?.CurrentValue ?? PiercingCount;
        public int BasePiercingCount => PiercingCount;
        
        // ShotDelay 수정자 관리
        public void AddShotDelayModifier(StatModifier<float> modifier) => _shotDelayState?.AddModifier(modifier);
        public void RemoveShotDelayModifier(string id) => _shotDelayState?.RemoveModifier(id);
        public void ClearShotDelayModifiers() => _shotDelayState?.ClearModifiers();
        public List<StatModifier<float>> GetShotDelayModifiers() => _shotDelayState?.GetModifiers() ?? new List<StatModifier<float>>();
        
        // BurstCount 수정자 관리
        public void AddBurstCountModifier(StatModifier<int> modifier) => _burstCountState?.AddModifier(modifier);
        public void RemoveBurstCountModifier(string id) => _burstCountState?.RemoveModifier(id);
        public void ClearBurstCountModifiers() => _burstCountState?.ClearModifiers();
        public List<StatModifier<int>> GetBurstCountModifiers() => _burstCountState?.GetModifiers() ?? new List<StatModifier<int>>();
        
        // ReloadTime 수정자 관리
        public void AddReloadTimeModifier(StatModifier<float> modifier) => _reloadTimeState?.AddModifier(modifier);
        public void RemoveReloadTimeModifier(string id) => _reloadTimeState?.RemoveModifier(id);
        public void ClearReloadTimeModifiers() => _reloadTimeState?.ClearModifiers();
        public List<StatModifier<float>> GetReloadTimeModifiers() => _reloadTimeState?.GetModifiers() ?? new List<StatModifier<float>>();
        
        // MagazineCapacity 수정자 관리
        public void AddMagazineCapacityModifier(StatModifier<int> modifier) => _magazineCapacityState?.AddModifier(modifier);
        public void RemoveMagazineCapacityModifier(string id) => _magazineCapacityState?.RemoveModifier(id);
        public void ClearMagazineCapacityModifiers() => _magazineCapacityState?.ClearModifiers();
        public List<StatModifier<int>> GetMagazineCapacityModifiers() => _magazineCapacityState?.GetModifiers() ?? new List<StatModifier<int>>();
        
        // ShotAngle 수정자 관리
        public void AddShotAngleModifier(StatModifier<float> modifier) => _shotAngleState?.AddModifier(modifier);
        public void RemoveShotAngleModifier(string id) => _shotAngleState?.RemoveModifier(id);
        public void ClearShotAngleModifiers() => _shotAngleState?.ClearModifiers();
        public List<StatModifier<float>> GetShotAngleModifiers() => _shotAngleState?.GetModifiers() ?? new List<StatModifier<float>>();
        
        // BulletSpeed 수정자 관리
        public void AddBulletSpeedModifier(StatModifier<float> modifier) => _bulletSpeedState?.AddModifier(modifier);
        public void RemoveBulletSpeedModifier(string id) => _bulletSpeedState?.RemoveModifier(id);
        public void ClearBulletSpeedModifiers() => _bulletSpeedState?.ClearModifiers();
        public List<StatModifier<float>> GetBulletSpeedModifiers() => _bulletSpeedState?.GetModifiers() ?? new List<StatModifier<float>>();
        
        // BulletSize 수정자 관리
        public void AddBulletSizeModifier(StatModifier<float> modifier) => _bulletSizeState?.AddModifier(modifier);
        public void RemoveBulletSizeModifier(string id) => _bulletSizeState?.RemoveModifier(id);
        public void ClearBulletSizeModifiers() => _bulletSizeState?.ClearModifiers();
        public List<StatModifier<float>> GetBulletSizeModifiers() => _bulletSizeState?.GetModifiers() ?? new List<StatModifier<float>>();
        
        // LifeCycle 수정자 관리
        public void AddLifeCycleModifier(StatModifier<float> modifier) => _liteCycleState?.AddModifier(modifier);
        public void RemoveLifeCycleModifier(string id) => _liteCycleState?.RemoveModifier(id);
        public void ClearLifeCycleModifiers() => _liteCycleState?.ClearModifiers();
        public List<StatModifier<float>> GetLifeCycleModifiers() => _liteCycleState?.GetModifiers() ?? new List<StatModifier<float>>();
        
        // BulletDamage 수정자 관리
        public void AddBulletDamageModifier(StatModifier<float> modifier) => _bulletDamageState?.AddModifier(modifier);
        public void RemoveBulletDamageModifier(string id) => _bulletDamageState?.RemoveModifier(id);
        public void ClearBulletDamageModifiers() => _bulletDamageState?.ClearModifiers();
        public List<StatModifier<float>> GetBulletDamageModifiers() => _bulletDamageState?.GetModifiers() ?? new List<StatModifier<float>>();
        
        // PiercingCount 수정자 관리
        public void AddPiercingCountModifier(StatModifier<int> modifier) => _piercingCountState?.AddModifier(modifier);
        public void RemovePiercingCountModifier(string id) => _piercingCountState?.RemoveModifier(id);
        public void ClearPiercingCountModifiers() => _piercingCountState?.ClearModifiers();
        public List<StatModifier<int>> GetPiercingCountModifiers() => _piercingCountState?.GetModifiers() ?? new List<StatModifier<int>>();
        
        //Full Auto 수정
        public void SetFullAuto(bool ison) => FullAuto = ison;
    }
}