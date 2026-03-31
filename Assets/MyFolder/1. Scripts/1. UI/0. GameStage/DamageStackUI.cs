using MyFolder._1._Scripts._7._PlayerRole;
using UnityEngine;
using UnityEngine.UI;

namespace MyFolder._1._Scripts._1._UI._0._GameStage
{
    public class DamageStackUI : MonoBehaviour
    {
        [SerializeField] private Image damageStackUI;
        [SerializeField] private GameObject backGroundUI;
        
        private void Start()
        {
            DamageStackManager.Instance.Damage_OnChanged_Callback += StackVaule_OnChanged;
            DamageStackManager.Instance.access_OnChanged += AccessAble_OnChanged;
        }

        private void AccessAble_OnChanged(bool accessAble)
        {
            backGroundUI.SetActive(accessAble);
        }

        private void StackVaule_OnChanged(float max, float value)
        {
            if(max == 0 || value == 0)
                damageStackUI.fillAmount = 0;
            else
                damageStackUI.fillAmount = value/max;
        }
    }
}