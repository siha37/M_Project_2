using MyFolder._1._Scripts._7._PlayerRole;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MyFolder._1._Scripts._1._UI._0._GameStage._0._Agent
{
    public class PlayerMainHUD : MonoBehaviour
    {
        [Header("체력바")]
        [SerializeField] private Image frontHealthBar;
        [SerializeField] private Image secondaryHealthBar;
        [SerializeField] private float healthBarLerpSpeed = 5f;
        private float targetHealthFill;
    
        [Header("실드바")]
        [SerializeField] private Image frontShieldBar;
        [SerializeField] private Image secondaryShieldBar;
        [SerializeField] private float shieldBarLerpSpeed = 5f;
        private float targetShieldFill;


        [Header("탄창")]
        [SerializeField] private TextMeshProUGUI ammoText;
        [SerializeField] private Image ammoBar;

        [Header("룰")] 
        [SerializeField] private Image roleIcon;
        [SerializeField] private Image roleIconFill;
        [SerializeField] private Sprite destoryIconSprite;
        [SerializeField] private Color destoryIconColor;
        [SerializeField] private Color destory_fill_IconColor;
        [SerializeField] private Sprite normalIconSprite;
        [SerializeField] private Color normalIconColor;
        [SerializeField] private Color normal_fill_IconColor;
        
        [Header("진행바")]
        [SerializeField] private PlayerProgressControll playerProgressControl;
        
        [Header("목숨")]
        [SerializeField] private TextMeshProUGUI reviveText;
        
        private void Start()
        {
            if (frontHealthBar) frontHealthBar.fillAmount = 1f;
            
            if (secondaryHealthBar) secondaryHealthBar.fillAmount = 1f;

            if (frontShieldBar) frontShieldBar.fillAmount = 1f;
            
            if (secondaryShieldBar) secondaryShieldBar.fillAmount = 1f;   
        }
        
        private void Update()
        {
            // Secondary 체력바가 Front 체력바를 따라가도록 Lerp 적용
            if (secondaryHealthBar && frontHealthBar)
            {
                secondaryHealthBar.fillAmount = Mathf.Lerp(secondaryHealthBar.fillAmount, frontHealthBar.fillAmount, Time.deltaTime * healthBarLerpSpeed);
            }
            
            // Secondary 체력바가 Front 체력바를 따라가도록 Lerp 적용
            if (secondaryShieldBar && frontShieldBar)
            {
                secondaryShieldBar.fillAmount = Mathf.Lerp(secondaryShieldBar.fillAmount, frontShieldBar.fillAmount, Time.deltaTime * shieldBarLerpSpeed);
            }
        }

        public void SetHealth(float currentHealth, float maxHealth)
        {   
            if (frontHealthBar)
            {
                targetHealthFill = Mathf.Clamp01(currentHealth / maxHealth);
                frontHealthBar.fillAmount = targetHealthFill;
            }
        }

        public void SetShield(float currentShield, float maxShield)
        {
            if (frontShieldBar)
            {
                targetShieldFill = Mathf.Clamp01(currentShield / maxShield);
                frontShieldBar.fillAmount = targetShieldFill;
            }
        }
        public void SetAmmo(int ammo,int Maxammo)
        {
            ammoText.text = ammo+" / "+Maxammo;
            ammoBar.fillAmount = (float)ammo / Maxammo;
        }

        public void SetRoleIcon(PlayerRoleType role)
        {
            switch (role)
            {
                case PlayerRoleType.Normal:
                    roleIcon.sprite = normalIconSprite;
                    roleIcon.color = normalIconColor;
                    roleIconFill.sprite = normalIconSprite;
                    roleIconFill.color = normal_fill_IconColor;
                    break;
                case PlayerRoleType.Destroyer:
                    roleIcon.sprite = destoryIconSprite;
                    roleIcon.color = destoryIconColor;
                    roleIconFill.sprite = destoryIconSprite;
                    roleIconFill.color = destory_fill_IconColor;
                    break;
            }
        }

        public void OnReloadProgress()
        {
            playerProgressControl.ProgressStart("재장전");   
        }
        public void OffReloadProgress()
        {
            playerProgressControl.ProgressEnd();
        }
        public void UpdateReloadProgress(float reloadProgress)
        {
            playerProgressControl.ProgressUpdate(reloadProgress);
        }
        
        public void OnReviveProgress()
        {
            playerProgressControl.ProgressStart("부활 시도");   
        }
        public void OffReviveProgress()
        {
            playerProgressControl.ProgressEnd();
        }
        public void UpdateReviveProgress(float reloadProgress)
        {
            playerProgressControl.ProgressUpdate(reloadProgress);
        }
        
        public void On_OnHealProgress()
        {
            playerProgressControl.ProgressStart("회복 활성화");   
        }
        public void Off_OnHealProgress()
        {
            playerProgressControl.ProgressEnd();
        }
        public void Update_OnHealProgress(float reloadProgress)
        {
            playerProgressControl.ProgressUpdate(reloadProgress);
        }
        
        public void On_OffHealProgress()
        {
            playerProgressControl.ProgressStart("회복 비활성화");   
        }
        public void Off_OffHealProgress()
        {
            playerProgressControl.ProgressEnd();
        }
        public void Update_OffHealProgress(float reloadProgress)
        {
            playerProgressControl.ProgressUpdate(reloadProgress);
        }
        
        public void UpdateCamouflageCooldownUI(float currty ,float camouflageCooldown)
        {
            roleIconFill.fillAmount = 1 - currty / camouflageCooldown;
        }

        public void UpdateCamouflageDisguiseUI(float currty ,float camouflageDisguise)
        {
            roleIconFill.fillAmount = currty / camouflageDisguise;
        }

        public void UpdateReviveAmount(int reviveAmount)
        {
            reviveText.text = reviveAmount.ToString();
        }
    }
}
