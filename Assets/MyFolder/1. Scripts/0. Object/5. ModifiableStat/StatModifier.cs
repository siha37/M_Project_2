using System;

namespace MyFolder._1._Scripts._0._Object._5._ModifiableStat
{
    [Serializable]
    public class StatModifier<T> where T : struct, IComparable<T>, IConvertible
    {
        public string id;
        public float percentBonus;  // 퍼센트는 항상 float
        public string description;

        public StatModifier(string id, float percentBonus, string description = "")
        {
            this.id = id;
            this.percentBonus = percentBonus;
            this.description = description;
        }
    }
}