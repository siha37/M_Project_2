using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using MyFolder._1._Scripts._0._Object;
using UnityEngine;
using UnityEngine.VFX;

namespace MyFolder._1._Scripts._13._Card
{
    /// <summary>
    /// 맵에 배치된 보상 획득 오브젝트.
    /// 플레이어가 HoldDuration(5초) 동안 상호작용을 유지하면 RewardManager에 수집 요청을 보낸다.
    /// Tag: "InteractableObj", Trigger Collider2D 필요.
    /// </summary>
    public class RewardObject : NetworkBehaviour, IHoldInteractable
    {
        // ─── SyncVar ────────────────────────────────────────────
        private readonly SyncVar<int> instanceId = new SyncVar<int>();
        private readonly SyncVar<ushort> rewardId = new SyncVar<ushort>();
        private readonly SyncVar<RewardCardRarity> rarity = new SyncVar<RewardCardRarity>();

        // ─── VFX ────────────────────────────────────────────────
        [SerializeField] private VisualEffect vfx;
        [SerializeField] private string vfxColorProperty = "_Color";
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color epicColor = new Color(0.5f, 0f, 1f);
        [SerializeField] private Color legendColor = new Color(1f, 0.7f, 0f);

        // ─── 홀드 상태 (로컬) ────────────────────────────────────
        public float HoldDuration => 5f;
        private GameObject currentInteractor;
        
        // ─── UI ─────────────────────────────────────────────────
        [SerializeField] private GameObject interactionUi;

        // ─── FishNet 생명주기 ─────────────────────────────────────
        public override void OnStartClient()
        {
            rarity.OnChange += OnRarityChanged;
            UpdateVfxColor(rarity.Value);
        }

        public override void OnStopClient()
        {
            rarity.OnChange -= OnRarityChanged;
            UpdateVfxColor(rarity.Value);
        }

        // ─── 서버 초기화 (Spawner 호출 후 RewardManager가 설정) ──
        public void InitializeServer(int id, ushort rewardCardId, RewardCardRarity cardRarity)
        {
            instanceId.Value = id;
            rewardId.Value = rewardCardId;
            rarity.Value = cardRarity;
            UpdateVfxColor(rarity.Value);
        }

        // ─── IHoldInteractable 구현 ───────────────────────────────
        public void StartHold(GameObject interactor)
        {
            currentInteractor = interactor;
        }

        public void CancelHold(GameObject interactor)
        {
            if (currentInteractor == interactor)
                currentInteractor = null;
        }

        public void CompleteHold(GameObject interactor)
        {
            if (currentInteractor != interactor) return;
            currentInteractor = null;

            if (RewardManager.Instance)
                RewardManager.Instance.RequestCollectServerRpc(instanceId.Value,NetworkManager.ClientManager.Connection);
        }

        // ─── VFX 색상 ─────────────────────────────────────────────
        private void OnRarityChanged(RewardCardRarity prev, RewardCardRarity next, bool asServer)
        {
            UpdateVfxColor(next);
        }

        private void UpdateVfxColor(RewardCardRarity cardRarity)
        {
            if (!vfx) return;

            Color color = cardRarity switch
            {
                RewardCardRarity.Epic   => epicColor,
                RewardCardRarity.Legend => legendColor,
                _                       => normalColor
            };

            vfx.SetVector4(vfxColorProperty, color);
        }

        public void interaction_OnOff(bool isOn)
        {
            interactionUi.SetActive(isOn);
        }
    }
}
