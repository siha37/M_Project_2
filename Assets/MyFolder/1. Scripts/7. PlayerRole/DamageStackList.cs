using System;
using Newtonsoft.Json;

namespace MyFolder._1._Scripts._7._PlayerRole
{
    [Serializable]
    public class DamageStackList
    {
        protected ushort Level;
        protected float Stack_Amount;

        [JsonConstructor]
        public DamageStackList([JsonProperty("Level")] ushort level, [JsonProperty("Stack_Amount")] float stack_amount)
        {
            Level = level;
            Stack_Amount = stack_amount;
        }
        
        public ushort level => Level;
        public float stack_amount => Stack_Amount;
    }

    [Serializable]
    public class DamageStackRatio
    {
        protected ushort Level;
        protected float IsPlayerRatio;
        protected float IsEnemyRatio;

        [JsonConstructor]
        public DamageStackRatio([JsonProperty("Level")] ushort level, [JsonProperty("IsPlayer")] float isPlayerRatio,[JsonProperty("IsEnemy")] float isEnemyRatio)
        {
            Level = level;
            IsPlayerRatio = isPlayerRatio;
            IsEnemyRatio = isEnemyRatio;
        }
        
        public ushort level => Level;
        public float isPlayerRatio => IsPlayerRatio;
        public float isEnemyRatio => IsEnemyRatio;
    }
}