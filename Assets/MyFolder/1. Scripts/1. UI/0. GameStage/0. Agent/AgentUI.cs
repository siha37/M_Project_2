using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MyFolder._1._Scripts._1._UI._0._GameStage._0._Agent
{
    public class AgentUI : MonoBehaviour
    {
        [Header("Canvas")]
        [SerializeField] protected GameObject canvas;
        [Header("체력바")]
        [SerializeField] protected Image frontHealthBar;
        [SerializeField] protected Image secondaryHealthBar;
        [SerializeField] protected float healthBarLerpSpeed = 5f;
        protected float targetHealthFill;

        [Header("재장전")]
        [SerializeField] protected Image reloadBar;

        [Header("부활")]
        [SerializeField] protected Image reviveBar;

        [Header("탄창")]
        [SerializeField] protected TextMeshProUGUI ammoText;

        protected void Start()
        {
            if (reloadBar)
                reloadBar.fillAmount = 0f;
            
            if (frontHealthBar)
                frontHealthBar.fillAmount = 1f;
            
            if (secondaryHealthBar)
                secondaryHealthBar.fillAmount = 1f;

            if (reviveBar)
                reviveBar.fillAmount = 0f;
        }

        protected virtual void Update()
        {
            // Secondary 체력바가 Front 체력바를 따라가도록 Lerp 적용
            if (secondaryHealthBar && frontHealthBar)
            {
                secondaryHealthBar.fillAmount = Mathf.Lerp(secondaryHealthBar.fillAmount, frontHealthBar.fillAmount, Time.deltaTime * healthBarLerpSpeed);
            }
        }

        public virtual void UpdateHealthUI(float currentHealth, float maxHealth)
        {
            if (frontHealthBar)
            {
                targetHealthFill = Mathf.Clamp01(currentHealth / maxHealth);
                frontHealthBar.fillAmount = targetHealthFill;
            }
        }


        public virtual void StartReloadUI()
        {
            if (reloadBar)
                reloadBar.fillAmount = 0f;
        }

        public virtual void UpdateReloadProgress(float progress)
        {
            if (reloadBar)
                reloadBar.fillAmount = Mathf.Clamp01(progress);
        }

        public virtual void EndReloadUI()
        {            
            if (reloadBar)
                reloadBar.fillAmount = 0f;
        }



        public virtual void UpdateAmmoUI(int currentAmmo, int maxAmmo)
        {
            if (ammoText)
                ammoText.text = $"{currentAmmo}/{maxAmmo}";
        }

        // UI 초기화 함수
        public virtual void InitializeUI(float initialHealth, float maxHealth, int initialAmmo, int maxAmmo,bool isOwner)
        {
            UpdateHealthUI(initialHealth, maxHealth);
            UpdateAmmoUI(initialAmmo, maxAmmo);
        }
    }
}
