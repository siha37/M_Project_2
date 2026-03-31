using System;
using System.Collections;
using FishNet.Object;
using MyFolder._1._Scripts._0._Object._0._Agent._0._Player._0._Component;
using MyFolder._1._Scripts._1._UI._0._GameStage._0._Agent;
using MyFolder._1._Scripts._3._SingleTone;
using Unity.Services.Matchmaker.Models;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MyFolder._1._Scripts._0._Object._0._Agent._0._Player
{
    public class PlayerInteractController : NetworkBehaviour
    {
        [SerializeField] private PlayerContext context;
        [SerializeField] private IntractArea interactArea;
        private GameObject currentInteractableObject;
        private Coroutine reviveCoroutine;
        private Coroutine holdCoroutine;
        public bool isActive = false;

        public override void OnStartClient()
        {
            if (IsOwner)
            {
                if (!interactArea || !context.Input || !context.AgentUI)
                {
                    LogManager.LogError(LogCategory.Player, $"{gameObject.name} 컴포넌트가 없습니다.", this);
                    enabled = false;
                    return;
                }

                ConnectEvent();
            }
        }

        private void OnEnable()
        {
            ConnectEvent();
        }

        private void OnDisable()
        {
            DisconnectEvent();
        }

        private void OnDestroy()
        {
            DisconnectEvent();
        }

        private void ConnectEvent()
        {
            if (context.Input)
            {
                context.Input.interactStartCallback += OnInteractStart;
                context.Input.interactPerformedCallback += OnInteractPerformed;
                context.Input.interactCanceledCallback += OnInteractCanceled;
                context.Status.OnReviveAbleDeathEvent += OnInteractCanceled;
            }
        }

        private void DisconnectEvent()
        {
            if (context.Input)
            {
                context.Input.interactStartCallback -= OnInteractStart;
                context.Input.interactPerformedCallback -= OnInteractPerformed;
                context.Input.interactCanceledCallback -= OnInteractCanceled;
                context.Status.OnReviveAbleDeathEvent -= OnInteractCanceled;
            }
        }

        private void OnInteractStart()
        {
            // 이미 상호작용 중이면 새로운 상호작용 불가
            if (isActive) return;

            if (context.Component.GetPComponent<PlayerHealComponent>() is PlayerHealComponent heal)
            {
                if (heal.headling) return;
            }

            currentInteractableObject = interactArea.GetNearestObject();
            
            if (currentInteractableObject)
            {
                if (currentInteractableObject.CompareTag("InteractableObj"))
                {
                    // 홀드 상호작용 우선 확인
                    IHoldInteractable holdInteractable;
                    if (currentInteractableObject.TryGetComponent(out holdInteractable))
                    {
                        holdCoroutine = StartCoroutine(HoldInteractionRoutine(holdInteractable));
                        return;
                    }

                    // 일반 즉시 상호작용
                    IInteractable interactable = currentInteractableObject.GetComponent<IInteractable>();
                    interactable?.Interact(gameObject);
                }
                else if (currentInteractableObject.CompareTag("Player"))
                {
                    PlayerNetworkSync playerSync = currentInteractableObject.GetComponent<PlayerNetworkSync>();
                    if (playerSync && playerSync.IsDead())
                    {
                        if (reviveCoroutine != null)
                            StopCoroutine(reviveCoroutine);
                        reviveCoroutine = StartCoroutine(RevivePlayerNetwork(playerSync));
                    }
                }
            }
        }

        private IEnumerator HoldInteractionRoutine(IHoldInteractable target)
        {
            float elapsed = 0f;
            float duration = target.HoldDuration;

            target.StartHold(gameObject);
            context.AgentUI.StartRewardHoldProgress();
            context.Controller.IsMovable = false;
            context.Controller.MoveStop();
            isActive = true;
            context.Shooter.OnAttack = false;

            while (elapsed < duration)
            {
                // 대상이 사라지거나 범위를 벗어나면 취소
                if (!currentInteractableObject || !interactArea.GetInteractableList().Contains(currentInteractableObject))
                {
                    CancelHold(target);
                    yield break;
                }

                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                context.AgentUI.UpdateRewardHoldProgress(progress);
                yield return null;
            }

            context.Controller.IsMovable = true;
            context.Shooter.OnAttack = true;
            context.AgentUI.EndRewardHoldProgress();
            isActive = false;
            holdCoroutine = null;

            target.CompleteHold(gameObject);
        }

        private void CancelHold(IHoldInteractable target)
        {
            context.Controller.IsMovable = true;
            context.Shooter.OnAttack = true;
            context.AgentUI.EndRewardHoldProgress();
            isActive = false;
            holdCoroutine = null;

            target.CancelHold(gameObject);
        }

        private IEnumerator RevivePlayerNetwork(PlayerNetworkSync targetNetworkSync)
        {
            float elapsedTime = 0f;
            context.Sync.OnRevivedStart();
            context.AgentUI.StartReviveProgress();
            context.Controller.IsMovable = false;
            context.Controller.MoveStop();
            isActive = true;
            context.Shooter.OnAttack = false;

            while (elapsedTime < PlayerStatus.reviveDelay)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / PlayerStatus.reviveDelay;
                context.AgentUI.UpdateReviveProgressIsOwner(progress);
                yield return null;
            }

            context.Controller.IsMovable = true;
            context.Shooter.OnAttack = true;
            targetNetworkSync.RequestRevive();
            context.Sync.OnRevivedEnd();
            context.AgentUI.EndReviveProgress();
            isActive = false;
            reviveCoroutine = null;
        }

        private void OnInteractPerformed()
        {
        }

        private void OnInteractCanceled()
        {
            if (holdCoroutine != null)
            {
                IHoldInteractable holdInteractable = currentInteractableObject
                    ? currentInteractableObject.GetComponent<IHoldInteractable>()
                    : null;

                StopCoroutine(holdCoroutine);
                if (holdInteractable != null)
                    CancelHold(holdInteractable);
                else
                {
                    context.Controller.IsMovable = true;
                    context.Shooter.OnAttack = true;
                    context.AgentUI.EndRewardHoldProgress();
                    isActive = false;
                    holdCoroutine = null;
                }
            }

            if (reviveCoroutine != null)
            {
                context.Controller.IsMovable = true;
                context.Shooter.OnAttack = true;

                StopCoroutine(reviveCoroutine);
                context.Sync.OnRevivedEnd();
                context.AgentUI.EndReviveProgress();
                isActive = false;
                reviveCoroutine = null;
                context.AgentUI.UpdateReviveProgressIsOwner(0);
            }

            currentInteractableObject = null;
        }
    }
}
