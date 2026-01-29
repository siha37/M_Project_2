using System;
using System.Collections.Generic;
using FishNet;
using FishNet.Object;
using MyFolder._1._Scripts._1._UI._0._GameStage._1._StageUI._1._Map;
using MyFolder._1._Scripts._3._SingleTone;
using MyFolder._1._Scripts._6._GlobalQuest._0._QuestClass;
using MyFolder._1._Scripts._6._GlobalQuest._1._GlobalQuestSpawner;
using MyFolder._1._Scripts._6._GlobalQuest._2._Data;
using MyFolder._1._Scripts._0._Object._1._Spawner;
using MyFolder._1._Scripts._10._Sound;
using MyFolder._1._Scripts._11._Feel;
using MyFolder._1._Scripts._2._View._1._ScreenMark;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace MyFolder._1._Scripts._6._GlobalQuest
{
    public class GlobalQuestManager : NetworkBehaviour
    {        
        public static GlobalQuestManager instance;
        
        [Header("동적 Quest 변수")]
        private List<GlobalQuestBase> globalQuests = new();
        private Dictionary<GlobalQuestBase, GlobalQuestReplicator> questToReplicator = new();
        private Dictionary<GlobalQuestBase, NetworkQuestEnemySpawner> questToEnemySpawner = new();

        
        [Header("스포너")]
        [SerializeField] private NetworkObject defencePrefab;
        [SerializeField] private NetworkObject survivalPrefab;
        [SerializeField] private NetworkObject replicatorPrefab;
        
        
        [Header("위치 정보")]
        [SerializeField] private List<QuestPoint> exterminationPoints = new();
        [SerializeField] private List<QuestPoint> defensePoints = new();
        [SerializeField] private List<QuestPoint> survivalPoints = new();
        
        [Header("프리팹")] 
        [SerializeField] private NetworkObject nqesPrefab;
        
        
        
        
        // QuestData 기반 팩토리
        private static readonly Dictionary<GlobalQuestType, Func<QuestSpawner, QuestData,QuestPoint, GlobalQuestBase>> _questDataMap =
            new()
            {
                { GlobalQuestType.Extermination, (spawner, data, point) => new ExterminationGlobalQuest(spawner, (ExterminationQuestData)data,point) },
                { GlobalQuestType.Defense,       (spawner, data, point) => new DefenceGlobalQuest(spawner, (DefenceQuestData)data,point) },
                { GlobalQuestType.Survival,      (spawner, data, point) => new SurvivalGlobalQuest(spawner, (SurvivalQuestData)data,point) },
            };
        private static readonly Dictionary<GlobalQuestType, Func<QuestSpawner>> _spawner_map =
            new()
            {
                { GlobalQuestType.Extermination, () => new ExterminationGlobalSpawner() },
                { GlobalQuestType.Defense,       () => new DefenseGlobalSpawner() },
                { GlobalQuestType.Survival,      () => new SurvivalGlobalSpawner() },
            };
        
        //초기화 변수
        private int exterminationTargetAmount;
        [SerializeField] private bool questTypeConst =false; 
        
        //퀘스트 데이터 생성 ID
        private ushort DataId = 1;
        
        //퀘스트 관리자 데이터
        private GlobalQuestManagerData ManagerData;
        
        //생성 주기 변수
        [SerializeField] private List<float> QuestCreateTime;
        //동적 주기 변수
        private float currentTime = 0;
        private int lastTimeIndex = 0;
        [SerializeField] private GlobalQuestType lastQuestType = GlobalQuestType.None;
        
        //callback
        public delegate void questDel(GlobalQuestBase quest);
        public questDel OnGlobalQuestCreated;
        public questDel OnGlobalQuestRemoved;
        
        //Getter
        private int globalQuestsCount => globalQuests.Count;
        private int totalQuestCount => 1;

        private void Awake()
        {
            if(!instance)
                instance = this;
        }
        public override void OnStartServer()
        {
            ManagerData = GameDataManager.Instance.GetGlobalQuestManagerData();
            QuestCreateTimesSplits();
            currentTime = 0;
        }
        public override void OnStartClient()
        {
        }

        private void QuestCreateTimesSplits()
        {
            if (ManagerData == null)
            {
                LogManager.LogWarning(LogCategory.Quest,"ManagerData is null",this);
                //데이터 공실 임의 방어
                QuestCreateTime = new List<float>(4){20,120,340};
            }
            else
            {
                string[] time_string = ManagerData.questCreateTime.Split("/");
                for (int i = 0; i < time_string.Length; i++)
                {
                    QuestCreateTime.Add(float.Parse(time_string[i]));
                }
            }
        }
        /// <summary>
        /// QuestData 기반 퀘스트 생성
        /// </summary>
        public GlobalQuestBase Create(GlobalQuestType type)
        {
            if (!_questDataMap.TryGetValue(type, out var ctor))
                throw new ArgumentOutOfRangeException(nameof(type), type, "Unsupported quest type");
            if(!_spawner_map.TryGetValue(type, out var stor))
                throw new ArgumentOutOfRangeException(nameof(type), type, "Unsupported quest type");
                
            
            QuestData data;
            List<QuestPoint> points = new();
            //Data 얻어오기
            switch (type)
            {
                case GlobalQuestType.Extermination:
                    data = GameDataManager.Instance.GetExterminationDataById(DataId);
                    points = exterminationPoints;
                    break;
                case GlobalQuestType.Defense:
                    data = GameDataManager.Instance.GetDefenceDataById(DataId);
                    points = defensePoints;
                    break;
                case GlobalQuestType.Survival:
                    data = GameDataManager.Instance.GetSurvivalDataById(DataId);
                    points = survivalPoints;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, "Unsupported quest type");
            }
            
            if (data == null)
            {
                LogManager.LogError(LogCategory.Quest, $"QuestData를 찾을 수 없습니다: Type={type}, DataId={DataId}", this);
                return null;
            }

            QuestPoint point = null;
            if (points != null && points.Count > 0)
            {
                point = points[Random.Range(0, points.Count)];
            }
            
            //위치 및 프리팹 적용
            QuestSpawner spawner = stor();
            spawner.SetSpawnPoints(point);
            spawner.SetSpawnPrefab(GetQuestPrefab(type));
            
            // QuestData 기반으로 퀘스트 생성
            var quest = ctor(spawner, data, point);
            
            //다음 퀘스트 ID 증가
            DataId++;
            
            LogManager.Log(LogCategory.Quest, $"QuestData 기반 퀘스트 생성: {type} (CardIds: Reward={data.rewardCardId}, Defeat={data.defeatCardId})", this);
            
            return quest;
        }

        private NetworkObject GetQuestPrefab(GlobalQuestType type)
        {
            switch (type)
            {
                case GlobalQuestType.Defense:
                    return defencePrefab;
                case GlobalQuestType.Survival:
                    return survivalPrefab;
                default:
                    Log($"지원되지 않는 퀘스트 타입의 프리팹 요청: {type}");
                    return null;
            }
        }

        private void Update()
        {
            if (!IsServerInitialized)
                return;
            if (globalQuestsCount >= totalQuestCount)
            {
                Log("Global Quest Manager : 현재 활성화된 퀘스트가 충분히 존재합니다");
                for (int i = 0; i < globalQuests.Count; i++ )
                {
                    if(globalQuests.Count <= i)
                        break;
                    
                    GlobalQuestBase quest = globalQuests[i];
                    
                    if(quest == null)
                        break;
                    
                    quest.Update();

                    // 변화 감지 미러링
                    if (questToReplicator.TryGetValue(quest, out var rep))
                        MirrorQuestToReplicatorIfChanged(quest, rep);
                    if (quest.IsEnd)
                        QuestRemove(quest);
                }
                return;
            }

            if (QuestCreateTime.Count > lastTimeIndex)
            {
                if (_8._Time.TimeManager.instance.CurrentTime >= QuestCreateTime[lastTimeIndex])
                {
                    //목표 시간 달성
                    if(!questTypeConst)lastQuestType = (GlobalQuestType)Random.Range(1, (int)GlobalQuestType.GlobalQuestTypeAmount);
                    // QuestData 기반 생성 방식 사용
                    GlobalQuestBase quest = Create(lastQuestType);
                    OnGlobalQuestCreated?.Invoke(quest);
                    globalQuests.Add(quest);

                    string AlertText = "";
                    switch (lastQuestType)
                    {
                        case GlobalQuestType.Defense:
                            AlertText = "방어 퀘스트 준비";
                            break;
                        case GlobalQuestType.Survival:
                            AlertText = "생존 퀘스트 준비";
                            break;
                        case GlobalQuestType.Extermination:
                            AlertText = "섬멸 퀘스트 준비";
                            break;
                    }
                    Feel_InGame.Instance.AlertFeel_Start(AlertText);


                    //스포너 중지 - 변경 해야함 - 스포너는 안 멈추고 무적 활성화, 스포너의 스포너 중지로
                    //SpawnerManager.instance.SpawnerInvincibleOn();

                    //필요 요소 생성
                    CreateReplicatorFor(quest);
                    CreateMarkFor(quest);
                    CreateSoundFor(quest);
                    AddScreenMark(quest);

                    // 생성 직후 다음 생성 주기 리셋
                    lastTimeIndex++;
                }
            }
        }

        
        private void QuestRemove(GlobalQuestBase quest)
        {
            OnGlobalQuestRemoved?.Invoke(quest);
            globalQuests.Remove(quest);
            
            //스포너 10초 뒤 재개
            //SpawnerManager.instance.SpawnerInvincibleOff(quest.QuestData.baseSpawnInvincibleOffTime);
            
            //요소 삭제
            RemoveReplicatorFor(quest);
            RemoveMarkFor(quest);
            RemoveEnemySpawnerFor(quest);
            RemoveSoundFor(quest);
            RemoveScreenMarkFor(quest);
        }

        private void Log(string message, Object obj = null)
        {
            LogManager.Log(LogCategory.Quest, message, obj);
        }

        #region Replicator
        private int _questIdSeed = 1;
        private void CreateReplicatorFor(GlobalQuestBase quest)
        {
            if (!replicatorPrefab)
            {
                Log("replicatorPrefab이 설정되지 않았습니다.");
                return;
            }

            // NetworkObject 컴포넌트 확인
            var prefabNetworkObject = replicatorPrefab.GetComponent<NetworkObject>();
            if (!prefabNetworkObject)
            {
                Log("replicatorPrefab에 NetworkObject 컴포넌트가 없습니다.");
                return;
            }

            // 서버 상태 재확인
            if (!IsServerInitialized)
            {
                Log("서버가 초기화되지 않았습니다.");
                return;
            }

            Log("레플리케이터 생성 시작");
            NetworkObject nob = Instantiate(replicatorPrefab, Vector3.zero, Quaternion.identity);
            
            var rep = nob.GetComponent<GlobalQuestReplicator>();
            if (!rep)
            {
                Log("생성된 오브젝트에 GlobalQuestReplicator 컴포넌트가 없습니다.");
                Destroy(nob.gameObject);
                return;
            }

            try
            {
                Log($"레플리케이터 스폰 시도: {nob.name}");
                InstanceFinder.ServerManager.Spawn(nob);
                Log("레플리케이터 스폰 성공");
                quest.QuestID = _questIdSeed;
                rep.QuestId.Value = _questIdSeed++;
                // 초기 미러링
                MirrorQuestToReplicatorIfChanged(quest, rep);
                
                // 초기 미러링 완료 신호
                rep.ResetComplete.Value = true;
                questToReplicator[quest] = rep;
                
                Log($"레플리케이터 생성 완료: QuestId={rep.QuestId.Value}");

                // 퀘스트 적 스포너 생성
                CreateEnemySpawnerFor(quest, rep.QuestId.Value);
            }
            catch (System.Exception e)
            {
                Log($"레플리케이터 스폰 실패: {e.Message}");
                Destroy(nob.gameObject);
            }
        }
        

        private void RemoveReplicatorFor(GlobalQuestBase quest)
        {
            if (!questToReplicator.TryGetValue(quest, out var rep))
                return;
            if (rep && rep.NetworkObject && rep.NetworkObject.IsSpawned)
                InstanceFinder.ServerManager.Despawn(rep.NetworkObject);
            questToReplicator.Remove(quest);
        }

        // 변화분만 반영하는 미러링
        private void MirrorQuestToReplicatorIfChanged(GlobalQuestBase quest, GlobalQuestReplicator rep)
        {
            if (rep.QuestType.Value != quest.Type)                                  rep.QuestType.Value = quest.Type;
            if (rep.QuestName.Value != quest.QuestName)                             rep.QuestName.Value = quest.QuestName;
            if (!Mathf.Approximately(rep.WaitingTime.Value, quest.waitingTime))     rep.WaitingTime.Value = quest.waitingTime;
            if (!Mathf.Approximately(rep.Progress.Value, quest.progress))           rep.Progress.Value = quest.progress;
            if (!Mathf.Approximately(rep.Target.Value, quest.target))               rep.Target.Value = quest.target;
            if (!Mathf.Approximately(rep.LimitTime.Value, quest.limitTime))         rep.LimitTime.Value = quest.limitTime;
            if (!Mathf.Approximately(rep.ElapsedTime.Value, quest.currentTime))     rep.ElapsedTime.Value = quest.currentTime;
            if (rep.Size.Value != quest.Point.Size)                                 rep.Size.Value = quest.Point.Size;
            if (rep.Position.Value != quest.Point.Point)                            rep.Position.Value = quest.Point.Point;
            if (rep.IsActive.Value != quest.IsActive)                               rep.IsActive.Value = quest.IsActive;
            if (rep.IsEnd.Value != quest.IsEnd)                                     rep.IsEnd.Value = quest.IsEnd;

            if (quest is SurvivalGlobalQuest survivalQuest)
            {
                if (!Mathf.Approximately(rep.MinusProgress.Value, survivalQuest.minusProgress))     rep.MinusProgress.Value = survivalQuest.minusProgress;
                if (!Mathf.Approximately(rep.MinusTiming.Value, survivalQuest.minusTiming))     rep.MinusTiming.Value = survivalQuest.minusTiming;
                if (!Mathf.Approximately(rep.MinusMutiple.Value, survivalQuest.minusMutiple))     rep.MinusMutiple.Value = survivalQuest.minusMutiple;
            }
        }
        #endregion

        #region Mark

        private void CreateMarkFor(GlobalQuestBase quest)
        {
            MapMarkType type = MapMarkType.Count;
            Color color = Color.white;
            type = MapMarkType.Area;
            switch (quest)
            {
                case ExterminationGlobalQuest:
                    color = Color.red;
                    break;
                case SurvivalGlobalQuest:
                    color = Color.cyan;
                    break;
                case DefenceGlobalQuest:
                    color = Color.blue;
                    break;
            }

            MapMarkContext context = new MapMarkContext(type,MarkType.QUEST, quest.Point.Point, color, quest.Point.Size);
            
            quest.MarkId = MapMarkManager.instance.Register(context);
        }

        private void RemoveMarkFor(GlobalQuestBase quest)
        {
            MapMarkManager.instance?.Unregister(quest.MarkId);
        }

        #endregion

        #region EnemySpawner
        private void CreateEnemySpawnerFor(GlobalQuestBase quest, int questId)
        {
            // 서버 상태 확인
            if (!IsServerInitialized) return;

            // 이미 존재하면 제거 후 재생성 방지
            if (questToEnemySpawner.ContainsKey(quest))
                return;

            // GameObject 생성 및 NetworkObject 부착
            NetworkObject nob = Instantiate(nqesPrefab);
            
            nob.transform.position = quest.Point.Point;
            nob.TryGetComponent(out NetworkQuestEnemySpawner spawner);

            // 초기화 호출
            spawner.Initialize(questId,quest.Type, quest.Point,quest.QuestData);

            // 네트워크 스폰
            try
            {
                InstanceFinder.ServerManager.Spawn(nob);
                questToEnemySpawner[quest] = spawner;
            }
            catch (Exception e)
            {
                Log($"적 스포너 스폰 실패: {e.Message}");
                Destroy(nob);
            }
        }

        private void RemoveEnemySpawnerFor(GlobalQuestBase quest)
        {
            // 적 스포너 제거
            if (questToEnemySpawner.TryGetValue(quest, out var sp))
            {
                sp.StopSpanwer();
                if (sp && sp.NetworkObject && sp.NetworkObject.IsSpawned)
                    InstanceFinder.ServerManager.Despawn(sp.NetworkObject);
                questToEnemySpawner.Remove(quest);
            }
        }

        public NetworkQuestEnemySpawner GetEnemySpawner(GlobalQuestBase quest)
        {
            return questToEnemySpawner[quest];
        }

        #endregion

        #region ScreenMark

        public void AddScreenMark(GlobalQuestBase quest)
        {
            NetworkObject point = quest.Point.GetComponent<NetworkObject>();
            // 화면 마커 등록
            switch (quest.Type)
            {
                case GlobalQuestType.Extermination:
                    ScreenMarkManager.Instance.AddTarget(point,TrackedObject.TrackedObjectType.Extermination);
                    break;
                case GlobalQuestType.Defense:
                    ScreenMarkManager.Instance.AddTarget(point,TrackedObject.TrackedObjectType.Defense);
                    break;
                case GlobalQuestType.Survival:
                    ScreenMarkManager.Instance.AddTarget(point,TrackedObject.TrackedObjectType.Survival);
                    break;
            }
        }

        private void RemoveScreenMarkFor(GlobalQuestBase quest)
        {
            NetworkObject point = quest.Point.GetComponent<NetworkObject>();
            // 화면 마커 등록
            switch (quest.Type)
            {
                case GlobalQuestType.Extermination:
                    ScreenMarkManager.Instance.RemoveTarget(point);
                    break;
                case GlobalQuestType.Defense:
                    ScreenMarkManager.Instance.RemoveTarget(point);
                    break;
                case GlobalQuestType.Survival:
                    ScreenMarkManager.Instance.RemoveTarget(point);
                    break;
            }
        }

        #endregion

        #region Sound

        private void CreateSoundFor(GlobalQuestBase quest)
        {
            GameSystemSound.Instance.Quest_CreateSFX();
        }

        private void RemoveSoundFor(GlobalQuestBase quest)
        {
            if (quest.IsComplete)
            {
                GameSystemSound.Instance.Quest_SuccessSFX();
            }
            else
            {
                GameSystemSound.Instance.Quest_FailSFX();
            }
        }
        
        #endregion
    }
}