using System;
using Newtonsoft.Json;

namespace MyFolder._1._Scripts._6._GlobalQuest._3._Card
{
    [Serializable]
    public class DefeatCardData
    {
        public ushort cardId;
        public string cardName;
        public string description;
        
        // 적군 탄속 감소 %
        public float enemyBulletSpeedMinPercentage;
        public float enemyBulletSpeedMaxPercentage;
        
        // 적군 탄 데미지 증가 %
        public float enemyBulletDamageMinPercentage;
        public float enemyBulletDamageMaxPercentage;
        
        // 적군 탄 사이즈 증가 %
        public float enemyBulletSizeMinPercentage;
        public float enemyBulletSizeMaxPercentage;
        
        // 적군 이동 속도 증가 %
        public float enemySpeedMinPercentage;
        public float enemySpeedMaxPercentage;
        
        // 적군 최대 체력 증가 %
        public float enemyHpMinPercentage;
        public float enemyHpMaxPercentage;
        
        // 기본 생성자
        public DefeatCardData()
        {
        }
        
        [JsonConstructor]
        public DefeatCardData(
            [JsonProperty("CardId")] ushort cardId,
            [JsonProperty("CardName")] string cardName,
            [JsonProperty("Description")] string description,
            [JsonProperty("BulletSpeedMinPercentage")] float enemyBulletSpeedMinPercentage,
            [JsonProperty("BulletSpeedMaxPercentage")] float enemyBulletSpeedMaxPercentage,
            [JsonProperty("BulletDamageMinPercentage")] float enemyBulletDamageMinPercentage,
            [JsonProperty("BulletDamageMaxPercentage")] float enemyBulletDamageMaxPercentage,
            [JsonProperty("BulletSizeMinPercentage")] float enemyBulletSizeMinPercentage,
            [JsonProperty("BulletSizeMaxPercentage")] float enemyBulletSizeMaxPercentage,
            [JsonProperty("SpeedMinPercentage")] float enemySpeedMinPercentage,
            [JsonProperty("SpeedMaxPercentage")] float enemySpeedMaxPercentage,
            [JsonProperty("HpMinPercentage")] float enemyHpMinPercentage,
            [JsonProperty("HpMaxPercentage")] float enemyHpMaxPercentage)
        {
            this.cardId = cardId;
            this.cardName = cardName;
            this.description = description;
            this.enemyBulletSpeedMinPercentage = enemyBulletSpeedMinPercentage;
            this.enemyBulletSpeedMaxPercentage = enemyBulletSpeedMaxPercentage;
            this.enemyBulletDamageMinPercentage = enemyBulletDamageMinPercentage;
            this.enemyBulletDamageMaxPercentage = enemyBulletDamageMaxPercentage;
            this.enemyBulletSizeMinPercentage = enemyBulletSizeMinPercentage;
            this.enemyBulletSizeMaxPercentage = enemyBulletSizeMaxPercentage;
            this.enemySpeedMinPercentage = enemySpeedMinPercentage;
            this.enemySpeedMaxPercentage = enemySpeedMaxPercentage;
            this.enemyHpMinPercentage = enemyHpMinPercentage;
            this.enemyHpMaxPercentage = enemyHpMaxPercentage;
        }
    }
}
