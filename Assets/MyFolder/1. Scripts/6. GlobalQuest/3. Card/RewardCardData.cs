using System;
using Newtonsoft.Json;

namespace MyFolder._1._Scripts._6._GlobalQuest._3._Card
{
    [Serializable]
    public class RewardCardData
    {
        public ushort cardId;
        public string cardName;
        public string description;
        
        // 탄속 증가
        public float bulletSpeedMinPercentage;
        public float bulletSpeedMaxPercentage;
        
        // 탄 데미지 증가
        public float bulletDamageMinPercentage;
        public float bulletDamageMaxPercentage;
        
        // 이동 속도 증가
        public float speedMinPercentage;
        public float speedMaxPercentage;
        
        // 최대 체력 증가
        public float hpMinPercentage;
        public float hpMaxPercentage;
        
        // 방패 게이지 증가
        public float defenceMinPercentage;
        public float defenceMaxPercentage;
        
        // 탄 사이즈 증가
        public float bulletSizeMinPercentage;
        public float bulletSizeMaxPercentage;
        
        // 발사 간 딜레이 감소
        public float shotDelayMinPercentage;
        public float shotDelayMaxPercentage;
        
        // 장탄 수 증가
        public float magazineCapacityMinPercentage;
        public float magazineCapacityMaxPercentage;
        
        public float reloadTimeMinPercentage;
        public float reloadTimeMaxPercentage;
        
        // 기본 생성자
        public RewardCardData()
        {
        }
        
        // JSON 생성자
        [JsonConstructor]
        public RewardCardData(
            [JsonProperty("CardId")] ushort cardId,
            [JsonProperty("CardName")] string cardName,
            [JsonProperty("Description")] string description,
            [JsonProperty("BulletSpeedMinPercentage")] float bulletSpeedMinPercentage,
            [JsonProperty("BulletSpeedMaxPercentage")] float bulletSpeedMaxPercentage,
            [JsonProperty("BulletDamageMinPercentage")] float bulletDamageMinPercentage,
            [JsonProperty("BulletDamageMaxPercentage")] float bulletDamageMaxPercentage,
            [JsonProperty("SpeedMinPercentage")] float speedMinPercentage,
            [JsonProperty("SpeedMaxPercentage")] float speedMaxPercentage,
            [JsonProperty("HpMinPercentage")] float hpMinPercentage,
            [JsonProperty("HpMaxPercentage")] float hpMaxPercentage,
            [JsonProperty("DefenceMinPercentage")] float defenceMinPercentage,
            [JsonProperty("DefenceMaxPercentage")] float defenceMaxPercentage,
            [JsonProperty("BulletSizeMinPercentage")] float bulletSizeMinPercentage,
            [JsonProperty("BulletSizeMaxPercentage")] float bulletSizeMaxPercentage,
            [JsonProperty("ShotDelayMinPercentage")] float shotDelayMinPercentage,
            [JsonProperty("ShotDelayMaxPercentage")] float shotDelayMaxPercentage,
            [JsonProperty("MagazineCapacityMinPercentage")] float magazineCapacityMinPercentage,
            [JsonProperty("MagazineCapacityMaxPercentage")] float magazineCapacityMaxPercentage,
            [JsonProperty("ReloadTimeMinPercentage")] float reloadTimeMinPercentage,
            [JsonProperty("ReloadTimeMaxPercentage")] float reloadTimeMaxPercentage)
        {
            this.cardId = cardId;
            this.cardName = cardName;
            this.description = description;
            this.bulletSpeedMinPercentage = bulletSpeedMinPercentage;
            this.bulletSpeedMaxPercentage = bulletSpeedMaxPercentage;
            this.bulletDamageMinPercentage = bulletDamageMinPercentage;
            this.bulletDamageMaxPercentage = bulletDamageMaxPercentage;
            this.speedMinPercentage = speedMinPercentage;
            this.speedMaxPercentage = speedMaxPercentage;
            this.hpMinPercentage = hpMinPercentage;
            this.hpMaxPercentage = hpMaxPercentage;
            this.defenceMinPercentage = defenceMinPercentage;
            this.defenceMaxPercentage = defenceMaxPercentage;
            this.bulletSizeMinPercentage = bulletSizeMinPercentage;
            this.bulletSizeMaxPercentage = bulletSizeMaxPercentage;
            this.shotDelayMinPercentage = shotDelayMinPercentage;
            this.shotDelayMaxPercentage = shotDelayMaxPercentage;
            this.magazineCapacityMinPercentage = magazineCapacityMinPercentage;
            this.magazineCapacityMaxPercentage = magazineCapacityMaxPercentage;
            this.reloadTimeMinPercentage = reloadTimeMinPercentage;
            this.reloadTimeMaxPercentage = reloadTimeMaxPercentage;
        }
    }
}
