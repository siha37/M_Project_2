using System;
using System.Collections;
using System.Collections.Generic;
using FishNet.Object;
using MyFolder._1._Scripts._3._SingleTone;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.EventSystems;

namespace MyFolder._1._Scripts._0._Object._0._Agent._0._Player
{
    public class PlayerInputControll : NetworkBehaviour
    {
        //INPUTACTIONS
        [SerializeField] PlayerInput playerinput;
        InputAction move;
        InputAction look;
        InputAction attack;
        InputAction defence;
        InputAction interact;
        InputAction reload;
        InputAction skill_1;
        InputAction heal;

        private PlayerContext p_context;

        string Keyboard = "PlayerKeyboard";
        string Gamepad = "PlayerPad";
        
        
        public enum ControllerType
        {
            Keyboard,
            Gamepad
        }
        public ControllerType controllerType;

        public delegate void InputActionCallBack();
        public delegate void InputActionVector2CallBack(Vector2 vector2);

        public InputActionVector2CallBack movePerformedCallback;
        public InputActionCallBack moveStopCallback;
        
        public InputActionVector2CallBack lookPerformedCallback;
        
        public InputActionCallBack attackStartCallback;
        public InputActionCallBack attackCallback;
        public InputActionCallBack attackCancelCallback;
        
        public InputActionVector2CallBack defenceControllCallback;
        
        public InputActionCallBack reloadCallback;
        
        public InputActionCallBack interactStartCallback;
        public InputActionCallBack interactPerformedCallback;
        public InputActionCallBack interactCanceledCallback;
        
        public InputActionCallBack skill_1StartCallback;
        public InputActionCallBack skill_1StopCallback;
        
        public InputActionCallBack heal_Callback;

        public bool IsActive_skill_1 = false;
        private Vector2 Mouse_position;
        
        // ✅ 클릭 차단에서 제외할 Canvas 목록 (적 HP 바, 이름표 등)
        [SerializeField] private List<string> excludedCanvasTags = new List<string> { "AgentUI" };

        public override void OnStartClient()
        {
            TryGetComponent(out p_context);
            
            if (!IsOwner)
            {
                // 원격 플레이어는 입력 완전 차단 + 장치 언페어(장치 훔치기 방지)
                playerinput.DeactivateInput();
                if (playerinput.user.valid)
                    playerinput.user.UnpairDevicesAndRemoveUser();
                playerinput.enabled = false;
            }
            else
            {
                playerinput.enabled = true;
                switch (controllerType)
                {
                    case ControllerType.Keyboard:
                        playerinput.SwitchCurrentActionMap(Keyboard);
                        break;
                    case ControllerType.Gamepad:
                        playerinput.SwitchCurrentActionMap(Gamepad);
                        break;
                }
                playerinput.neverAutoSwitchControlSchemes = true; // 의도치 않은 스킴 전환 방지
                
                RegisterInputActions();
                
                playerinput.ActivateInput();
                
                
                // ✅ PlayerStatus 데이터 로딩 이벤트 구독
                if (p_context.Status)
                {
                    p_context.Status.OnDataRefreshed += OnPlayerDataRefreshed;
                }
            }
        }

        private void OnEnable()
        {
            if(IsOwner)
                RegisterInputActions();
        }

        private void OnDisable()
        {
            if(IsOwner)
            {
                UnregisterInputActions();
                // ✅ 공격 상태 초기화
                isAttacking = false;
                StopAllCoroutines();
            }
        }

        private bool isAttacking = false;
        public bool IsAttacking => isAttacking;

        // ✅ 데이터 안전성 검사
        private bool IsPlayerDataSafe()
        {
            return p_context.Status && 
                   p_context.Status.PlayerData != null && 
                   p_context.Status.PlayerData.IsData;
        }

        /// <summary>
        /// PlayerData 로딩 완료 시 호출되는 콜백
        /// </summary>
        private void OnPlayerDataRefreshed()
        {
            LogManager.Log(LogCategory.Player, $"{gameObject.name} PlayerData 로딩 완료 - 입력 제어 업데이트", this);
        }

        /// <summary>
        /// UI 위에 포인터가 있는지 확인하는 헬퍼 메서드
        /// 제외할 Canvas(적 HP 바, 이름표 등)는 무시하고 UI 버튼만 감지
        /// </summary>
        private bool IsPointerOverUI()
        {
            // EventSystem이 없으면 false 반환
            if (!EventSystem.current)
                return false;

            // ✅ Input System에서 마우스 위치 가져오기
            Vector2 mousePosition = Vector2.zero;
            if (Mouse.current != null)
            {
                mousePosition = Mouse.current.position.ReadValue();
            }
            else
            {
                return false; // 마우스가 없으면 false
            }

            // PointerEventData 생성
            PointerEventData pointerData = new PointerEventData(EventSystem.current)
            {
                position = mousePosition
            };

            // Raycast 결과 저장
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);

