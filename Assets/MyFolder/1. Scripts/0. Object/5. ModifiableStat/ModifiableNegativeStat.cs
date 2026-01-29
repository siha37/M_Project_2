using System;
using UnityEngine;

namespace MyFolder._1._Scripts._0._Object._5._ModifiableStat
{
    public class ModifiableNegativeStat<T> : ModifiableStat<T> where T : struct, IComparable<T>, IConvertible
    {
        public ModifiableNegativeStat(T baseValue) : base(baseValue) { }
        
        protected override void CalculateFloatBasedValue()
        {
            double baseValueDouble = Convert.ToDouble(_baseValue);
            float baseValuefloat = (float)baseValueDouble;
            float totalPercent = 0.0f;

            foreach (var modifier in _modifiers)
            {
                totalPercent += modifier.percentBonus;
            }

            if (totalPercent == 0)
            {
                _currentvalue = ConvertAndClamp(baseValueDouble);
                return;
            }
            // 원본값 * (1 + 총 퍼센트) + 총 고정값
            double result = Mathf.Max(0.1f,baseValuefloat - (baseValuefloat * (totalPercent / 100)));
            result = Math.Round(result, 2);
            _currentvalue = ConvertAndClamp(result);
#if UNITY_EDITOR
            Debug.Log($"변경값:{_currentvalue}");
#endif
        }
    }
}