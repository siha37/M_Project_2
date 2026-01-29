using MyFolder._1._Scripts._3._SingleTone;
using UnityEngine;

namespace MyFolder._1._Scripts._0._Object._0._Agent._0._Player._0._Component
{
    public class PlayerDefenceComponent : IPlayerUpdateComponent
    {
    
        private float lookAngle;
        private float targetLookAngle;
        private float currentLookAngle;
        public float LookAngle => lookAngle;
        PlayerCamouflageComponent camouflageComponent;
        
        private PlayerContext context;
        
        
        [Header("Look Settings")]
        [SerializeField] private float gamepadDeadzone = 0.15f;
        [SerializeField] private float lookSensitivity = 1f;
        [SerializeField] private float lookSmoothing = 10f;
        [SerializeField] private bool enableLookSmoothing = false;
        
        //네트워크 동기화 딜레이
        private float lastNetworkSyncTime = 0f;
        
        // 방어 보간용 타겟 값
        protected float defencetLookAngle;
        protected float currentDefenceLookAngle;
        protected bool shouldInterpolateDefenceRotation = false;


        private bool recoverAble;
        private float currentRecoverTime;
        
        public void Start(PlayerContext context)
        {
            this.context = context;
        }

        public void Stop()
        {
        }

        public void SetKeyEvent(PlayerInputControll inputControll)
        {
            inputControll.lookPerformedCallback += SetDefenceBallPosition;
        }

        public void KeyEnter()
        {
        }

        public void KeyPress()
        {
        }

        public void KeyExit()
        {
        }

        public void Update()
        {
            if (shouldInterpolateDefenceRotation)
            {
                // 부드러운 각도 보간
                float angleDifference = Mathf.DeltaAngle(currentDefenceLookAngle, defencetLookAngle);
            
                if (Mathf.Abs(angleDifference) > 0.5f) // 0.5도 이상 차이날 때만 보간
                {
                    currentDefenceLookAngle = Mathf.LerpAngle(currentDefenceLookAngle, defencetLookAngle, 
                        Time.deltaTime * 20);
                
                    // 실제 회전 적용
                    context.DefencePivot.rotation = Quaternion.Euler(0, 0, currentDefenceLookAngle);
                }
                else
                {
                    // 거의 도달했으면 정확한 값으로 설정
                    currentDefenceLookAngle = defencetLookAngle;
                    // 실제 회전 적용
                    context.DefencePivot.rotation = Quaternion.Euler(0, 0, currentDefenceLookAngle);
                    shouldInterpolateDefenceRotation = false;
                }
            }

        }

        public void FixedUpdate()
        {
            RecoverDelay();
        }

        public void LateUpdate()
        {
        }

