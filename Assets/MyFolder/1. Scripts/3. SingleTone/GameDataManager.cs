using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using MyFolder._1._Scripts._0._Object._0._Agent;
using MyFolder._1._Scripts._0._Object._0._Agent._0._Player;
using MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy;
using MyFolder._1._Scripts._0._Object._4._Shooting;
using MyFolder._1._Scripts._6._GlobalQuest._2._Data;
using MyFolder._1._Scripts._6._GlobalQuest._3._Card;
using MyFolder._1._Scripts._7._PlayerRole;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.IO;
using MyFolder._1._Scripts._0._Object;
using MyFolder._1._Scripts._0._System.Data;
using MyFolder._1._Scripts._6._GlobalQuest;

namespace MyFolder._1._Scripts._3._SingleTone
{
    public class GameDataManager : NetworkBehaviour
    {
        public static GameDataManager Instance { get; private set; }

        // 캐싱된 데이터
        private Dictionary<ushort, EnemyData> cachedEnemyData = new();
        private Dictionary<ushort, PlayerData> cachedPlayerData = new();
        private Dictionary<ushort, ShootingData> cachedShootingData = new();
        private Dictionary<PlayerRoleType, PlayerRoleDefinition> playerRoleDefinitionsData = new();

        //퀘스트
        private GlobalQuestManagerData globalQuestManagerData;
        private Dictionary<ushort, ExterminationQuestData> exterminationQuest = new();
        private Dictionary<ushort, DefenceQuestData> defenceQuest = new();
        private Dictionary<ushort, SurvivalQuestData> survivalQuest = new();
        
        //퀘스트 오브젝트
        private Dictionary<ushort, AgentData> DefenceQuestObjectData = new();
        private Dictionary<ushort, AgentData> SurvivalQuestObjectData = new();
        
        // 카드 데이터 캐시 추가
        private Dictionary<ushort, RewardCardData> cachedRewardCards = new();
        private Dictionary<ushort, DefeatCardData> cachedDefeatCards = new();
        
        //오브젝트 데이터 캐시
        private Dictionary<ushort, SpawnerData> cachedSpawnerData = new();
        private Dictionary<ushort, SpawnerManagerData> cachedSpawnerManagerData = new();

        public bool IsDataInitialized => isDataLocalLoaded;
        // 로컬별 초기화 플래그
        public bool isDataLocalLoaded = false;
        private bool _isPreloading = false;

        [Header("Runtime Options")]
        [SerializeField] private bool useCsvDownloader = false;

        [Header("Addressable Keys")]
        [SerializeField] private string enemyDataKey = "EnemyData";
        [SerializeField] private string playerDataKey = "PlayerData";
        [SerializeField] private string shootingDataKey = "ShootingData";
        [SerializeField] private string roleDefinitionKey = "RoleData";
        [SerializeField] private string globalQuestManagerKey = "GlobalQuestManagerData";
        [SerializeField] private string exterminationQuestKey = "ExterminationData";
        [SerializeField] private string defenceQuestKey = "DefenceData";
        [SerializeField] private string survivalQuestKey = "SurvivalData";
        
        [SerializeField] private string defenceQuestObjectKey = "DefenceQuestObjectData";
        [SerializeField] private string survivalQuestObjectKey = "SurvivalQuestObjectData";
        
        // 카드 데이터 Addressable Keys 추가
        [SerializeField] private string rewardCardDataKey = "RewardCardData";
        [SerializeField] private string defeatCardDataKey = "DefeatCardData";
        
        [SerializeField] private string spawnerDataKey = "SpawnerData";
        [SerializeField] private string spawnerManagerDataKey = "SpawnerManagerData";

        public void Awake()
        {
            Instance = this;
        }

        public override void OnStartServer()
        {
            // 서버에서 모든 데이터 사전 로딩
            StartCoroutine(PreloadAllGameData());
        }
        
        public override void OnStopServer()
        {
            base.OnStopServer();
    
            // 서버 종료 시 AI 데이터 초기화
            ResetGameData();
    
            LogManager.Log(LogCategory.System, "GameDataManager 서버 종료 - AI 데이터 초기화 완료", this);
        }
        public override void OnStartClient()
        {
            // 클라이언트에서도 자체적으로 데이터 로딩
            StartCoroutine(PreloadAllGameData());
        }