            // 결과 중 제외할 Canvas가 아닌 UI가 있는지 확인
            foreach (RaycastResult result in results)
            {
                Canvas canvas = result.gameObject.GetComponentInParent<Canvas>();
                if (canvas != null)
                {
                    // ✅ 제외 태그가 있는 Canvas는 무시 (적 HP 바, 이름표 등)
                    bool isExcluded = false;
                    foreach (string tag in excludedCanvasTags)
                    {
                        if (canvas.CompareTag(tag))
                        {
                            isExcluded = true;
                            break;
                        }
                    }
                    
                    // 제외되지 않은 Canvas의 UI를 찾았다면 true 반환
                    if (!isExcluded)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
        

        private void MovePerformed(InputAction.CallbackContext context)
        {
            // ✅ 데이터 안전성 검사
            if (!IsPlayerDataSafe())
            {
                LogManager.LogWarning(LogCategory.Player, $"{gameObject.name} PlayerData가 로딩되지 않아 이동 입력 무시", this);
                return;
            }
            
            Vector2 inputVector = context.ReadValue<Vector2>();
            LogManager.Log(LogCategory.Player,$"[PlayerInputControll] MovePerformed 호출됨 - Input: {inputVector}, Callback 등록됨: {movePerformedCallback != null}");
            movePerformedCallback?.Invoke(inputVector);
        }
        private void MoveCancle(InputAction.CallbackContext context)
        {
            moveStopCallback?.Invoke();
        }
        
        private void LookPerformed(InputAction.CallbackContext context)
        {
            // ✅ 데이터 안전성 검사
            if (!IsPlayerDataSafe())
            {
                return; // Look은 로그 없이 조용히 무시
            }

            Mouse_position = context.ReadValue<Vector2>();
            lookPerformedCallback?.Invoke(context.ReadValue<Vector2>());
        }
        
        private void AttackStart(InputAction.CallbackContext context)
        {
            // ✅ UI 위에 마우스가 있는지 확인
            if (IsPointerOverUI())
            {
                return; // UI 클릭 시 공격 무시
            }

            // ✅ 데이터 안전성 검사
            if (!IsPlayerDataSafe())
            {
                LogManager.LogWarning(LogCategory.Player, $"{gameObject.name} PlayerData가 로딩되지 않아 공격 입력 무시", this);
                return;
            }
            
            isAttacking = true;
            attackStartCallback?.Invoke();
            StartCoroutine(AttackLoop());
        }
        private void AttackCancel(InputAction.CallbackContext context)
        {
            // ✅ 데이터 안전성 검사
            if (!IsPlayerDataSafe())
            {
                LogManager.LogWarning(LogCategory.Player, $"{gameObject.name} PlayerData가 로딩되지 않아 이동 입력 무시", this);
                return;
            }
            
            isAttacking = false;
            attackCancelCallback?.Invoke();
        }
        private IEnumerator AttackLoop()
        {
            // ✅ 데이터 안전성 검사
            if (!IsPlayerDataSafe())
            {
                LogManager.LogWarning(LogCategory.Player, $"{gameObject.name} PlayerData가 로딩되지 않아 이동 입력 무시", this);
                yield break;
            }
            while (isAttacking)
            {
                attackCallback?.Invoke();
                yield return null;
            }
        }

        private void DefenceStart(InputAction.CallbackContext context)
        {
            // ✅ UI 위에 마우스가 있는지 확인
            if (IsPointerOverUI())
            {
                return; // UI 클릭 시 방어 무시
            }

            // ✅ 데이터 안전성 검사
            if (!IsPlayerDataSafe())
            {
                if(gameObject)
                    LogManager.LogWarning(LogCategory.Player, $"{gameObject.name} PlayerData가 로딩되지 않아 공격 입력 무시", this);
                return;
            }
            
            defenceControllCallback?.Invoke(Mouse_position);
        }
        
        private void ReloadStart(InputAction.CallbackContext context)
        {
            // ✅ 데이터 안전성 검사
            if (!IsPlayerDataSafe())
            {
                LogManager.LogWarning(LogCategory.Player, $"{gameObject.name} PlayerData가 로딩되지 않아 재장전 입력 무시", this);
                return;
            }
            
            reloadCallback?.Invoke();
        }
        
        private void InteractStart(InputAction.CallbackContext context)
        {
            interactStartCallback?.Invoke();
        }
        private void InteractPerformed(InputAction.CallbackContext context)
        {
            interactPerformedCallback?.Invoke();
        }
        private void InteractCanceled(InputAction.CallbackContext context)
        {
            interactCanceledCallback?.Invoke();
        }

        private void Skill1Start(InputAction.CallbackContext context)
        {
            if(!IsActive_skill_1)
                return;
                
            // ✅ 데이터 안전성 검사
            if (!IsPlayerDataSafe())
            {
                LogManager.LogWarning(LogCategory.Player, $"{gameObject.name} PlayerData가 로딩되지 않아 스킬 입력 무시", this);
                return;
            }
            
            skill_1StartCallback?.Invoke();
        }
        private void Skill1Stop(InputAction.CallbackContext context)
        {
            skill_1StopCallback?.Invoke();
        }

        private void HealEnter(InputAction.CallbackContext context)
        {
            heal_Callback?.Invoke();
        }

        public override void OnStopClient()
        {
            UnregisterInputActions();
            
            // ✅ PlayerStatus 이벤트 구독 해제
            if (p_context.Status != null)
            {
                p_context.Status.OnDataRefreshed -= OnPlayerDataRefreshed;
            }
        }

        private void OnDestroy()
        {
            try
            {
                // ✅ 안전한 입력 액션 해제
                if (playerinput != null && playerinput.enabled)
                {
                    UnregisterInputActions();
                }
                
                // ✅ 안전한 PlayerStatus 이벤트 구독 해제
                if (p_context != null && p_context.Status != null)
                {
                    p_context.Status.OnDataRefreshed -= OnPlayerDataRefreshed;
                }
            }
            catch (System.Exception ex)
            {
                LogManager.LogWarning(LogCategory.Player, $"PlayerInputControll OnDestroy 오류: {ex.Message}", this);
            }
        }

        private void RegisterInputActions()
        {
            if (!playerinput || !playerinput.enabled || !IsOwner)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"[PlayerInputControll] RegisterInputActions 실패 - PlayerInput: {playerinput != null}, Enabled: {playerinput?.enabled}, IsOwner: {IsOwner}");
#endif
                return;
            }
            #if UNITY_EDITOR
            Debug.Log($"[PlayerInputControll] RegisterInputActions 시작 - {gameObject.name}");
            #endif
            move = playerinput.currentActionMap.FindAction("Move");
            look = playerinput.currentActionMap.FindAction("Look");
            attack = playerinput.currentActionMap.FindAction("Attack");
            defence = playerinput.currentActionMap.FindAction("Defence");
            interact = playerinput.currentActionMap.FindAction("Interact");
            reload = playerinput.currentActionMap.FindAction("Reload");
            skill_1 = playerinput.currentActionMap.FindAction("Skill_1");
            heal = playerinput.currentActionMap.FindAction("Heal");
            
            if (move != null)
            {
                move.performed += MovePerformed;
                move.canceled += MoveCancle;
            }
        
            if (look != null)
            {
                look.performed += LookPerformed;
            }
        
            if (attack != null)
            {
                attack.started += AttackStart;
                attack.canceled += AttackCancel;
            }

            if (defence != null)
            {
                defence.started += DefenceStart;
            }
            
            if (interact != null)
            {
                interact.started += InteractStart;
                interact.performed += InteractPerformed;
                interact.canceled += InteractCanceled;
            }
        
            if (reload != null)
            {
                reload.started += ReloadStart;
            }

            if (skill_1 != null)
            {
                skill_1.started += Skill1Start;
                skill_1.canceled += Skill1Stop;
            }

            if (heal != null)
            {
                heal.started += HealEnter;
            }
        }
        private void UnregisterInputActions()
        {
            try
            {
                if (!playerinput || !playerinput.enabled)
                    return;
                    
                if (move != null)
                {
                    move.performed -= MovePerformed;
                    move.canceled -= MoveCancle;
                    move = null;
                }
            
                if (look != null)
                {
                    look.performed -= LookPerformed;
                    look = null;
                }
            
                if (attack != null)
                {
                    attack.started -= AttackStart;
                    attack.canceled -= AttackCancel;
                    attack = null;
                }
            
                if (defence != null)
                {
                    defence.started -= DefenceStart;
                    defence = null;
                }
                
                if (interact != null)
                {
                    interact.started -= InteractStart;
                    interact.performed -= InteractPerformed;
                    interact.canceled -= InteractCanceled;
                    interact = null;
                }
            
                if (reload != null)
                {
                    reload.started -= ReloadStart;
                    reload = null;
                }
                
                if (skill_1 != null)
                {
                    skill_1.started -= Skill1Start;
                    skill_1.canceled -= Skill1Stop;
                    skill_1 = null;
                }

                if (heal != null)
                {
                    heal.started -= HealEnter;
                }
            }
            catch (System.Exception ex)
            {
                LogManager.LogWarning(LogCategory.Player, $"UnregisterInputActions 오류: {ex.Message}", this);
            }
        }
    }
}
