using System;
using System.Collections.Generic;
using UnityEngine;

namespace MyFolder._1._Scripts._0._Object._5._ModifiableStat
{
    [Serializable]
    public class ModifiableStat<T> where T : struct, IComparable<T>, IConvertible
    {
        [SerializeField] protected T _baseValue;
        [SerializeField] protected List<StatModifier<T>> _modifiers = new List<StatModifier<T>>();
        [SerializeField] protected T _currentvalue;
        
        public T BaseValue => _baseValue;
        public T CurrentValue => _currentvalue;

        public ModifiableStat(T baseValue)
        {
            _baseValue = baseValue;
            _currentvalue = baseValue;
        }

        public void SetBaseValue(T value)
        {
            _baseValue = value;
        }

        public void AddModifier(StatModifier<T> modifier)
        {
            _modifiers.Add(modifier);
            CalculateValue();
        }

        public void RemoveModifier(string id)
        {
            _modifiers.RemoveAll(m => m.id == id);
            _currentvalue = _baseValue;
        }

        public void ClearModifiers()
        {
            _modifiers.Clear();
            _currentvalue = _baseValue; // 수정자 제거 후 현재값을 기본값으로 리셋
        }
        private void CalculateValue()
        {
            CalculateFloatBasedValue();
        }

        protected virtual void CalculateFloatBasedValue()
        {
            double baseValueDouble = Convert.ToDouble(_baseValue);
            double totalPercent = 0.0;

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
            double result = baseValueDouble * (totalPercent / 100) + baseValueDouble;
            result = Math.Round(result, 2);
            _currentvalue = ConvertAndClamp(result);
#if UNITY_EDITOR
            Debug.Log($"변경값:{_currentvalue}");
#endif
        }

        protected T ConvertAndClamp(double value)
        {
            if (typeof(T) == typeof(float))
            {
                float floatValue = (float)Math.Max(float.MinValue, Math.Min(float.MaxValue, value));
                return (T)(object)floatValue;
            }
            else if (typeof(T) == typeof(int))
            {
                int intValue = (int)Math.Round(Math.Max(int.MinValue, Math.Min(int.MaxValue, value)));
                return (T)(object)intValue;
            }
            else if (typeof(T) == typeof(ushort))
            {
                ushort ushortValue = (ushort)Math.Round(Math.Max(ushort.MinValue, Math.Min(ushort.MaxValue, value)));
                return (T)(object)ushortValue;
            }
            else if (typeof(T) == typeof(short))
            {
                short shortValue = (short)Math.Round(Math.Max(short.MinValue, Math.Min(short.MaxValue, value)));
                return (T)(object)shortValue;
            }
            else if (typeof(T) == typeof(byte))
            {
                byte byteValue = (byte)Math.Round(Math.Max(byte.MinValue, Math.Min(byte.MaxValue, value)));
                return (T)(object)byteValue;
            }
            else if (typeof(T) == typeof(double))
            {
                return (T)(object)value;
            }
            
            return _baseValue;
        }

        public List<StatModifier<T>> GetModifiers()
        {
            return new List<StatModifier<T>>(_modifiers);
        }
    }
}