        #region Load Agent Data

        private IEnumerator PreloadAllGameData()
        {
            if (_isPreloading || isDataLocalLoaded)
                yield break;
            _isPreloading = true;

            LogManager.Log(LogCategory.System, "GameDataManager 데이터 로딩 시작", this);

            // 1. Enemy 데이터 로드
            yield return StartCoroutine(LoadDataFromJSON(enemyDataKey,cachedEnemyData));

            // 2. Player 데이터 로드
            yield return StartCoroutine(LoadDataFromJSON(playerDataKey,cachedPlayerData));

            // 3. Shooting 데이터 로드
            yield return StartCoroutine(LoadShootingDataFromJSON());

            // 4. Role Definition 데이터 로드
            yield return StartCoroutine(LoadRoleDefinitionFromJSON());
            
            // 5. GlobalQuestManager 데이터 로드
            yield return StartCoroutine(LoadGlobalQuestDataFromJSON());

            // 6. ExterminationQuest 데이터 로드
            yield return StartCoroutine(LoadExterminationQuestFromJSON());

            // 7. DefenceQuest 데이터 로드
            yield return StartCoroutine(LoadDefenceQuestFromJSON());

            // 8. Survival Quest 데이터 로드
            yield return StartCoroutine(LoadSurvivalQuestFromJSON());
            
            // 9. DefenceQuestObject 데이터 로드
            yield return StartCoroutine(LoadDataFromJSON(defenceQuestObjectKey,DefenceQuestObjectData));
            
            // 10. SurvivalQuestObject 데이터 로드
            yield return StartCoroutine(LoadDataFromJSON(survivalQuestObjectKey,SurvivalQuestObjectData));
            
            // 11. Reward Card 데이터 로드
            yield return StartCoroutine(LoadRewardCardDataFromJSON());
            
            // 12. Defeat Card 데이터 로드
            yield return StartCoroutine(LoadDefeatCardDataFromJSON());
            
            // 13. Spawner 데이터 로드
            yield return StartCoroutine(LoadDataFromJSON(spawnerDataKey,cachedSpawnerData));

            // 14. SpawnerManager 데이터 로드
            yield return StartCoroutine(LoadSpawnerManagerDataFromJSON());
            
            isDataLocalLoaded = true;
            _isPreloading = false;
            LogManager.Log(LogCategory.System, "GameDataManager 데이터 로딩 완료", this);
        }
        
        // ObjectData 전용 로더
        private IEnumerator LoadDataFromJSON<T>(string Key,Dictionary<ushort, T> targetCache)where T : ObjectData
        {
            LogManager.Log(LogCategory.System, "AgentData 로드 시작", this);

            // Local-first
            if (useCsvDownloader && TryLoadLocalJson(Key, out var localText))
            {
                try
                {
                    var list = JsonParcing.ReaderArray<T>(localText);
                    targetCache.Clear();
                    foreach (var d in list)
                        targetCache.Add(d.typeId, d);
                    LogManager.Log(LogCategory.System, $"{Key} 로컬 로드 완료: {cachedEnemyData.Count}", this);
                    yield break;
                }
                catch (System.Exception ex)
                {
                    LogManager.LogWarning(LogCategory.System, $"{Key} 로컬 파싱 실패: {ex.Message}", this);
                }
            }

            var handle = Addressables.LoadAssetAsync<TextAsset>(Key);
            yield return handle;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                try
                {
                    var textAsset = handle.Result;
                    var agentDataList = JsonParcing.ReaderArray<T>(textAsset);

                    targetCache.Clear();
                    foreach (var data in agentDataList)
                    {
                        ushort id = data.typeId;
                        targetCache.Add(id, data);
                    }

                    LogManager.Log(LogCategory.System, $"{Key} 로드 완료: {cachedEnemyData.Count}", this);
                }
                catch (System.Exception ex)
                {
                    LogManager.LogError(LogCategory.System, $"{Key} 파싱 오류: {ex.Message}", this);
                }

                Addressables.Release(handle);
            }
            else
            {
                LogManager.LogError(LogCategory.System, $"{Key} 로드 실패: {handle.OperationException?.Message}", this);
            }
        }
        