        #region Rotation

        
        /// <summary>
        /// 입력 확인 후 볼 이동
        /// </summary>
        private void SetDefenceBallPosition(Vector2 position)
        {
            if (!context.Controller.IsOwner) return;
            
            // ✅ 위장 중이면 방패 작동 불가
            if(camouflageComponent!= null)
            {
                camouflageComponent = context.Component?.GetPComponent<PlayerCamouflageComponent>() as PlayerCamouflageComponent;
            }
            if (camouflageComponent != null && camouflageComponent.IsDisguised)
            {
                return;
            }
            
            if (!context.MainCamera)
            {
                context.SetCamera = Camera.main;
                if (!context.MainCamera)
                {
                    LogManager.LogError(LogCategory.Player, "[DefenceComponent]No MainCamera found");
                    return;
                }
            }
            
            Vector2 targetVector = Vector2.zero;
        
            switch (context.Input.controllerType)
            {
                case PlayerInputControll.ControllerType.Keyboard:
                    // 개선된 마우스 위치 계산
                    Vector2 worldMousePos = GetWorldMousePosition(position);
                    targetVector = worldMousePos - (Vector2)context.transform.position;
                    break;
                
                case PlayerInputControll.ControllerType.Gamepad:
                    // 개선된 데드존 처리
                    float inputMagnitude = position.sqrMagnitude;
                    if (inputMagnitude > gamepadDeadzone * gamepadDeadzone)
                    {
                        // 데드존 보정 적용
                        float correctedMagnitude = (Mathf.Sqrt(inputMagnitude) - gamepadDeadzone) / (1f - gamepadDeadzone);
                        targetVector = position.normalized * correctedMagnitude;
                    }
                    else
                    {
                        return; // 데드존 내부면 무시
                    }
                    break;
            }
            
            // 개선된 각도 계산
            UpdateLookAngle(targetVector);
            defencetLookAngle = lookAngle;
            context.Sync.RequestDefenceLookAngle(lookAngle);
            // 첫 번째 값이면 즉시 설정
            if (Mathf.Abs(currentDefenceLookAngle) < 0.01f)
            {
                currentDefenceLookAngle = lookAngle;
                // 실제 회전 적용
                context.DefencePivot.rotation = Quaternion.Euler(0, 0, currentDefenceLookAngle);
                shouldInterpolateDefenceRotation = false;
            }
            else
            {
                // 일반적인 경우: 보간 처리 시작
                shouldInterpolateDefenceRotation = true;
            }
            
        }
        
            
        private void UpdateLookAngle(Vector2 targetVector)
        {
            targetLookAngle = Mathf.Atan2(targetVector.y, targetVector.x) * Mathf.Rad2Deg * lookSensitivity;
        
            if (enableLookSmoothing)
            {
                currentLookAngle = Mathf.LerpAngle(currentLookAngle, targetLookAngle, 
                    Time.deltaTime * lookSmoothing);
                lookAngle = currentLookAngle;
            }
            else
            {
                lookAngle = targetLookAngle;
            }
        }
        
        private Vector2 GetWorldMousePosition(Vector2 screenPosition)
        {
            if (!context.MainCamera) return Vector2.zero;
        
            // 2D 게임에서 정확한 월드 좌표 계산
            // 카메라에서 게임 월드(Z=0)까지의 거리 사용
            float distanceToGameWorld = Mathf.Abs(context.MainCamera.transform.position.z);
            Vector3 worldPos = context.MainCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, distanceToGameWorld));
            return new Vector2(worldPos.x, worldPos.y);
        }

        #endregion
        
        #region Defence

        /// <summary>
        /// 피해 적용 및 죽음 처리
        /// </summary>
        /// <param name="damage"></param>
        /// <param name="hitDirection"></param>
        // ✅ UI 업데이트 완전 제거, 순수 데미지 계산만
        public void TakeDefence(float damage, Vector2 hitDirection = default)
        {
            if (context.Status.isDead || context.Status.IsCrackDefence) return;
        
            context.Status.currentDefence -= damage;
            context.Status.currentDefence = Mathf.Clamp(context.Status.currentDefence, 0, context.Status.PlayerData.defence);
        
            if (context.Status.currentDefence <= 0)
            {
                //방어막 비활성화
                context.Status.IsCrackDefence = true;
            }
            if (context.Status.currentDefence < context.Status.PlayerData.defence)
            {
                //재생 여부 활성화 / 타임 초기화
                recoverAble = true;
                currentRecoverTime = 0;
            }
        }

        private void RecoverDelay()
        {
            if (recoverAble && !context.Status.isDead)
            {
                if (context.Status.PlayerData.defenceRecoverDelay <= currentRecoverTime)
                {
                    context.Status.currentDefence += Mathf.Clamp(context.Status.PlayerData.defenceRecoverAmountForFrame,0, context.Status.PlayerData.defence);
                    context.Status.IsCrackDefence = false;
                    context.Sync.UpdateDefenceSyncVars();

                    if (context.Status.currentDefence >= context.Status.PlayerData.defence)
                    {
                        //재생 여부 비활성화
                        recoverAble = false;
                    }
                }
                else
                {
                    currentRecoverTime += Time.deltaTime;
                }
            }
        }
        
        #endregion
    }
}