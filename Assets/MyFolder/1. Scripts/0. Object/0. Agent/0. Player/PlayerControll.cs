using System;
using System.Collections;
using FishNet;
using FishNet.Component.Transforming;
using FishNet.Object;
using MyFolder._1._Scripts._0._Object._0._Agent._0._Player._0._Component;
using MyFolder._1._Scripts._3._SingleTone;
using MyFolder._1._Scripts._9._Vivox;
using UnityEngine;

namespace MyFolder._1._Scripts._0._Object._0._Agent._0._Player
{
    public class PlayerControll : NetworkBehaviour
    {
        [SerializeField] PlayerContext context;
        
        Transform tf;
        [SerializeField] Rigidbody2D rd2D;

        public Action<Vector2> OnMoveCallback;
        public Action OffMoveCallback;
        
        // 현재 임시 기입 - 이동 관련
        private Vector2 prevDirection;
        private bool onMove = false;
        private bool isMovable = true;
        public bool IsMovable {set{isMovable=value;}}
        
        // ✅ 위장 시스템용 public 속성
        public bool IsMoving => onMove;
        public Vector2 MoveDirection => prevDirection;
        private float Currentyspeed;
        public override void OnStartClient()
        {
            tf = transform;

            if(!IsOwner){
                context.Input.enabled = false;
                context.Shooter.enabled = false;
                context.PlayerInteract.enabled = false;
                // ✅ AgentUI는 NetworkSync에서 관리하므로 여기서 제어하지 않음
            }
            else
            {
                //시각화 초기화
                if(context.Canvas)
                    if(context.Canvas.TryGetComponent(out Canvas canvas))
                        canvas.enabled = true;
                if(context.ShieldEffect)
                    context.ShieldEffect?.SetActive(true);
                if(context.ShieldSprite)
                    context.ShieldSprite.enabled = true;
                if(context.CharacterSprite)
                    context.CharacterSprite.enabled = true;
                if(context.SkeletonMesh)
                    context.SkeletonMesh.enabled = true;
                if(context.ShieldSprite)
                    context.ShieldSprite.enabled = true;
                
                context.Input.movePerformedCallback += Move;
                context.Input.moveStopCallback += MoveStop;
                context.Input.attackCallback += AttackMoveSpeed;
                context.Input.attackCancelCallback += AttackMoveSpeed;
                
                // 3D Object 연결
                VivoxManager.Instance.SetParticipantObejct(gameObject);
            }
        }

        private void FixedUpdate()
        {
            if(context && context.Status.DataLoaded)
                Currentyspeed = context.Input.IsAttacking && !context.Shooter.IsReloading ? context.Status.PlayerData.attackSpeed : context.Status.PlayerData.speed;
            if(rd2D)
                rd2D.linearVelocity = prevDirection * Currentyspeed;
        }


        void Move(Vector2 direction)
        {
            if(isMovable)
            {
                Currentyspeed = context.Input.IsAttacking && !context.Shooter.IsReloading ? context.Status.PlayerData.attackSpeed : context.Status.PlayerData.speed;
                onMove = true;
                
                Physics2D.queriesHitTriggers = true;  // Trigger Collider도 감지하도록 설정
                RaycastHit2D hit = Physics2D.Raycast(transform.position - new Vector3(0,-2.3f,0), Vector2.down,0.5f, LayerMask.GetMask("Ground"));
                float type =0;
                if (hit)
                {
                    if (hit.collider.CompareTag("GroundDrit"))
                        type = 0;
                    else if (hit.collider.CompareTag("GroundStone"))
                        type = 1;
                }
                context.Sfx.SetWalking(true,type);
                prevDirection = direction;
                rd2D.linearVelocity = new Vector2(direction.x, direction.y) * Currentyspeed;
                
                if(IsServerInitialized)
                    MoveObserver(direction);
                else
                    MoveServerRpc(direction);
            }
        }

        [ServerRpc]
        void MoveServerRpc(Vector2 direction)
        {
            MoveObserver(direction);
        }

        [ObserversRpc]
        void MoveObserver(Vector2 direction)
        {
            OnMoveCallback?.Invoke(direction);
        }
        
        /// <summary>
        /// 단순 입력을 기준으로 하여 속도를 재조정함 / 재장전 및 연사가 아닌경우는 제외해야함
        /// </summary>
        void AttackMoveSpeed()
        {
            if (onMove)
            {
                float speed = !context.Input.IsAttacking
                    ? context.Status.PlayerData.speed
                    : context.Status.PlayerData.attackSpeed;
                rd2D.linearVelocity = new Vector2(prevDirection.x, prevDirection.y) * speed;
            }
        }
    
        public void MoveStop()
        {
            if(!rd2D || !rd2D) return;
            
            onMove = false;
            context.Sfx.SetWalking(false);
            rd2D.linearVelocity = Vector2.zero;
            prevDirection = Vector2.zero;
            if(IsServerInitialized)
                MoveStopObserver();
            else
                MoveStopServerRpc();
        }

        [ServerRpc(RequireOwnership = false)]
        void MoveStopServerRpc()
        {
            MoveStopObserver();
        }
        
        [ObserversRpc]
        void MoveStopObserver()
        {
            OffMoveCallback?.Invoke();
        }

        // 콜백 해제를 위한 메서드 추가
        public override void OnStopClient()
        {
            if (context.Input && IsOwner)
            {
                context.Input.movePerformedCallback -= Move;
                context.Input.moveStopCallback -= MoveStop;
                context.Input.attackCallback -= AttackMoveSpeed;
                context.Input.attackCancelCallback -= AttackMoveSpeed;
            }
        }

        private void OnDisable()
        {
            MoveStop();
        }

        private void OnDestroy()
        {
            try
            {
                // ✅ 안전한 콜백 해제
                if (context && context.Input && IsOwner)
                {
                    context.Input.movePerformedCallback -= Move;
                    context.Input.moveStopCallback -= MoveStop;
                    context.Input.attackCallback -= AttackMoveSpeed;
                    context.Input.attackCancelCallback -= AttackMoveSpeed;
                }

                TryGetComponent(out NetworkTransform transform);
                transform.OnStopNetwork();
                Owner.SetFirstObject(null);
                InstanceFinder.ServerManager.Despawn(gameObject);
            }
            catch (Exception ex)
            {
                LogManager.LogWarning(LogCategory.Player, $"PlayerControll OnDestroy 오류: {ex.Message}", this);
            }
        }

    }
}