        //단독
        private IEnumerator LoadShootingDataFromJSON()
        {
            LogManager.Log(LogCategory.System, "ShootingData 로드 시작", this);

            // Local-first
            if (useCsvDownloader && TryLoadLocalJson(shootingDataKey, out var localText))
            {
                try
                {
                    var list = JsonParcing.ReaderArray<ShootingData>(localText);
                    cachedShootingData.Clear();
                    foreach (var d in list)
                        cachedShootingData.Add(d.typeId, d);
                    LogManager.Log(LogCategory.System, $"ShootingData 로컬 로드 완료: {cachedShootingData.Count}", this);
                    yield break;
                }
                catch (System.Exception ex)
                {
                    LogManager.LogWarning(LogCategory.System, $"ShootingData 로컬 파싱 실패: {ex.Message}", this);
                }
            }

            var handle = Addressables.LoadAssetAsync<TextAsset>(shootingDataKey);
            yield return handle;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                try
                {
                    var textAsset = handle.Result;
                    var shootingDataList = JsonParcing.ReaderArray<ShootingData>(textAsset);

                    cachedShootingData.Clear();
                    foreach (var data in shootingDataList)
                    {
                        ushort id = data.typeId;
                        cachedShootingData.Add(id, data);
                    }

                    LogManager.Log(LogCategory.System, $"ShootingData 로드 완료: {cachedShootingData.Count}", this);
                }
                catch (System.Exception ex)
                {
                    LogManager.LogError(LogCategory.System, $"ShootingData 파싱 오류: {ex.Message}", this);
                }

                Addressables.Release(handle);
            }
            else
            {
                LogManager.LogError(LogCategory.System, $"ShootingData 로드 실패: {handle.OperationException?.Message}", this);
            }
        }

        //단독
        private IEnumerator LoadRoleDefinitionFromJSON()
        {
            LogManager.Log(LogCategory.System, "RoleDefinition 로드 시작", this);

            // Local-first
            if (useCsvDownloader && TryLoadLocalJson(roleDefinitionKey, out var localText))
            {
                try
                {
                    var list = JsonParcing.ReaderArray<PlayerRoleDefinition>(localText);
                    playerRoleDefinitionsData.Clear();
                    foreach (var d in list)
                        playerRoleDefinitionsData.Add(d.GetRole, d);
                    LogManager.Log(LogCategory.System, $"RoleDefinition 로컬 로드 완료: {playerRoleDefinitionsData.Count}", this);
                    yield break;
                }
                catch (System.Exception ex)
                {
                    LogManager.LogWarning(LogCategory.System, $"RoleDefinition 로컬 파싱 실패: {ex.Message}", this);
                }
            }

            var handle = Addressables.LoadAssetAsync<TextAsset>(roleDefinitionKey);
            yield return handle;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                try
                {
                    var textAsset = handle.Result;
                    var roleDefinitionList = JsonParcing.ReaderArray<PlayerRoleDefinition>(textAsset);

                    playerRoleDefinitionsData.Clear();
                    foreach (var data in roleDefinitionList)
                    {
                        playerRoleDefinitionsData.Add(data.GetRole, data);
                    }

                    LogManager.Log(LogCategory.System, $"RoleDefinition 로드 완료: {playerRoleDefinitionsData.Count}", this);
                }
                catch (System.Exception ex)
                {
                    LogManager.LogError(LogCategory.System, $"RoleDefinition 파싱 오류: {ex.Message}", this);
                }

                Addressables.Release(handle);
            }
            else
            {
                LogManager.LogError(LogCategory.System, $"RoleDefinition 로드 실패: {handle.OperationException?.Message}", this);
            }
        }

        //단독
        private IEnumerator LoadGlobalQuestDataFromJSON()
        {
            
            LogManager.Log(LogCategory.System, "globalQuestManagerKey 로드 시작", this);
            // Local-first
            if (useCsvDownloader && TryLoadLocalJson(globalQuestManagerKey, out var localText))
            {
                try
                {
                    var list = JsonParcing.ReaderArray<GlobalQuestManagerData>(localText);
                    foreach (var d in list)
                        globalQuestManagerData = d;
                    LogManager.Log(LogCategory.System, $"globalQuestManagerKey 로컬 로드 완료: {exterminationQuest.Count}", this);
                    yield break;
                }
                catch (System.Exception ex)
                {
                    LogManager.LogWarning(LogCategory.System, $"globalQuestManagerKey 로컬 파싱 실패: {ex.Message}", this);
                }
            }

            var handle = Addressables.LoadAssetAsync<TextAsset>(globalQuestManagerKey);
            yield return handle;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                try
                {
                    var textAsset = handle.Result;
                    var list = JsonParcing.ReaderArray<GlobalQuestManagerData>(textAsset);

                    foreach (var d in list)
                        globalQuestManagerData = d;

                    LogManager.Log(LogCategory.System, $"exterminationQuest 로드 완료: {exterminationQuest.Count}", this);
                }
                catch (System.Exception ex)
                {
                    LogManager.LogError(LogCategory.System, $"exterminationQuest 파싱 오류: {ex.Message}", this);
                }

                Addressables.Release(handle);
            }
            else
            {
                LogManager.LogError(LogCategory.System, $"exterminationQuest 로드 실패: {handle.OperationException?.Message}", this);
            }
        }
        
        //단독
        private IEnumerator LoadExterminationQuestFromJSON()
        {
            LogManager.Log(LogCategory.System, "exterminationQuest 로드 시작", this);

            // Local-first
            if (useCsvDownloader && TryLoadLocalJson(exterminationQuestKey, out var localText))
            {
                try
                {
                    var list = JsonParcing.ReaderArray<ExterminationQuestData>(localText);
                    exterminationQuest.Clear();
                    foreach (var d in list)
                        exterminationQuest.Add(d.typeId, d);
                    LogManager.Log(LogCategory.System, $"exterminationQuest 로컬 로드 완료: {exterminationQuest.Count}", this);
                    yield break;
                }
                catch (System.Exception ex)
                {
                    LogManager.LogWarning(LogCategory.System, $"exterminationQuest 로컬 파싱 실패: {ex.Message}", this);
                }
            }

            var handle = Addressables.LoadAssetAsync<TextAsset>(exterminationQuestKey);
            yield return handle;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                try
                {
                    var textAsset = handle.Result;
                    var list = JsonParcing.ReaderArray<ExterminationQuestData>(textAsset);

                    exterminationQuest.Clear();
                    foreach (var data in list)
                    {
                        exterminationQuest.Add(data.typeId, data);
                    }

                    LogManager.Log(LogCategory.System, $"exterminationQuest 로드 완료: {exterminationQuest.Count}", this);
                }
                catch (System.Exception ex)
                {
                    LogManager.LogError(LogCategory.System, $"exterminationQuest 파싱 오류: {ex.Message}", this);
                }

                Addressables.Release(handle);
            }
            else
            {
                LogManager.LogError(LogCategory.System, $"exterminationQuest 로드 실패: {handle.OperationException?.Message}", this);
            }
        }

        //단독
        private IEnumerator LoadDefenceQuestFromJSON()
        {
            LogManager.Log(LogCategory.System, "defenceQuest 로드 시작", this);

            // Local-first
            if (useCsvDownloader && TryLoadLocalJson(defenceQuestKey, out var localText))
            {
                try
                {
                    var list = JsonParcing.ReaderArray<DefenceQuestData>(localText);
                    defenceQuest.Clear();
                    foreach (var d in list)
                        defenceQuest.Add(d.typeId, d);
                    LogManager.Log(LogCategory.System, $"defenceQuest 로컬 로드 완료: {defenceQuest.Count}", this);
                    yield break;
                }
                catch (System.Exception ex)
                {
                    LogManager.LogWarning(LogCategory.System, $"defenceQuest 로컬 파싱 실패: {ex.Message}", this);
                }
            }

            var handle = Addressables.LoadAssetAsync<TextAsset>(defenceQuestKey);
            yield return handle;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                try
                {
                    var textAsset = handle.Result;
                    var list = JsonParcing.ReaderArray<DefenceQuestData>(textAsset);

                    defenceQuest.Clear();
                    foreach (var data in list)
                    {
                        defenceQuest.Add(data.typeId, data);
                    }

                    LogManager.Log(LogCategory.System, $"defenceQuest 로드 완료: {defenceQuest.Count}", this);
                }
                catch (System.Exception ex)
                {
                    LogManager.LogError(LogCategory.System, $"defenceQuest 파싱 오류: {ex.Message}", this);
                }

                Addressables.Release(handle);
            }
            else
            {
                LogManager.LogError(LogCategory.System, $"defenceQuest 로드 실패: {handle.OperationException?.Message}", this);
            }
        }

        //단독
        private IEnumerator LoadSurvivalQuestFromJSON()
        {
            LogManager.Log(LogCategory.System, "survivalQuest 로드 시작", this);

            // Local-first
            if (useCsvDownloader && TryLoadLocalJson(survivalQuestKey, out var localText))
            {
                try
                {
                    var list = JsonParcing.ReaderArray<SurvivalQuestData>(localText);
                    survivalQuest.Clear();
                    foreach (var d in list)
                        survivalQuest.Add(d.typeId, d);
                    LogManager.Log(LogCategory.System, $"survivalQuest 로컬 로드 완료: {survivalQuest.Count}", this);
                    yield break;
                }
                catch (System.Exception ex)
                {
                    LogManager.LogWarning(LogCategory.System, $"survivalQuest 로컬 파싱 실패: {ex.Message}", this);
                }
            }

            var handle = Addressables.LoadAssetAsync<TextAsset>(survivalQuestKey);
            yield return handle;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                try
                {
                    var textAsset = handle.Result;
                    var list = JsonParcing.ReaderArray<SurvivalQuestData>(textAsset);

                    survivalQuest.Clear();
                    foreach (var data in list)
                    {
                        survivalQuest.Add(data.typeId, data);
                    }

                    LogManager.Log(LogCategory.System, $"survivalQuest 로드 완료: {survivalQuest.Count}", this);
                }
                catch (System.Exception ex)
                {
                    LogManager.LogError(LogCategory.System, $"survivalQuest 파싱 오류: {ex.Message}", this);
                }

                Addressables.Release(handle);
            }
            else
            {
                LogManager.LogError(LogCategory.System, $"survivalQuest 로드 실패: {handle.OperationException?.Message}", this);
            }
        }
        
        //단독
        private IEnumerator LoadRewardCardDataFromJSON()
        {
            LogManager.Log(LogCategory.System, "RewardCard 로드 시작", this);

            // Local-first
            if (useCsvDownloader && TryLoadLocalJson(rewardCardDataKey, out var localText))
            {
                try
                {
                    var list = JsonParcing.ReaderArray<RewardCardData>(localText);
                    cachedRewardCards.Clear();
                    foreach (var d in list)
                        cachedRewardCards.Add(d.cardId, d);
                    LogManager.Log(LogCategory.System, $"RewardCard 로컬 로드 완료: {cachedRewardCards.Count}", this);
                    yield break;
                }
                catch (System.Exception ex)
                {
                    LogManager.LogWarning(LogCategory.System, $"RewardCard 로컬 파싱 실패: {ex.Message}", this);
                }
            }

            var handle = Addressables.LoadAssetAsync<TextAsset>(rewardCardDataKey);
            yield return handle;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                try
                {
                    var textAsset = handle.Result;
                    var list = JsonParcing.ReaderArray<RewardCardData>(textAsset);

                    cachedRewardCards.Clear();
                    foreach (var data in list)
                    {
                        cachedRewardCards.Add(data.cardId, data);
                    }

                    LogManager.Log(LogCategory.System, $"RewardCard 로드 완료: {cachedRewardCards.Count}", this);
                }
                catch (System.Exception ex)
                {
                    LogManager.LogError(LogCategory.System, $"RewardCard 파싱 오류: {ex.Message}", this);
                }

                Addressables.Release(handle);
            }
            else
            {
                LogManager.LogError(LogCategory.System, $"RewardCard 로드 실패: {handle.OperationException?.Message}", this);
            }
        }
        
        //단독
        private IEnumerator LoadDefeatCardDataFromJSON()
        {
            LogManager.Log(LogCategory.System, "DefeatCard 로드 시작", this);

            // Local-first
            if (useCsvDownloader && TryLoadLocalJson(defeatCardDataKey, out var localText))
            {
                try
                {
                    var list = JsonParcing.ReaderArray<DefeatCardData>(localText);
                    cachedDefeatCards.Clear();
                    foreach (var d in list)
                        cachedDefeatCards.Add(d.cardId, d);
                    LogManager.Log(LogCategory.System, $"DefeatCard 로컬 로드 완료: {cachedDefeatCards.Count}", this);
                    yield break;
                }
                catch (System.Exception ex)
                {
                    LogManager.LogWarning(LogCategory.System, $"DefeatCard 로컬 파싱 실패: {ex.Message}", this);
                }
            }

            var handle = Addressables.LoadAssetAsync<TextAsset>(defeatCardDataKey);
            yield return handle;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                try
                {
                    var textAsset = handle.Result;
                    var list = JsonParcing.ReaderArray<DefeatCardData>(textAsset);

                    cachedDefeatCards.Clear();
                    foreach (var data in list)
                    {
                        cachedDefeatCards.Add(data.cardId, data);
                    }

                    LogManager.Log(LogCategory.System, $"DefeatCard 로드 완료: {cachedDefeatCards.Count}", this);
                }
                catch (System.Exception ex)
                {
                    LogManager.LogError(LogCategory.System, $"DefeatCard 파싱 오류: {ex.Message}", this);
                }

                Addressables.Release(handle);
            }
            else
            {
                LogManager.LogError(LogCategory.System, $"DefeatCard 로드 실패: {handle.OperationException?.Message}", this);
            }
        }

        //단독        
        private IEnumerator LoadSpawnerManagerDataFromJSON()
        {  
            LogManager.Log(LogCategory.System, "spawnerDataKey 로드 시작", this);

            // Local-first
            if (useCsvDownloader && TryLoadLocalJson(spawnerManagerDataKey, out var localText))
            {
                try
                {
                    var list = JsonParcing.ReaderArray<SpawnerManagerData>(localText);
                    cachedSpawnerManagerData.Clear();
                    foreach (var d in list)
                        cachedSpawnerManagerData.Add(d.typeId, d);
                    LogManager.Log(LogCategory.System, $"DefeatCard 로컬 로드 완료: {cachedSpawnerManagerData.Count}", this);
                    yield break;
                }
                catch (System.Exception ex)
                {
                    LogManager.LogWarning(LogCategory.System, $"DefeatCard 로컬 파싱 실패: {ex.Message}", this);
                }
            }

            var handle = Addressables.LoadAssetAsync<TextAsset>(spawnerManagerDataKey);
            yield return handle;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                try
                {
                    var textAsset = handle.Result;
                    var list = JsonParcing.ReaderArray<SpawnerManagerData>(textAsset);

                    cachedSpawnerManagerData.Clear();
                    foreach (var data in list)
                    {
                        cachedSpawnerManagerData.Add(data.typeId, data);
                    }

                    LogManager.Log(LogCategory.System, $"DefeatCard 로드 완료: {cachedSpawnerManagerData.Count}", this);
                }
                catch (System.Exception ex)
                {
                    LogManager.LogError(LogCategory.System, $"DefeatCard 파싱 오류: {ex.Message}", this);
                }

                Addressables.Release(handle);
            }
            else
            {
                LogManager.LogError(LogCategory.System, $"DefeatCard 로드 실패: {handle.OperationException?.Message}", this);
            }
        }
        
        // Local JSON helper
        private bool TryLoadLocalJson(string key, out UnityEngine.TextAsset textAsset)
        {
            textAsset = null;
            if (!useCsvDownloader) return false;
            var file = key.EndsWith(".json") ? key : key + ".json";
            var path = Path.Combine(UnityEngine.Application.persistentDataPath, "Data", file);
            if (!File.Exists(path)) return false;
            try
            {
                string json = System.IO.File.ReadAllText(path);
                textAsset = new UnityEngine.TextAsset(json);
                isDataLocalLoaded = true;
                return true;
            }
            catch (System.Exception ex)
            {
                LogManager.LogWarning(LogCategory.System, $"Local JSON read failed({key}): {ex.Message}", this);
                return false;
            }
        }

        public void SetUseCsvDownloader(bool value)
        {
            useCsvDownloader = value;
        }
        #endregion

        #region DataGettor

        public PlayerData GetPlayerDataById(ushort typeId)
        {
            return cachedPlayerData.GetValueOrDefault(typeId);
        }

        public ShootingData GetShootingDataById(ushort typeId)
        {
            return cachedShootingData.GetValueOrDefault(typeId);
        }

        public EnemyData GetEnemyDataById(ushort typeId)
        {
            return cachedEnemyData.GetValueOrDefault(typeId);
        }

        public PlayerRoleDefinition GetPlayerRoleDefinitionByRole(PlayerRoleType roleType)
        {
            return playerRoleDefinitionsData.GetValueOrDefault(roleType);
        }

        public GlobalQuestManagerData GetGlobalQuestManagerData()
        {
            return globalQuestManagerData;
        }
        
        public ExterminationQuestData GetExterminationDataById(ushort typeId)
        {
            return exterminationQuest.GetValueOrDefault(typeId);
        }

        public DefenceQuestData GetDefenceDataById(ushort typeId)
        {
            return defenceQuest.GetValueOrDefault(typeId);
        }

        public SurvivalQuestData GetSurvivalDataById(ushort typeId)
        {
            return survivalQuest.GetValueOrDefault(typeId);
        }

        public AgentData GetDefenceQuestObjectDataById(ushort typeId)
        {
            return DefenceQuestObjectData.GetValueOrDefault(typeId);
        }

        public AgentData GetSurvivalQuestObjectDataById(ushort typeId)
        {
            return SurvivalQuestObjectData.GetValueOrDefault(typeId);
        }
        
        // 카드 데이터 Getter 메서드들
        public RewardCardData GetRewardCardsByType(ushort cardId)
        {
            return cachedRewardCards.GetValueOrDefault(cardId);
        }
        
        public DefeatCardData GetDefeatCardsByType(ushort cardId)
        {
            return cachedDefeatCards.GetValueOrDefault(cardId);
        }
        
        public SpawnerData GetSpawnerDataById(ushort id)
        {
            return cachedSpawnerData.GetValueOrDefault(id);
        }
        public SpawnerManagerData GetSpawnerManagerDataById(ushort id)
        {
            return cachedSpawnerManagerData.GetValueOrDefault(id);
        }
        
        public List<EnemyData> GetAllEnemyData()
        {
            return cachedEnemyData.Values.ToList();
        }
        
        public List<ShootingData> GetAllShootingData()
        {
            return cachedShootingData.Values.ToList();
        }

        #endregion

        #region Reset
        
        /// <summary>
        /// 게임 종료 시 AI 데이터 초기화 (모든 패배 카드 효과 제거)
        /// </summary>
        public void ResetGameData()
        {
            if(IsServerInitialized)
                ResetGameData_Observers();
        }

        [ObserversRpc]
        private void ResetGameData_Observers()
        {
            
            // ===== EnemyData 초기화 =====
            foreach(var enemyData in cachedEnemyData.Values)
            {
                enemyData.ClearHpModifiers();
                enemyData.ClearSpeedModifiers();
                enemyData.ClearAttackSpeedModifiers();
            }
    
    
            // ===== ShootingData 초기화 (패배 카드가 적용되는 부분) =====
            foreach(var shootingData in cachedShootingData.Values)
            {
                shootingData.ClearBulletSpeedModifiers();   // 적 탄속
                shootingData.ClearBulletDamageModifiers();  // 적 탄 데미지
                shootingData.ClearBulletSizeModifiers();    // 적 탄 사이즈
            }
            
        }

        #endregion
        
    }
}

