using System;
using System.Collections;
using FishNet.Object;
using MyFolder._1._Scripts._3._SingleTone;
using MyFolder._1._Scripts._8999._Utility.Corutin;
using Spine.Unity;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using Random = UnityEngine.Random;

namespace MyFolder._1._Scripts._0._Object._0._Agent._0._Player._0._Component
{
    public class Shooter : NetworkBehaviour
    {
        [SerializeField] private PlayerContext context;
        
        private float lookAngle;
        private float targetLookAngle;
        private float currentLookAngle;
        private bool canShoot = true;


        private bool isReloading = false;
        private bool onAttack = true;
        [Header("SplineParameter")]
        [SpineBone]
        public string boneName;
        Spine.Bone bone;
        readonly float upInMinAngle =45, upInMaxAngle=135,upOutMinAngle=40, upOutMaxAngle=140;
        readonly float downInMinAngle = -135, downInMaxAngle = -45,downOutMinAngle=-140, downOutMaxAngle = -40;
        private readonly float leftMinAngle = -90, leftMaxAngle = 90;
        private Vector3 spineBonePosition;
        private Vector3 spineBoneFollowPosition;
        private bool BoneOutPosition = false;
        private PlayerSkeletonAnimationComponent animationComponent;
        public void Start()
        {
            if(context.Skeleton && !string.IsNullOrEmpty(boneName))
                bone = context.Skeleton.skeleton.FindBone(boneName);
        }

        public bool OnAttack
        {
            get { return onAttack; }
            set { onAttack = value; }
        }
        public bool IsReloading => isReloading;

        public float LookAngle => lookAngle;
        //네트워크 동기화 딜레이
        private float lastNetworkSyncTime = 0f;

        
        [Header("Look Settings")]
        [SerializeField] private float gamepadDeadzone = 0.15f;
        [SerializeField] private float lookSensitivity = 1f;
        [SerializeField] private float lookSmoothing = 10f;
        [SerializeField] private bool enableLookSmoothing = false;
        
        public override void OnStartClient()
        {
            if(IsOwner)
            {
                context.Input.lookPerformedCallback += Look;
                context.Input.attackStartCallback += FirstAttackTrigger;
                context.Input.attackCallback += AttackTrigger;
                context.Input.reloadCallback += ReloadTrigger;
                InitializeCamera();
            }
        }
        // 미구현 스탯 - 관통(burstCount) /  탄 사이즈
        private void InitializeCamera()
        {
            if (!context.MainCamera)
            {
                context.SetCamera = Camera.main;
                if (!context.MainCamera)
                {
                    context.SetCamera = FindFirstObjectByType<Camera>();
                }
                if (!context.MainCamera)
                {
                    LogManager.LogError(LogCategory.Player, $"{gameObject.name} 카메라를 찾을 수 없습니다!", this);
                }
            }
        }

        private void Update()
        {
            BoneUpdate();
        }

