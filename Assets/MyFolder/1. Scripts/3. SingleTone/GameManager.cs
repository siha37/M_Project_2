using System.Collections;
using System.Collections.Generic;
using FishNet;
using FishNet.Connection;
using FishNet.Managing.Scened;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using MyFolder._1._Scripts._1._UI._0._GameStage._1._StageUI;
using MyFolder._1._Scripts._11._Feel;
using MyFolder._1._Scripts._3._SingleTone.GameSetting;
using MyFolder._1._Scripts._7._PlayerRole;
using MyFolder._1._Scripts._8._Time;
using MyFolder._1._Scripts._8999._Utility.Corutin;
using UnityEngine;

namespace MyFolder._1._Scripts._3._SingleTone
{
    public class GameManager : NetworkBehaviour
    {
        private static GameManager instance;
        public static GameManager Instance
        {
            get
            {
                if (!instance)
                {
                    instance = FindFirstObjectByType<GameManager>();

                    if (!instance)
                    {
                        GameObject obj = new GameObject();
                        obj.name = nameof(GameManager);
                        instance = obj.AddComponent<GameManager>();
                    }
                }
                return instance;
            }
        }

        private void Awake()
        {
            if (!instance)
            {
                instance = this;
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        // 매니저 참조
        private NetworkPlayerManager networkPlayerManager;
        private TimeManager timeManager;
        private PlayerSettingManager playerSettingManager;
        private SpawnerManager spawnerManager;
        [SerializeField] GameStageUI gameStageUI;
        
        // 게임 종료 관련
        private bool isGameEnded = false;
        private bool waitFirst = false;
        private bool sean2lock = false;
        private string targetScene;
        
        // 시간 연산
        private const float rimitTime = 30;
        private float rimitCounter = 0;
        
        //역할자 저장
        List<NetworkObject> destroyer = null;
        
        private readonly SyncVar<string> enddiscription = new SyncVar<string>();
        
        
        // 승리 결과 타입
        public enum GameResult
        {
            None,
            NormalWin,
            DestroyerWin
        }
        public override void OnStartClient()
        {
            enddiscription.OnChange += OnChangeEndingDiscription;
        }
        
        private  void Start()
        {
            networkPlayerManager = NetworkPlayerManager.Instance;
            timeManager = _8._Time.TimeManager.instance;
            playerSettingManager = PlayerSettingManager.Instance;
            spawnerManager = SpawnerManager.instance;
            StartCoroutine(nameof(waitForPlayers));
        }

        IEnumerator waitForPlayers()
        {
            yield return WaitForSecondsCache.Get(30);
            waitFirst = true;
        }

        private void Update()
        {
            // ✅ 서버에서만 게임 종료 조건 체크
            if (!IsServerInitialized) return;
            
            CheckGameEndConditions();
        }

        /// <summary>
        /// 게임 종료 조건 체크
        /// </summary>
        private void CheckGameEndConditions()
        {
            if (isGameEnded) return;
            if (!waitFirst) return;
            if (!networkPlayerManager || !timeManager || !playerSettingManager)
                return;
            
            List<NetworkObject> alivePlayers = networkPlayerManager.GetAlivePlayers();
            
            // 조건 1: 시간이 종료되고 Normal 플레이어가 한 명이라도 살아있으면 Normal 승리
            if (timeManager.IsEnd)
            {
                if (HasAliveNormalPlayer(alivePlayers))
                {
                    EndGame(GameResult.NormalWin,"시민이 살아남았습니다.");
                    return;
                }
            }
            
            // 조건 2: 제거자 전체 사망 중 1분 소요 후 Normal 승리
            if (!timeManager.IsEnd)
            {
                if (!HasAliveDestroyer(alivePlayers))
                {
                    if (rimitCounter >= rimitTime)
                    {
                        EndGame(GameResult.NormalWin,"제거자는 싸늘한 시체가 되었습니다.");
                        return;
                    }
                    else
                    {
                        rimitCounter += Time.deltaTime;    
                    }
                    if(!sean2lock)
                    {
                        sean2lock = true;
                        if(destroyer == null)
                            destroyer = networkPlayerManager.GetDestroyerPlayers();
                        destroyer?.ForEach(e=> On_ShowDestroyerLimit(e.Owner, rimitTime-rimitCounter));
                    }
                    else
                    {
                        destroyer?.ForEach(e=> UpdateDestroyerLimit(e.Owner, rimitTime-rimitCounter));
                    }
                }
                else if(sean2lock)
                {
                    sean2lock = false;
                    destroyer?.ForEach(e=> Off_ShowDestroyerLimit(e.Owner));
                }
            }
            
            // 조건 3: 시간 종료 전에 살아남은 플레이어 중 전부 Destroyer면 Destroyer 승리
            if (!timeManager.IsEnd && playerSettingManager.GetPlayerCount() != 1)
            {
                if (IsPlayerDestroyer(alivePlayers))
                {
                    EndGame(GameResult.DestroyerWin,"시민이 전멸하였습니다.");
                    return;
                }
            }

            // 조건 4: 스포너를 전체 파괴 시 Normal 승리
            if (!timeManager.IsEnd)
            {
                if (spawnerManager.SpawnerCount == 0)
                {
                    EndGame(GameResult.NormalWin,"유령? 그게 어디있죠? 없어요!");
                    return;
                }
            }
            
            
            // 조건 5: 전부 사망
            if (!timeManager.IsEnd)
            {
                if (alivePlayers.Count == 0)
                {
                    EndGame(GameResult.DestroyerWin,"마을이 휑합니다. 확실히 시민들은 죽었군요.");
                    return;
                }
            }
            
        }

        /// <summary>
        /// Normal 역할의 살아있는 플레이어가 있는지 확인
        /// </summary>
        private bool HasAliveNormalPlayer(List<NetworkObject> alivePlayers)
        {
            foreach (var player in alivePlayers)
            {
                if (!player) continue;
                
                // 플레이어의 ClientId 가져오기
                int clientId = player.Owner?.ClientId ?? -1;
                if (clientId == -1) continue;
                
                // 플레이어 설정 가져오기
                var settings = playerSettingManager.GetPlayerSettings(clientId);
                if (settings is { role: PlayerRoleType.Normal })
                {
                    return true;
                }
            }
            
            return false;
        }

        /// <summary>
        /// 플레이어가 Destroyer 역할인지 확인
        /// </summary>
        private bool IsPlayerDestroyer(List<NetworkObject> player)
        {
            if (player == null) return false;
            bool result = true;
            // 플레이어의 ClientId 가져오기
            foreach (NetworkObject networkObject in player)
            {
                int clientId = networkObject.Owner?.ClientId ?? -1;
                if (clientId == -1)
                {
                    result= false;
                    continue;
                }
            
                // 플레이어 설정 가져오기
                var settings = playerSettingManager.GetPlayerSettings(clientId);
                
                // 시민이 있다면 false
                if(settings is { role: PlayerRoleType.Normal })
                    result = false;
                // 시민이 한번도 없다면 true
            }
            return result;
        }

        

        private bool HasAliveDestroyer(List<NetworkObject> alivePlayers)
        {
            foreach (var player in alivePlayers)
            {
                if (!player) continue;
                
                // 플레이어의 ClientId 가져오기
                int clientId = player.Owner?.ClientId ?? -1;
                if (clientId == -1) continue;
                
                // 플레이어 설정 가져오기
                var settings = playerSettingManager.GetPlayerSettings(clientId);
                if (settings != null && settings.role == PlayerRoleType.Destroyer)
                {
                    return true;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// 게임 종료 처리
        /// </summary>
        private void EndGame(GameResult result,string discription)
        {
            if (isGameEnded) return;
            
  
            isGameEnded = true;
            
            GameDataManager.Instance.ResetGameData();
            
            // 결과에 따라 씬 전환
            targetScene = result switch
            {
                GameResult.NormalWin => "NormalWin",
                GameResult.DestroyerWin => "DestroyerWin",
                _ => null
            };
            
            enddiscription.Value = discription;
        }

        private void OnChangeEndingDiscription(string old, string newData,bool isServer)
        {
            GameSettingManager.Instance.EndDescription = newData;
            if (IsServerInitialized)
            {
                SceneLoadData sceneData = new SceneLoadData(new List<string> {targetScene})
                {
                    ReplaceScenes = ReplaceOption.All
                };
                NetworkManager.SceneManager.LoadGlobalScenes(sceneData);
            }
        }
        /// <summary>
        /// 씬 전환 전 네트워크 오브젝트 정리
        /// </summary>
        private void CleanupNetworkObjectsBeforeSceneTransition()
        {
            if (!IsServerInitialized) return;
            
            // 2. 모든 적 오브젝트 디스폰
            var networkManager = InstanceFinder.NetworkManager;
            if (networkManager && networkManager.ServerManager)
            {
                // EnemyContainer의 모든 자식 오브젝트 디스폰
                var enemyContainer = GameObject.FindGameObjectWithTag("EnemyContainer");
                if (enemyContainer)
                {
                    var enemyNetworkObjects = new List<NetworkObject>();
                    foreach (Transform child in enemyContainer.transform)
                    {
                        if (child && child.TryGetComponent<NetworkObject>(out var nob) && nob.IsSpawned)
                        {
                            enemyNetworkObjects.Add(nob);
                        }
                    }
            
                    foreach (var nob in enemyNetworkObjects)
                    {
                        if (nob && nob.IsSpawned)
                        {
                            networkManager.ServerManager.Despawn(nob);
                        }
                    }
                }
            }
    
            // 3. NetworkTransform 비활성화 (Transform 에러 방지)
            var networkTransforms = FindObjectsByType<FishNet.Component.Transforming.NetworkTransform>(FindObjectsSortMode.None);
            foreach (var nt in networkTransforms)
            {
                if (nt && nt.enabled)
                {
                    nt.enabled = false;
                }
            }
        }
        [TargetRpc]
        private void UpdateDestroyerLimit(NetworkConnection target,float limitTime)
        {
            gameStageUI?.DestroyerTimeLimitUpdate(limitTime);
        }

        [TargetRpc]
        private void On_ShowDestroyerLimit(NetworkConnection target,float limitTime)
        {
            Feel_InGame.Instance.On_DestroyerEndLimitUIFeel_Start();
            gameStageUI?.DestroyerTimeLimitUpdate(limitTime);
        }

        [TargetRpc]
        private void Off_ShowDestroyerLimit(NetworkConnection target)
        {
            Feel_InGame.Instance.Off_DestroyerEndLimitUIFeel_Start();
        }
    }
}
