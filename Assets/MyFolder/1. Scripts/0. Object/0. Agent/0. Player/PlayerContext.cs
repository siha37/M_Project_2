using MyFolder._1._Scripts._0._Object._0._Agent._0._Player._0._Component;
using MyFolder._1._Scripts._0._Object._0._Agent._0._Player.Data;
using MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.Data;
using MyFolder._1._Scripts._1._UI._0._GameStage._0._Agent;
using MyFolder._1._Scripts._10._Sound;
using Spine.Unity;
using UnityEngine;
using UnityEngine.VFX;

namespace MyFolder._1._Scripts._0._Object._0._Agent._0._Player
{
    public class PlayerContext : MonoBehaviour
    {
        //COMPONENT
        [SerializeField] private PlayerControll controller;
        [SerializeField] private PlayerInputControll input;
        [SerializeField] private PlayerInteractController playerInteract;
        [SerializeField] private PlayerStatus status;
        [SerializeField] private PlayerNetworkSync sync;
        [SerializeField] private PlayerComponentManager component;
        [SerializeField] private Shooter shooter;
        [SerializeField] private PlayerUI agentUI;
        [SerializeField] private SpriteRenderer sprite;
        [SerializeField] private SkeletonAnimation skeleton_Anim;
        [SerializeField] private PlayerSfx sfx;

        
        // OBJECT
        [SerializeField] private Transform defencePivot;
        [SerializeField] private Transform defenceBall;
        [SerializeField] private Camera mainCamera;
        [SerializeField] private Transform shotPivot;
        [SerializeField] private Transform shotPoint;
        [SerializeField] private Animator smokeAnimator;


        // 애니메이션 세트 (ScriptableObject)
        [Header("애니메이션 세트")]
        [Tooltip("플레이어 애니메이션 세트")]
        [SerializeField] private PlayerAnimationSet playerAnimationSet;
        
        [Tooltip("위장 시 사용할 적군 애니메이션 세트")]
        [SerializeField] private EnemyAnimationSet enemyAnimationSet;

        [SerializeField] private Sprite playerSprite;
        [SerializeField] private Sprite enemySprite;
        

        [Header("FogofWar 시각 오브젝트 (임시)")] 
        public GameObject Canvas;
        public SpriteRenderer ShieldSprite;
        public GameObject ShieldEffect;
        public SpriteRenderer CharacterSprite;
        public SpriteRenderer ShotPointSprite;
        public GameObject DefenceBarUI;
        public GameObject PlayerNameUI;
        public Canvas uiCanvas;
        public MeshRenderer SkeletonMesh;

        [Header("VFX")]
        public VisualEffect HealVfx;
        

        //Set
        public Camera SetCamera { set => mainCamera = value; }

        //Get
        public PlayerControll Controller                =>controller;
        public PlayerInputControll Input                =>input;
        public PlayerInteractController PlayerInteract  =>playerInteract;
        public PlayerStatus Status                      =>status;
        public PlayerNetworkSync Sync                   =>sync;
        public PlayerComponentManager Component         =>component;
        public Shooter Shooter                          =>shooter;
        public PlayerUI AgentUI                          =>agentUI;
        public SpriteRenderer Sprite                     =>sprite;
        public SkeletonAnimation Skeleton                    =>skeleton_Anim;
        public PlayerSfx Sfx                             =>sfx;
        
        public Transform DefencePivot                   => defencePivot;
        public Transform DefenceBall                    => defenceBall;
        public Camera MainCamera                        => mainCamera;
        public Transform ShotPivot                        => shotPivot;
        public Transform ShotPoint                        => shotPoint;
        public Animator SmokeAnimator => smokeAnimator;
        
        // 애니메이션 세트 접근자
        public PlayerAnimationSet PlayerAnimationSet    => playerAnimationSet;
        public EnemyAnimationSet EnemyAnimationSet      => enemyAnimationSet;
        public Sprite PlayerSprite      => playerSprite;
        public Sprite EnemySprite      => enemySprite;
    }
}