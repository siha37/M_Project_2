using System.Collections;
using FishNet;
using FishNet.Managing.Client;
using MyFolder._1._Scripts._1._UI._3._Cursor;
using MyFolder._1._Scripts._7._PlayerRole;
using MyFolder._1._Scripts._8999._Utility.Corutin;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MyFolder._1._Scripts._1._UI._0._GameStage._0._Agent
{
    public class PlayerUI : AgentUI
    {
        private PlayerMainHUD playerMainHUD = null;
        private AnimatedCursor animatedCursor = null;

        [Header("방어력")]
        [SerializeField] protected Image frontShieldBar;
        [SerializeField] protected Image secondaryShieldBar;
        [SerializeField] protected float shieldBarLerpSpeed = 5f;
        
        [Header("Name")]
        [SerializeField] protected TextMeshProUGUI nameText;
        
        protected float targetShieldFill;

        protected override void Update()
        {
            base.Update();
            
            // Secondary 체력바가 Front 체력바를 따라가도록 Lerp 적용
            if (secondaryShieldBar && frontShieldBar)
            {
                secondaryShieldBar.fillAmount = Mathf.Lerp(secondaryShieldBar.fillAmount, frontShieldBar.fillAmount, Time.deltaTime * shieldBarLerpSpeed);
            }
        }
        
        public override void InitializeUI(float initialHealth, float maxHealth, int initialAmmo, int maxAmmo,
            bool isOwner)
        {
            base.InitializeUI(initialHealth, maxHealth, initialAmmo, maxAmmo, isOwner);
            if (isOwner)
            {
                playerMainHUD = FindFirstObjectByType<PlayerMainHUD>();
                animatedCursor = FindFirstObjectByType<AnimatedCursor>();
                if (!playerMainHUD)
                    Invoke(nameof(GetHUD), 1);
            }
        }
        public void InitializePlayerUI(float initialHealth, float maxHealth, int initialAmmo, int maxAmmo,float initialShield,float maxShield
            ,int reviveAmount,bool isOwner)
        {
            InitializeUI(initialHealth, maxHealth, initialAmmo, maxAmmo, isOwner);
            UpdateShieldUI(initialShield, maxShield);
            UpdateReviveAmount(reviveAmount);
        }

        private void GetHUD()
        {
            playerMainHUD = FindFirstObjectByType<PlayerMainHUD>();
        }

        public override void UpdateHealthUI(float currentHealth, float maxHealth)
        {
            base.UpdateHealthUI(currentHealth, maxHealth);
            if (playerMainHUD)
                playerMainHUD.SetHealth(currentHealth, maxHealth);
        }

        public void EmptyCursor(bool enable)
        {
            if(animatedCursor)
                animatedCursor.EmptyCursor(enable);
        }

        public void ShotCursor()
        {
            if(animatedCursor)
            {
                animatedCursor.ShotCursor();
            }
        }
        
        public override void StartReloadUI()
        {
            base.StartReloadUI();
            //if (playerMainHUD) playerMainHUD.OnReloadProgress();
            if(animatedCursor)
                animatedCursor.ReloadCursor(true);
        }

        public override void UpdateReloadProgress(float progress)
        {
            base.UpdateReloadProgress(progress);
            //if (playerMainHUD) playerMainHUD.UpdateReloadProgress(progress);
            if(animatedCursor)
                animatedCursor.ReloadCursor_Update(progress);
        }

        public override void EndReloadUI()
        {            
            base.EndReloadUI();
            //if (playerMainHUD) playerMainHUD.OffReloadProgress();
            EmptyCursor(false);
            if(animatedCursor)
                animatedCursor.ReloadCursor(false);
            
        }
        public void UpdateShieldUI(float currentShield, float maxShield)
        {
            if (frontShieldBar)
            {
                targetShieldFill = Mathf.Clamp01(currentShield / maxShield);
                frontShieldBar.fillAmount = targetShieldFill;
            }
            if (playerMainHUD)
                playerMainHUD.SetShield(currentShield, maxShield);
        }

        public override void UpdateAmmoUI(int currentAmmo, int maxAmmo)
        {
            base.UpdateAmmoUI(currentAmmo, maxAmmo);
            if (playerMainHUD)
                playerMainHUD.SetAmmo(currentAmmo, maxAmmo);
        }

        public void SetRoleImage(PlayerRoleType role)
        {
            if (playerMainHUD)
                playerMainHUD.SetRoleIcon(role);
            else
                StartCoroutine(nameof(RetrySetRoleImage),role);
        }

        public IEnumerator RetrySetRoleImage(PlayerRoleType role)
        {
            while(!playerMainHUD)
                yield return WaitForSecondsCache.Get(0.3f);
            if (playerMainHUD)
                playerMainHUD.SetRoleIcon(role);
        }

        public void StartReviveProgress()
        {
            if (playerMainHUD)
                playerMainHUD.OnReviveProgress();
        }

        public void EndReviveProgress()
        {
            if (playerMainHUD)
                playerMainHUD.OffReviveProgress();
        }
        public void UpdateReviveProgress(float progress)
        {
            if (reviveBar)
                reviveBar.fillAmount = Mathf.Clamp01(progress);
        }

        public void UpdateReviveProgressIsOwner(float progress)
        {
            if (playerMainHUD)
                playerMainHUD.UpdateReviveProgress(progress);
        }
        public void UpdateCamouflageCooldownUI(float currty ,float camouflageCooldown)
        {
            if(playerMainHUD)
                playerMainHUD.UpdateCamouflageCooldownUI(currty, camouflageCooldown);
        }

        public void UpdateCamouflageDisguiseUI(float currty ,float camouflageDisguise)
        {
            if(playerMainHUD)
                playerMainHUD.UpdateCamouflageDisguiseUI(currty, camouflageDisguise);
        }
        public void StartOnHealProgress()
        {
            if (playerMainHUD)
                playerMainHUD.On_OnHealProgress();
        }

        public void EndOnHealProgress()
        {
            if (playerMainHUD)
                playerMainHUD.Off_OnHealProgress();
        }
        public void UpdateOnHealProgress(float progress)
        {
            if (playerMainHUD)
                playerMainHUD.Update_OnHealProgress(progress);
        }
        
        public void StartOffHealProgress()
        {
            if (playerMainHUD)
                playerMainHUD.On_OffHealProgress();
        }

        public void EndOffHealProgress()
        {
            if (playerMainHUD)
                playerMainHUD.Off_OffHealProgress();
        }
        public void UpdateOffHealProgress(float progress)
        {
            if (playerMainHUD)
                playerMainHUD.Update_OffHealProgress(progress);
        }
        public void UpdateReviveAmount(int reviveAmount)
        {
            if (playerMainHUD)
                playerMainHUD.UpdateReviveAmount(reviveAmount);
        }
    }
}