        public void BoneUpdate()
        {
            if(bone != null)
            {
                spineBoneFollowPosition = Vector3.Lerp(spineBoneFollowPosition,spineBonePosition + transform.position,Time.deltaTime * lookSmoothing);
                Vector3 skeletonSpacePoint = context.Skeleton.transform.InverseTransformPoint(spineBoneFollowPosition);
                skeletonSpacePoint.x *= context.Skeleton.Skeleton.ScaleX;
                skeletonSpacePoint.y *= context.Skeleton.Skeleton.ScaleY;
                bone.SetLocalPosition(skeletonSpacePoint);
            }
        }
        void Look(Vector2 position)
        {
            if (!context.MainCamera)
            {
                context.SetCamera = Camera.main;
                return;
            }
            if (!IsOwner) return;
        
            Vector2 targetVector = Vector2.zero;
        
            switch (context.Input.controllerType)
            {
                case PlayerInputControll.ControllerType.Keyboard:
                    // 개선된 마우스 위치 계산
                    Vector2 worldMousePos = GetWorldMousePosition(position);
                    targetVector = worldMousePos - (Vector2)transform.position;

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

            UpdateLookAngle(targetVector);
            // 본 Aim 위치 변경
            bonePositionUpdate();
            // 로컬에서 즉시 반영 (입력 지연 최소화)
            context.ShotPivot.rotation = Quaternion.Euler(new Vector3(0, 0, lookAngle));
        
            // 네트워크 동기화는 덜 자주 (성능 최적화)
            if (Time.time - lastNetworkSyncTime > 0.05f) // 20fps로 동기화
            {
                if (context.Sync)
                {
                    context.Sync.RequestUpdateLookAngle(lookAngle);
                }
                lastNetworkSyncTime = Time.time;
            }
        }

        private void bonePositionUpdate()
        {
            if(bone == null && context.Skeleton && !string.IsNullOrEmpty(boneName))
                bone = context.Skeleton.skeleton.FindBone(boneName);
            if (bone != null)
            {
                if(animationComponent == null)
                    animationComponent = context.Component.GetPComponent<PlayerSkeletonAnimationComponent>() as PlayerSkeletonAnimationComponent;
                
                // 우 설정
                if(leftMinAngle < lookAngle && leftMaxAngle > lookAngle)
                {
                    if (animationComponent != null) animationComponent.SetAimDirection = 1;
                    context.Skeleton.Skeleton.ScaleX = -1;
                    if (BoneOutPosition && spineBonePosition.x < 0) 
                    {
                        spineBonePosition = new Vector3(spineBonePosition.x * -1,spineBonePosition.y,0);
                    }
                }
                // 좌 설정
                else
                {
                    if (animationComponent != null) animationComponent.SetAimDirection = 0;
                    context.Skeleton.Skeleton.ScaleX = 1;
                    if (BoneOutPosition && spineBonePosition.x > 0)
                    {
                        spineBonePosition = new Vector3(spineBonePosition.x * -1,spineBonePosition.y,0);
                    }
                }   
                
                // 위를 바라봄
                if (upInMinAngle <= lookAngle && upInMaxAngle >= lookAngle)
                {
                    if (animationComponent != null) animationComponent.UpdownCursor = 1;
                    BoneOutPosition = true;
                }
                // 아래를 바라봄
                else if (downInMinAngle <= lookAngle && downInMaxAngle >= lookAngle)
                {
                    BoneOutPosition = true;
                }
                
                if(!BoneOutPosition)
                {
                    // 각도(degree)를 방향 벡터로 변환
                    float angleInDegrees = LookAngle; // 예: 45, -90, 180 등
                    float angleInRadians = angleInDegrees * Mathf.Deg2Rad;

                    Vector2 direction = new Vector2(
                        Mathf.Cos(angleInRadians),
                        Mathf.Sin(angleInRadians)
                    );
                    spineBonePosition = direction * 8;
                }
                else
                {
                    if ((upOutMinAngle >= lookAngle || upOutMaxAngle <= lookAngle) &&
                        (downOutMinAngle >= lookAngle || downOutMaxAngle <= lookAngle))
                    {
                        BoneOutPosition = false;
                        
                        //위를 보고있었는지 확인
                        if (animationComponent?.UpdownCursor == 1)
                        {
                            //위였다면 아래로 전환
                            animationComponent.UpdownCursor = 0;
                        }
                    }
                }
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

        public void ClientUpdateLookAngle(float lookAngle)
        {
            this.lookAngle = lookAngle;
            
            if (context.ShotPivot)
            {
                context.ShotPivot.rotation = Quaternion.Euler(0, 0, lookAngle);
            }
            
            // bone 업데이트는 항상 실행 (네트워크 동기화용)
            bonePositionUpdate();
            BoneUpdate();
        }

        // 발사 가능 상태 확인 후 실행 ( 단발 )
        private void FirstAttackTrigger()
        {
            if(!onAttack || !canShoot || isReloading || context.Status.GetShootingData.fullAuto) return;

            // 다중 탄일 시
            if (context.Status.GetShootingData.burstCount > 1)
            {
                for (int i = 0; i < context.Status.GetShootingData.burstCount; i++)
                {
                    OnAttacking();
                }
                StartCoroutine(ShootDelay());
            }
            else //단일 탄
            {
                if(OnAttacking())
                    StartCoroutine(ShootDelay());
            }
        }
        
        // 발사 가능 상태 확인 후 실행 ( 연발 )
        private void AttackTrigger()
        {
            if (!onAttack || !canShoot || isReloading || !context.Status.GetShootingData.fullAuto) return;
            
            // 다중 탄일 시
            if (context.Status.GetShootingData.burstCount > 1)
            {
                for (int i = 0; i < context.Status.GetShootingData.burstCount; i++)
                {
                    if (!OnAttacking())
                        break;
                }
                StartCoroutine(ShootDelay());
            }
            else
            {
                if(OnAttacking())
                    StartCoroutine(ShootDelay());
            }
        }

        
        // 총알 발사 함수 호출
        private bool OnAttacking()
        {
            if(!IsOwner) return false;
            
            if (context.Status.bulletCurrentCount <= 0)
            {
                ReloadTrigger();
                return false;
            }
            // 네트워크 동기화된 발사 처리
            if (context.Sync)
            {
                // 서버에 발사 요청 (네트워크 동기화)
                context.Sync.RequestShoot(ShotAngleRange(lookAngle), context.ShotPoint.position);
                context.AgentUI.ShotCursor();
                return true;
            }
            return false;
        }
        private IEnumerator ShootDelay()
        {
            canShoot = false;
            yield return WaitForSecondsCache.Get(context.Status.GetShootingData.shotDelay);
            canShoot = true;
        }

        // ✅ 로컬 Reload 메서드 제거 - NetworkSync에서 처리
        private void ReloadTrigger()
        {
            if (!isReloading && context.Status.bulletCurrentCount < context.Status.GetShootingData.magazineCapacity)
            {
                // ✅ 네트워크 동기화만 사용 (폴백 방식 제거)
                if (context.Sync)
                {
                    isReloading = true;
                    context.Sync.RequestReload();
                }
            }
        }

        public void SetReloadingstate(bool isReloading)
        {
            this.isReloading = isReloading;
        }
        
    
        // 콜백 해제를 위한 메서드 추가
        public override void OnStopClient()
        {
            if (IsOwner)
            {
                context.Input.lookPerformedCallback -= Look;
                context.Input.attackStartCallback -= FirstAttackTrigger;
                context.Input.attackCallback -= AttackTrigger;
                context.Input.reloadCallback -= ReloadTrigger;
            }
        }
        private void OnDestroy()
        {
            // 추가 안전 장치로 OnDestroy에서도 콜백 해제
            if (IsOwner)
            {
                context.Input.lookPerformedCallback -= Look;
                context.Input.attackStartCallback -= FirstAttackTrigger;
                context.Input.attackCallback -= AttackTrigger;
                context.Input.reloadCallback -= ReloadTrigger;
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

        private float ShotAngleRange(float angle)
        {
            float aimPrecision = context.Status.GetShootingData.shotAngle;
            float aimError = Random.Range(-aimPrecision, aimPrecision);
            return angle+aimError;
        }
    }
}