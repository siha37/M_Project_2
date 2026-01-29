using System;
using System.Collections;
using MyFolder._1._Scripts._0._Object._0._Agent._0._Player._0._Component;
using MyFolder._1._Scripts._1._UI._0._GameStage._0._Agent;
using MyFolder._1._Scripts._3._SingleTone;
using Unity.Services.Matchmaker.Models;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MyFolder._1._Scripts._0._Object._0._Agent._0._Player
{
    public class PlayerInteractController : MonoBehaviour
    {
        [SerializeField] private PlayerContext context;
        [SerializeField] private IntractArea interactArea;
        private GameObject currentInteractableObject;
        private Coroutine reviveCoroutine;
        public bool isActive =false;
        private void Start()
        {
            if (!interactArea)
            {
                LogManager.LogError(LogCategory.Player, $"{gameObject.name} IntractArea 컴포넌트가 없습니다.", this);
                enabled = false;
                return;
            }

            if (!context.Input)
            {
                LogManager.LogError(LogCategory.Player, $"{gameObject.name} PlayerInputControll 컴포넌트가 없습니다.", this);
                enabled = false;
                return;
            }

            if (!context.AgentUI)
            {
                LogManager.LogError(LogCategory.Player, $"{gameObject.name} AgentUI 컴포넌트가 없습니다.", this);
                enabled = false;
                return;
            }

            
            ConnectEvent();
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
            // 이벤트 연결 해제
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
            // 이벤트 연결 해제
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
            if (context.Component.GetPComponent<PlayerHealComponent>() is PlayerHealComponent HEAL)
            {
                if(HEAL.headling)
                    return;
            }
            // 가장 가까운 상호작용 가능한 오브젝트 찾기
            currentInteractableObject = interactArea.GetNearestObject();
            
            if (currentInteractableObject)
            {
                if (currentInteractableObject.CompareTag("Object"))
                {
                    // Object와 상호작용
                    IInteractable interactable = currentInteractableObject.GetComponent<IInteractable>();
                    interactable?.Interact(gameObject);
                }
                else if (currentInteractableObject.CompareTag("Player"))
                {
                    // Player의 상태 확인
                    PlayerNetworkSync playerSync = currentInteractableObject.GetComponent<PlayerNetworkSync>();
                    if (playerSync && playerSync.IsDead())
                    {
                        // 이전 부활 코루틴이 있다면 중지
                        if (reviveCoroutine != null)
                        {
                            StopCoroutine(reviveCoroutine);
                        }
                        // 새로운 부활 처리 시작
                        reviveCoroutine = StartCoroutine(RevivePlayerNetwork(playerSync));
                    }
                }
            }
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
            // 네트워크 동기화된 부활 처리
            targetNetworkSync.RequestRevive();
            context.Sync.OnRevivedEnd();
            context.AgentUI.EndReviveProgress();
            isActive = false;
            reviveCoroutine = null;
        }

        private void OnInteractPerformed()
        {
            // 상호작용 수행 중 처리
        }

        private void OnInteractCanceled()
        {
            // 상호작용 취소 시 처리
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
