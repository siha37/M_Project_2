using System.Collections.Generic;
using System.Text;
using FishNet;
using FishNet.Connection;
using FishNet.Object;
using MyFolder._1._Scripts._0._Object._0._Agent;
using MyFolder._1._Scripts._0._Object._0._Agent._0._Player._1._SubObject._0._Shield;
using MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy;
using MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.Main;
using MyFolder._1._Scripts._1._UI._0._GameStage;
using MyFolder._1._Scripts._10._Sound.Impact;
using MyFolder._1._Scripts._12._Pool;
using MyFolder._1._Scripts._3._SingleTone;
using MyFolder._1._Scripts._8999._Utility.Corutin;
using Unity.VisualScripting;
using UnityEngine;

namespace MyFolder._1._Scripts._0._Object._2._Projectile
{
    public class BulletManager : NetworkBehaviour
    {
        #region Singleton
        
        public static BulletManager Instance { get; private set; }
        
        #endregion

        #region Inspector Settings
        
        [Header("Bullet Pool Settings")]
        [SerializeField] private GameObject bulletPrefab;
        [SerializeField] private GameObject bulletsParent;
        [SerializeField] private int initialPoolSize = 100;
        [SerializeField] private int expandSize = 50;           // 확장 시 추가할 개수
        [SerializeField] private int maxPoolSize = 800;         // 최대 풀 크기
        [SerializeField] private bool enableDynamicExpansion = true; // 동적 확장 활성화
        
        #endregion

        #region Server Bullet Pool
        
        private List<ServerBullet> activeBullets = new List<ServerBullet>();
        private Queue<ServerBullet> bulletPool = new Queue<ServerBullet>();
        
        #endregion

        #region Visual Bullet Pool (Client)
        
        private Queue<GameObject> visualBulletPool = new Queue<GameObject>();
        // ✅ 성능 최적화: HashSet과 List 병용
        private HashSet<GameObject> activeVisualBulletsSet = new HashSet<GameObject>();
        private List<GameObject> activeVisualBullets = new List<GameObject>(); // 순회 및 디버깅용

        // ✅ ID 기반 시각 총알 관리 추가
        private Dictionary<uint, GameObject> visualBulletsById = new Dictionary<uint, GameObject>();
        private Dictionary<uint, Coroutine> bulletCoroutines = new Dictionary<uint, Coroutine>();
        
        // 🔍 디버깅: 총알 생명주기 추적
        private Dictionary<uint, BulletLifecycleLog> bulletLifecycleLogs = new Dictionary<uint, BulletLifecycleLog>();
        
        #endregion

        #region Bullet Particle

        [SerializeField] private ParticleEffectPool particleEffectPool;

        #endregion
        
        #region Lifecycle & Initialization
        
        public override void OnStartServer()
        {
            if (!Instance)
            {
                Instance = this;
                InitializeServerPool();
                LogManager.Log(LogCategory.Projectile, "BulletManager 서버 초기화 완료 - 발사 준비됨", this);
            }
            else
            {
                LogManager.LogWarning(LogCategory.Projectile, "BulletManager 서버 인스턴스가 이미 존재합니다.", this);
            }
        }

        public override void OnStartClient()
        {
            if (!Instance)
            {
                Instance = this;
                LogManager.Log(LogCategory.Projectile, "BulletManager 클라이언트 인스턴스 설정됨", this);
            }

            // ✅ Host 모드 지원: 서버에서도 시각 풀 초기화 (Host일 때 시각적 표현 필요)
            InitializeVisualPool();
            LogManager.Log(LogCategory.Projectile, "BulletManager 시각 풀 초기화 완료 (Host 모드 지원)", this);
        }
        
        #endregion

        #region Server Pool Management
        
        private void InitializeServerPool()
        {
            for (int i = 0; i < initialPoolSize; i++)
            {
                ServerBullet bullet = new ServerBullet();
                bulletPool.Enqueue(bullet);
            }
            LogManager.Log(LogCategory.Projectile, $"BulletManager 서버 총알 풀 초기화: {initialPoolSize}개", this);
        }

        private bool ExpandServerPool()
        {
            if (!enableDynamicExpansion)
            {
                LogManager.LogWarning(LogCategory.Projectile, "BulletManager 동적 확장이 비활성화되어 있습니다.", this);
                return false;
            }

            int currentTotalSize = bulletPool.Count + activeBullets.Count;
            if (currentTotalSize >= maxPoolSize)
            {
                LogManager.LogError(LogCategory.Projectile, $"BulletManager 최대 풀 크기 도달: {maxPoolSize}개", this);
                return false;
            }

            int actualExpandSize = Mathf.Min(expandSize, maxPoolSize - currentTotalSize);

            for (int i = 0; i < actualExpandSize; i++)
            {
                ServerBullet bullet = new ServerBullet();
                bulletPool.Enqueue(bullet);
            }

            LogManager.Log(LogCategory.Projectile, $"BulletManager 서버 풀 확장: +{actualExpandSize}개 (총 {currentTotalSize + actualExpandSize}개)", this);
            return true;
        }
        
        #endregion

        #region Visual Pool Management
        
        private void InitializeVisualPool()
        {
            for (int i = 0; i < initialPoolSize; i++)
            {
                CreateVisualBulletPoolObject();
            }
            LogManager.Log(LogCategory.Projectile, $"BulletManager 클라이언트 시각 풀 초기화: {initialPoolSize}개", this);
        }

        private void CreateVisualBulletPoolObject()
        {
            GameObject bullet;
            if(bulletsParent)
                bullet = Instantiate(bulletPrefab,bulletsParent.transform);
            else
                bullet = Instantiate(bulletPrefab);
            bullet.SetActive(false);

            // 시각 전용 설정
            Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
            if (rb) rb.bodyType = RigidbodyType2D.Kinematic;

            // 네트워크 컴포넌트 제거 (시각 전용이므로)
            NetworkObject netObj = bullet.GetComponent<NetworkObject>();
            if (netObj) DestroyImmediate(netObj);

            Projectile proj = bullet.GetComponent<Projectile>();
            if (proj) DestroyImmediate(proj);

            visualBulletPool.Enqueue(bullet);
        }

        private bool ExpandVisualPool()
        {
            if (!enableDynamicExpansion)
            {
                LogManager.LogWarning(LogCategory.Projectile, "BulletManager 동적 확장이 비활성화되어 있습니다.", this);
                return false;
            }

            int currentTotalSize = visualBulletPool.Count + activeVisualBullets.Count;
            if (currentTotalSize >= maxPoolSize)
            {
                LogManager.LogError(LogCategory.Projectile, $"BulletManager 최대 시각 풀 크기 도달: {maxPoolSize}개", this);
                return false;
            }

            int actualExpandSize = Mathf.Min(expandSize, maxPoolSize - currentTotalSize);

            for (int i = 0; i < actualExpandSize; i++)
            {
                CreateVisualBulletPoolObject();
            }

            LogManager.Log(LogCategory.Projectile, $"BulletManager 시각 풀 확장: +{actualExpandSize}개 (총 {currentTotalSize + actualExpandSize}개)", this);
            return true;
        }
        
        #endregion

        #region Bullet Firing (ServerRpc)
        
        [ServerRpc(RequireOwnership = false)]
        public void FireBulletWithConnection(Vector3 startPos, float angle, float speed, float damage, float lifetime,float size,float piercing, NetworkConnection shooter)
        {
            if (!IsServerInitialized) return;

            if (bulletPool.Count > 0)
            {
                // 총알 획득 및 디큐
                ServerBullet bullet = bulletPool.Dequeue();
                // 총알 초기화 및 연결 / 활성화 등록
                bullet.InitializeWithConnection(startPos, angle, speed, damage, lifetime, size, piercing, shooter);
                activeBullets.Add(bullet);

                // 시각 총알 생성
                CreateVisualBulletRpc(startPos, angle, speed, lifetime, size, bullet.bulletId, 0f);

                // 그 다음 Update 실행 (네트워크 지연 보정)
                bullet.Update(Time.fixedDeltaTime);

                // ✅ Update 중 충돌로 반환되었는지 확인
                if (bullet.bulletId == 0)
                {
                    // 이미 충돌로 반환됨 - 발사 즉시 충돌한 경우
                    LogManager.Log(LogCategory.Projectile, $"⚡ 총알이 발사 즉시 충돌하여 반환됨", this);
                }
            }
            else
            {
                // 풀 확장 시도
                LogManager.LogWarning(LogCategory.Projectile, "BulletManager 서버 총알 풀이 고갈되었습니다! 풀 확장을 시도합니다...", this);

                if (ExpandServerPool() && bulletPool.Count > 0)
                {
                    // 확장 성공 시 다시 시도
                    ServerBullet bullet = bulletPool.Dequeue();
                    bullet.InitializeWithConnection(startPos, angle, speed, damage, lifetime, size, piercing, shooter);
                    activeBullets.Add(bullet);

                    // CreateVisualBulletRpc 먼저
                    CreateVisualBulletRpc(startPos, angle, speed, lifetime, size, bullet.bulletId, 0f);
                    
                    bullet.Update(Time.fixedDeltaTime);

                    // Update 중 충돌로 반환되었는지 확인
                    if (bullet.bulletId == 0)
                    {
                        LogManager.Log(LogCategory.Projectile, 
                            $"⚡ 총알이 발사 즉시 충돌하여 반환됨 (풀 확장)", this);
                        return;
                    }
                    
                    LogManager.Log(LogCategory.Projectile, $"BulletManager 풀 확장 후 서버 총알 발사: {activeBullets.Count}개 활성", this);
                }
                else
                {
                    LogManager.LogError(LogCategory.Projectile, "BulletManager 풀 확장 실패! 총알 발사를 취소합니다.", this);
                }
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void FireBulletForEnemy(Vector3 startPos, float angle, float speed, float damage, float lifetime,float size,float piercing, GameObject enemyObject)
        {
            if (!IsServerInitialized) return;

            if (bulletPool.Count > 0)
            {
                ServerBullet bullet = bulletPool.Dequeue();
                bullet.InitializeForEnemy(startPos, angle, speed, damage, lifetime,size, piercing,enemyObject);
                activeBullets.Add(bullet);

                // ✅ 수정: serverElapsed를 0으로 설정 (적 총알은 즉시 발사)
                CreateVisualBulletRpc(startPos, angle, speed, lifetime, size, bullet.bulletId, 0f);
            }
            else
            {
                // 풀 확장 시도
                if (ExpandServerPool() && bulletPool.Count > 0)
                {
                    ServerBullet bullet = bulletPool.Dequeue();
                    bullet.InitializeForEnemy(startPos, angle, speed, damage, lifetime, size, piercing, enemyObject);
                    activeBullets.Add(bullet);

                    // ✅ 수정: serverElapsed를 0으로 설정
                    CreateVisualBulletRpc(startPos, angle, speed, lifetime, size, bullet.bulletId, 0f);
                }
            }
        }
        
        #endregion

        #region Visual Bullet Management (RPC)
        
        [ObserversRpc]
        private void CreateVisualBulletRpc(Vector3 startPos, float angle, float speed, float lifetime, float size, uint bulletId, float serverElapsed)
        {
            // ✅ lifetime 관리 권한이 서버에만 있으므로 보정 불필요
            if (visualBulletPool.Count > 0)
            {
                GameObject visualBullet = visualBulletPool.Dequeue();
                
                visualBullet.transform.position = startPos;
                visualBullet.transform.rotation = Quaternion.Euler(0, 0, angle);
                visualBullet.transform.localScale = new Vector3(size, size, 1);
                visualBullet.SetActive(true);
            
                // ✅ ID로 매칭 저장
                visualBulletsById[bulletId] = visualBullet;
                Coroutine moveCoroutine = StartCoroutine(MoveVisualBulletWithPhysics(visualBullet, speed, lifetime, bulletId, serverElapsed));
                bulletCoroutines[bulletId] = moveCoroutine;
            
                activeVisualBulletsSet.Add(visualBullet); // HashSet에 추가
                activeVisualBullets.Add(visualBullet); // List에 추가 (순회용)
            
                // 🔍 디버깅: 생명주기 로그 시작
                bulletLifecycleLogs[bulletId] = new BulletLifecycleLog
                {
                    bulletId = bulletId,
                    createTime = Time.time,
                    lifetime = lifetime,
                    events = new System.Collections.Generic.List<string>
                    {
                        $"[{Time.time:F2}] 생성 - 위치:{startPos}, 속도:{speed}, lifetime:{lifetime}"
                    }
                };
            
                string roleText = IsServerInitialized ? "(Host/Server)" : "(Client)";
                LogManager.Log(LogCategory.Projectile, 
                    $"🟢 [ID:{bulletId}] 시각 총알 생성 {roleText}: {activeVisualBullets.Count}개 활성, 풀:{visualBulletPool.Count}개", this);
            }
            else
            {
                // ✅ 시각 풀 확장 시도
                LogManager.LogWarning(LogCategory.Projectile, "BulletManager 시각 총알 풀이 고갈되었습니다! 풀 확장을 시도합니다...", this);
            
                if (ExpandVisualPool() && visualBulletPool.Count > 0)
                {
                    // 확장 성공 시 다시 시도
                    GameObject visualBullet = visualBulletPool.Dequeue();
                    visualBullet.transform.position = startPos;
                    visualBullet.transform.rotation = Quaternion.Euler(0, 0, angle);
                    visualBullet.transform.localScale = new Vector3(size, size, 1);
                    visualBullet.SetActive(true);
                
                    visualBulletsById[bulletId] = visualBullet;
                    Coroutine moveCoroutine = StartCoroutine(MoveVisualBulletWithPhysics(visualBullet, speed, lifetime, bulletId, serverElapsed));
                    bulletCoroutines[bulletId] = moveCoroutine;
                
                    activeVisualBulletsSet.Add(visualBullet); // HashSet에 추가
                    activeVisualBullets.Add(visualBullet); // List에 추가 (순회용)
                    
                    // 🔍 디버깅: 생명주기 로그 시작
                    bulletLifecycleLogs[bulletId] = new BulletLifecycleLog
                    {
                        bulletId = bulletId,
                        createTime = Time.time,
                        lifetime = lifetime,
                        events = new System.Collections.Generic.List<string>
                        {
                            $"[{Time.time:F2}] 생성(풀확장) - 위치:{startPos}, 속도:{speed}, lifetime:{lifetime}"
                        }
                    };
                    
                    string roleText = IsServerInitialized ? "(Host/Server)" : "(Client)";
                    LogManager.Log(LogCategory.Projectile, 
                        $"🟢 [ID:{bulletId}] 시각 총알 생성(풀확장) {roleText}: {activeVisualBullets.Count}개 활성, 풀:{visualBulletPool.Count}개", this);
                }
                else
                {
                    LogManager.LogError(LogCategory.Projectile, "BulletManager 시각 풀 확장 실패! 시각 총알 생성을 취소합니다.", this);
                }
            }
        }
    
    // ✅ bulletId와 serverElapsed 파라미터 추가된 시각 총알 이동
    private System.Collections.IEnumerator MoveVisualBulletWithPhysics(GameObject bullet, float speed, float lifetime, uint bulletId, float serverElapsed)
    {
        Vector3 direction = bullet.transform.right;
        LayerMask wallLayer = LayerMask.GetMask("Wall","WallSide");
        
        // 🔍 디버깅: 코루틴 시작
        LogBulletEvent(bulletId, $"코루틴 시작 - serverElapsed:{serverElapsed:F3}s");
        
        // ✅ 네트워크 지연 보정: 서버가 이미 진행한 시간만큼 앞으로 이동
        if (serverElapsed > 0f)
        {
            Vector3 compensatedPos = bullet.transform.position + direction * (speed * serverElapsed);
            
            // 보정된 위치까지 벽 충돌 검사
            if (!Physics2D.Linecast(bullet.transform.position, compensatedPos, wallLayer))
            {
                bullet.transform.position = compensatedPos;
                LogBulletEvent(bulletId, $"보정 이동 완료 - 거리:{(speed * serverElapsed):F2}");
            }
            else
            {
                // ✅ 변경: 타임아웃 후 자동 정리 추가
                LogBulletEvent(bulletId, "보정 중 벽 충돌 감지 - 2초 대기 시작");
                yield return WaitForSecondsCache.Get(2f);
                
                if (visualBulletsById.ContainsKey(bulletId))
                {
                    LogBulletEvent(bulletId, "타임아웃 강제 반환 (보정 중 벽충돌)");
                    LogManager.LogWarning(LogCategory.Projectile, 
                        $"[ID:{bulletId}] 패킷손실방지 - 타임아웃 강제 반환 (보정 중 벽충돌)", this);
                    ReturnVisualBulletById(bulletId);
                }
                else
                {
                    LogManager.Log(LogCategory.Projectile, 
                        $"✅ [ID:{bulletId}] 서버 명령으로 이미 반환됨 (보정 중 벽충돌)", this);
                }
                yield break;
            }
        }
        
        // ✅ 변경: lifetime 안전 장치 추가
        float safetyTimeout = lifetime + 2f;
        float elapsed = serverElapsed;
        
        LogBulletEvent(bulletId, $"메인 루프 시작 - safetyTimeout:{safetyTimeout:F2}s");
        
        while (bullet.activeInHierarchy && elapsed < safetyTimeout)
        {
            Vector3 nextPos = bullet.transform.position + direction * (speed * Time.fixedDeltaTime);
        
            // ✅ 벽 충돌 검사 (시각 총알도 벽에서 멈춤)
            if (Physics2D.Linecast(bullet.transform.position, nextPos, wallLayer))
            {
                // ✅ 변경: 타임아웃 후 자동 정리 추가
                LogBulletEvent(bulletId, $"⚠️ 벽 충돌 감지 (elapsed:{elapsed:F2}s) - 2초 대기 시작");
                yield return WaitForSecondsCache.Get(2f);
                
                if (visualBulletsById.ContainsKey(bulletId))
                {
                    LogBulletEvent(bulletId, "⛔ 타임아웃 강제 반환 (벽충돌)");
                    LogManager.LogWarning(LogCategory.Projectile, 
                        $"🔴 [ID:{bulletId}] 패킷손실방지 - 타임아웃 강제 반환 (벽충돌, elapsed:{elapsed:F2}s)", this);
                    ReturnVisualBulletById(bulletId);
                }
                else
                {
                    LogManager.Log(LogCategory.Projectile, 
                        $"✅ [ID:{bulletId}] 서버 명령으로 이미 반환됨 (벽충돌)", this);
                }
                yield break;
            }
        
            bullet.transform.position = nextPos;
            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
    
        // 🔍 디버깅: 루프 종료 원인 파악
        if (!bullet.activeInHierarchy)
        {
            LogBulletEvent(bulletId, $"✅ 루프 종료 - bullet 비활성화됨 (elapsed:{elapsed:F2}s)");
            LogManager.Log(LogCategory.Projectile, 
                $"✅ [ID:{bulletId}] 코루틴 정상 종료 - 서버 명령으로 비활성화됨", this);
        }
        else if (elapsed >= safetyTimeout)
        {
            // ✅ 추가: lifetime 초과 시 강제 반환
            if (visualBulletsById.ContainsKey(bulletId))
            {
                LogBulletEvent(bulletId, $"⛔ lifetime 초과 강제 반환 (elapsed:{elapsed:F2}s >= {safetyTimeout:F2}s)");
                LogManager.LogWarning(LogCategory.Projectile, 
                    $"🔴 [ID:{bulletId}] 패킷손실방지 - lifetime 초과 강제 반환 (elapsed:{elapsed:F2}s)", this);
                ReturnVisualBulletById(bulletId);
            }
            else
            {
                LogManager.Log(LogCategory.Projectile, 
                    $"✅ [ID:{bulletId}] 서버 명령으로 이미 반환됨 (lifetime 초과)", this);
            }
        }
    }
    
    private void ReturnVisualBulletById(uint bulletId)
    {
        // 🔍 디버깅: 반환 시작
        LogBulletEvent(bulletId, $"ReturnVisualBulletById 호출 - 존재여부:{visualBulletsById.ContainsKey(bulletId)}");
        
        if (visualBulletsById.TryGetValue(bulletId, out GameObject bullet))
        {
            // ✅ 추가: 코루틴 명시적 정지
            bool hadCoroutine = false;
            if (bulletCoroutines.TryGetValue(bulletId, out Coroutine coroutine))
            {
                hadCoroutine = true;
                if (coroutine != null)
                {
                    StopCoroutine(coroutine);
                    LogBulletEvent(bulletId, "🛑 코루틴 정지 완료");
                }
                else
                {
                    LogBulletEvent(bulletId, "⚠️ 코루틴 참조가 null");
                }
                bulletCoroutines.Remove(bulletId);
            }
            else
            {
                LogBulletEvent(bulletId, "⚠️ 코루틴이 Dictionary에 없음");
            }

            // Dictionary에서 제거
            visualBulletsById.Remove(bulletId);

            // 기존 컬렉션에서 제거
            if (activeVisualBulletsSet.Contains(bullet))
            {
                activeVisualBulletsSet.Remove(bullet);
                activeVisualBullets.Remove(bullet);
                bullet.SetActive(false);
                visualBulletPool.Enqueue(bullet);
                
                LogBulletEvent(bulletId, $"✅ 풀 반환 완료 - 활성:{activeVisualBullets.Count}, 풀:{visualBulletPool.Count}");
                LogManager.Log(LogCategory.Projectile, 
                    $"🔵 [ID:{bulletId}] 비주얼 총알 반환 완료 - 활성:{activeVisualBullets.Count}, 풀:{visualBulletPool.Count}, 코루틴:{(hadCoroutine ? "있음" : "없음")}", this);
            }
            else
            {
                LogBulletEvent(bulletId, "⚠️ activeVisualBulletsSet에 없음 - 강제 반환");
                LogManager.LogWarning(LogCategory.Projectile, 
                    $"⚠️ [ID:{bulletId}] Set에 없는 총알 강제 반환", this);
                bullet.SetActive(false);
                visualBulletPool.Enqueue(bullet);
            }
            
            // 🔍 생명주기 로그 출력
            PrintBulletLifecycle(bulletId);
        }
        else // ✅ 추가: 예외 상황 로깅
        {
            LogManager.LogWarning(LogCategory.Projectile, 
                $"⚠️ [ID:{bulletId}] 중복반환시도 - 이미 반환됨", this);
            
            // 혹시 코루틴만 남아있는지 확인
            if (bulletCoroutines.ContainsKey(bulletId))
            {
                LogManager.LogError(LogCategory.Projectile, 
                    $"❌ [ID:{bulletId}] 고아 코루틴 발견! 강제 정리", this);
                if (bulletCoroutines.TryGetValue(bulletId, out Coroutine orphanCoroutine) && orphanCoroutine != null)
                {
                    StopCoroutine(orphanCoroutine);
                }
                bulletCoroutines.Remove(bulletId);
            }
        }
    }

        [ObserversRpc]
        private void ReturnVisualBulletRpc(uint bulletId)
        {
            string roleText = IsServerInitialized ? "(Host)" : "(Client)";
            LogManager.Log(LogCategory.Projectile, $"📡 [ID:{bulletId}] ReturnVisualBulletRpc 수신 {roleText}", this);
            LogBulletEvent(bulletId, $"📡 RPC 수신 {roleText}");
            
            ReturnVisualBulletById(bulletId);
        }
        
        #endregion

        #region Bullet Collision & Damage
        
        public void OnBulletHit(ServerBullet bullet, GameObject target, Vector3 hitPoint)
        {
            if (!IsServerInitialized) return;

            NetworkConnection attacker = null;
            if (bullet.ownerNetworkId != 111)
            {
                InstanceFinder.ServerManager.Clients.TryGetValue((int)bullet.ownerNetworkId, out attacker);
            }

            string ownerTypeText = bullet.ownerType.ToString();
            LogManager.Log(LogCategory.Projectile, $"총알 충돌: {ownerTypeText} 총알(ID:{bullet.bulletId}) -> {target.tag}({target.name}) @ {hitPoint}", this);

            ShowBulletHitEffect(bullet.position, hitPoint, bullet.bulletId, target.tag, target.layer, bullet.speed, false);
            if (bullet.damage > 0)
            {
                AgentNetworkSync agentSync = target.GetComponent<AgentNetworkSync>();
                Shield shield = target.GetComponent<Shield>();
                if (agentSync)
                {
                    Vector2 hitDirection = bullet.GetDirection();
                    bool isCritical = agentSync.RequestTakeDamage(bullet.damage, hitDirection, attacker);
                    LogManager.Log(LogCategory.Projectile,
                        $"데미지 적용: {bullet.damage} (공격자:{attacker?.ClientId}, 타겟:{target.name})", this);


                    ShowDamageTextRpc(hitPoint, bullet.damage,
                        isCritical
                            ? DamageTextWorldManager.DamageType.critical
                            : DamageTextWorldManager.DamageType.hit);

                    if (bullet.piercing > 0)
                    {
                        bullet.piercing--;
                        return;
                    }

                    ReturnServerBullet(bullet);
                }
                else if (shield)
                {
                    if (!shield.shieldActive())
                    {
                        return;
                    }
                    Vector2 hitDirection = bullet.GetDirection();
                    
                    shield.OnDefence(bullet.damage, hitDirection, attacker);
                    LogManager.Log(LogCategory.Projectile,
                        $"방패 차감 적용: {bullet.damage} (공격자:{attacker?.ClientId}, 타겟:{target.name})", this);

                    // 플레이어 방패는 플레이어 피격 처리로 간주
                    int dmgInt = Mathf.RoundToInt(bullet.damage);
                    ShowDamageTextRpc(hitPoint, dmgInt,DamageTextWorldManager.DamageType.shield);

                    ReturnServerBullet(bullet);
                }
                else
                {
                    LogManager.Log(LogCategory.Projectile,"agentSync 없음");
                    ReturnServerBullet(bullet);
                }
            }
            else
            {
                ReturnServerBullet(bullet);
            }
        }
    
        [ObserversRpc]
        private void ShowBulletHitEffect(Vector3 bulletPos, Vector3 hitPos, uint bulletId, string targetTag, int targetLayer, float bulletSpeed, bool isCritical)
        {
            BulletImpactAudio.PlayImpactAt(hitPos, targetLayer, targetTag);
            
            Vector2 hitDirection = (hitPos - bulletPos).normalized;
            particleEffectPool.PlayAt(hitPos, hitDirection);
        }


        [ObserversRpc]
        private void ShowDamageTextRpc(Vector3 worldPos, float amount, DamageTextWorldManager.DamageType type)
        {
            var mgr = DamageTextWorldManager.Instance;
            if (!mgr) return;

            mgr.TrySpawnStamp(worldPos, amount, type);
        }

        private void ReturnServerBullet(ServerBullet bullet)
        {
            // 🔍 디버깅: 서버 총알 반환
            LogBulletEvent(bullet.bulletId, $"서버 총알 반환 시작 - 원인:{System.Environment.StackTrace.Split('\n')[1].Trim()}");
            LogManager.Log(LogCategory.Projectile, 
                $"🟡 [ID:{bullet.bulletId}] 서버 총알 반환 - RPC 전송 예정", this);
            
            // ✅ 비주얼 총알도 함께 반환 명령
            ReturnVisualBulletRpc(bullet.bulletId);

            activeBullets.Remove(bullet);
            bullet.Reset();
            bulletPool.Enqueue(bullet);
            
            LogManager.Log(LogCategory.Projectile, 
                $"🟡 [ID:{bullet.bulletId}] 서버 총알 풀 반환 완료 - 활성:{activeBullets.Count}, 풀:{bulletPool.Count}", this);
        }
        
        #endregion

        #region Unity Lifecycle (Update)
        
        private void FixedUpdate()
        {
            if (IsServerInitialized)
            {
                // 고정 틱 시간 사용 + 고속 탄환 서브스텝 분할
                float dt = Time.fixedDeltaTime;
                const float maxStep = 0.30f; // 한 번에 이동/검사할 최대 거리(월드 유닛)

                int i = 0;
                while (i < activeBullets.Count)
                {
                    ServerBullet bullet = activeBullets[i];

                    float moveDist = bullet.speed * dt;
                    int steps = Mathf.Max(1, Mathf.CeilToInt(moveDist / maxStep));
                    float stepDt = dt / steps;

                    for (int s = 0; s < steps; s++)
                    {
                        bullet.Update(stepDt);
                        if (bullet.bulletId == 0)
                            break; // 충돌 처리로 반납된 경우
                    }

                    // bullet.Update 중 리스트가 변형될 수 있으므로 안전 체크
                    if (i >= activeBullets.Count || !ReferenceEquals(activeBullets[i], bullet))
                    {
                        continue;
                    }

                    // 생명주기 종료 시 반납
                    if (bullet.IsExpired())
                    {
                        ReturnServerBullet(bullet);
                    }
                    else
                    {
                        i++;
                    }
                }
            }
        }

        private void LateUpdate()
        {
            // ✅ 매 프레임 디버그 정보 업데이트 (모든 환경에서)
            debugInfo = GetDebugInfo();

            // ✅ 주기적 무결성 검사 (선택)
            if (enableAutoValidation && Time.unscaledTime >= _nextValidationTime)
            {
                lastValidationOk = ValidatePools(out lastValidationReport);
                if (!lastValidationOk)
                {
                    LogManager.LogWarning(LogCategory.Projectile, $"BulletManager 무결성 경고:\n{lastValidationReport}", this);
                }
                _nextValidationTime = Time.unscaledTime + validationInterval;
            }
        }
        
        #endregion

        #region Debug & Statistics
        
        public void LogPoolStatus()
        {
            if (IsServerInitialized)
            {
                int totalServer = activeBullets.Count + bulletPool.Count;
                LogManager.Log(LogCategory.Projectile, $"BulletManager 서버 - 활성: {activeBullets.Count}, 풀: {bulletPool.Count}, 총계: {totalServer}/{maxPoolSize}", this);
            }
            else
            {
                int totalVisual = activeVisualBullets.Count; // HashSet은 직접 크기를 가져올 수 없으므로 List 크기를 사용
                LogManager.Log(LogCategory.Projectile, $"BulletManager 클라이언트 - 활성: {activeVisualBullets.Count}, 풀: {visualBulletPool.Count}, 총계: {totalVisual}/{maxPoolSize}", this);
            }
        }

        public PoolStats GetPoolStats()
        {
            if (IsServerInitialized)
            {
                return new PoolStats
                {
                    active = activeBullets.Count,
                    pooled = bulletPool.Count,
                    total = activeBullets.Count + bulletPool.Count,
                    maxSize = maxPoolSize,
                    utilizationRate = (float)(activeBullets.Count + bulletPool.Count) / maxPoolSize
                };
            }
            else
            {
                return new PoolStats
                {
                    active = activeVisualBullets.Count,
                    pooled = visualBulletPool.Count,
                    total = activeVisualBullets.Count + visualBulletPool.Count,
                    maxSize = maxPoolSize,
                    utilizationRate = (float)(activeVisualBullets.Count + visualBulletPool.Count) / maxPoolSize
                };
            }
        }

        public PoolDebugInfo GetDebugInfo()
        {
            if (IsServer) // 호스트
            {
                return new PoolDebugInfo
                {
                    role = "Host",
                    activeBullets = activeBullets.Count,  // 메인은 연산 총알
                    pooledBullets = bulletPool.Count,
                    totalBullets = activeBullets.Count + bulletPool.Count,
                    utilization = (float)activeBullets.Count / maxPoolSize,

                    // 상세 정보
                    serverLogicBullets = activeBullets.Count,
                    visualBullets = activeVisualBullets.Count
                };
            }
            else // 게스트
            {
                return new PoolDebugInfo
                {
                    role = "Client",
                    activeBullets = activeVisualBullets.Count,  // 메인은 시각 총알
                    pooledBullets = visualBulletPool.Count,
                    totalBullets = activeVisualBullets.Count + visualBulletPool.Count,
                    utilization = (float)activeVisualBullets.Count / maxPoolSize,

                    // 상세 정보 (게스트에서는 의미 없으므로 0)
                    serverLogicBullets = 0,
                    visualBullets = activeVisualBullets.Count
                };
            }
        }

        [ContextMenu("Validate Bullet Pools Now")]
        public void ValidateBulletPoolsNow()
        {
            lastValidationOk = ValidatePools(out lastValidationReport);
            if (lastValidationOk)
            {
                LogManager.Log(LogCategory.Projectile, "BulletManager 무결성 검사: OK", this);
            }
            else
            {
                LogManager.LogWarning(LogCategory.Projectile, $"BulletManager 무결성 검사: 문제 발견\n{lastValidationReport}", this);
            }
        }

        private bool ValidatePools(out string report)
        {
            bool ok = true;
            StringBuilder sb = new StringBuilder();

            // 서버 풀 검사
            HashSet<ServerBullet> serverActiveSet = new HashSet<ServerBullet>();
            HashSet<uint> activeIds = new HashSet<uint>();
            for (int i = 0; i < activeBullets.Count; i++)
            {
                ServerBullet b = activeBullets[i];
                if (b == null)
                {
                    ok = false; sb.AppendLine("[Server] activeBullets 에 null 항목 존재");
                    continue;
                }
                if (!serverActiveSet.Add(b))
                {
                    ok = false; sb.AppendLine("[Server] activeBullets 에 중복 참조 존재");
                }
                if (b.bulletId == 0)
                {
                    ok = false; sb.AppendLine("[Server] 활성 총알 bulletId 가 0 (반납 상태여야 함)");
                }
                if (!activeIds.Add(b.bulletId))
                {
                    ok = false; sb.AppendLine($"[Server] 활성 총알 bulletId 중복: {b.bulletId}");
                }
            }
            ServerBullet[] pooledServer = bulletPool.ToArray();
            for (int i = 0; i < pooledServer.Length; i++)
            {
                ServerBullet b = pooledServer[i];
                if (b == null)
                {
                    ok = false; sb.AppendLine("[Server] bulletPool 에 null 항목 존재");
                    continue;
                }
                if (serverActiveSet.Contains(b))
                {
                    ok = false; sb.AppendLine("[Server] 동일 총알이 active 와 pool 모두에 존재");
                }
                if (b.bulletId != 0)
                {
                    ok = false; sb.AppendLine("[Server] 풀에 있는 총알의 bulletId 가 0 아님");
                }
            }

            // 시각 풀 검사
            if (activeVisualBullets.Count != activeVisualBulletsSet.Count)
            {
                ok = false; sb.AppendLine($"[Visual] List/Set 크기 불일치: List={activeVisualBullets.Count}, Set={activeVisualBulletsSet.Count}");
            }
            for (int i = 0; i < activeVisualBullets.Count; i++)
            {
                GameObject go = activeVisualBullets[i];
                if (!go)
                {
                    ok = false; sb.AppendLine("[Visual] activeVisualBullets 에 null 항목 존재");
                    continue;
                }
                if (!activeVisualBulletsSet.Contains(go))
                {
                    ok = false; sb.AppendLine("[Visual] List 항목이 Set 에 존재하지 않음");
                }
                if (!go.activeInHierarchy)
                {
                    ok = false; sb.AppendLine("[Visual] 활성 목록의 총알이 비활성 상태");
                }
                if (visualBulletPool.Contains(go))
                {
                    ok = false; sb.AppendLine("[Visual] 동일 오브젝트가 활성과 풀에 동시에 존재");
                }
            }
            GameObject[] pooledVisual = visualBulletPool.ToArray();
            for (int i = 0; i < pooledVisual.Length; i++)
            {
                GameObject go = pooledVisual[i];
                if (!go)
                {
                    ok = false; sb.AppendLine("[Visual] visualBulletPool 에 null 항목 존재");
                    continue;
                }
                if (activeVisualBulletsSet.Contains(go))
                {
                    ok = false; sb.AppendLine("[Visual] 풀의 오브젝트가 활성 Set 에도 존재");
                }
                if (go.activeInHierarchy)
                {
                    ok = false; sb.AppendLine("[Visual] 풀의 오브젝트가 활성 상태");
                }
            }

            // ID/Coroutine 매핑 검사
            foreach (KeyValuePair<uint, GameObject> kv in visualBulletsById)
            {
                if (!kv.Value)
                {
                    ok = false; sb.AppendLine($"[Visual] visualBulletsById[{kv.Key}] 가 null");
                    continue;
                }
                if (!activeVisualBulletsSet.Contains(kv.Value))
                {
                    ok = false; sb.AppendLine($"[Visual] ID {kv.Key} 가 활성 Set 에 없음");
                }
                if (!bulletCoroutines.ContainsKey(kv.Key))
                {
                    ok = false; sb.AppendLine($"[Visual] ID {kv.Key} 의 코루틴 누락");
                }
            }
            foreach (KeyValuePair<uint, Coroutine> kv in bulletCoroutines)
            {
                if (!visualBulletsById.ContainsKey(kv.Key))
                {
                    ok = false; sb.AppendLine($"[Visual] 코루틴만 존재하고 ID 매핑 없음: {kv.Key}");
                }
            }

            report = sb.ToString();
            return ok;
        }
        
        #endregion

        #region Editor Visualization (Gizmos)
        
        private void OnDrawGizmos()
        {
            if (!enableBulletGizmos)
                return;

            if (!Application.isPlaying && !drawInEditMode)
                return;

            if (activeBullets == null || activeBullets.Count == 0)
                return;

            // 시각화를 위해 동일한 수식 사용
            const float lengthScale = 1.5f;

            for (int i = 0; i < activeBullets.Count; i++)
            {
                ServerBullet b = activeBullets[i];
                if (b == null || b.bulletId == 0)
                    continue;

                Vector3 start = b.prevPosition;
                Vector3 end = b.position;
                Vector2 delta = (Vector2)(end - start);
                float dist = delta.magnitude;

                if (drawPathLine)
                {
                    Gizmos.color = gizmoPathColor;
                    Gizmos.DrawLine(start, end);
                }

                if (drawCastVolume && dist > 0f)
                {
                    Vector2 dir = delta / dist;
                    float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                    float thickness = b.size * 0.2f;
                    float boxLength = thickness * lengthScale;
                    float totalLength = dist + boxLength;

                    // 스윕 부피의 중심
                    Vector3 center = start + (Vector3)(dir * (dist * 0.5f));

                    Matrix4x4 prev = Gizmos.matrix;
                    Gizmos.matrix = Matrix4x4.TRS(center, Quaternion.Euler(0f, 0f, angle), Vector3.one);
                    Gizmos.color = gizmoCastColor;
                    Gizmos.DrawWireCube(Vector3.zero, new Vector3(totalLength, thickness, 0.001f));
                    Gizmos.matrix = prev;
                }
            }
        }
        
        #endregion

        #region Debug Settings (Inspector)
        
        [Header("Debug Info (Runtime Only)")]
        [SerializeField] private PoolDebugInfo debugInfo;

        [Header("Gizmos (Editor Visualization)")]
        [SerializeField] private bool enableBulletGizmos = true;
        [SerializeField] private bool drawInEditMode;
        [SerializeField] private bool drawCastVolume = true;
        [SerializeField] private bool drawPathLine = true;
        [SerializeField] private Color gizmoPathColor = new Color(1f, 1f, 0f, 0.9f);
        [SerializeField] private Color gizmoCastColor = new Color(0f, 1f, 1f, 0.5f);

        [Header("Validation (Runtime)")]
        [SerializeField] private bool enableAutoValidation;
        [SerializeField] private float validationInterval = 5f;
        [SerializeField] private bool lastValidationOk = true;
        [SerializeField, TextArea] private string lastValidationReport = string.Empty;
        private float _nextValidationTime;
        
        #endregion

        #region Data Structures
        
        [System.Serializable]
        public struct PoolStats
        {
            public int active;
            public int pooled;
            public int total;
            public int maxSize;
            public float utilizationRate;

            public override string ToString()
            {
                return $"Active: {active}, Pooled: {pooled}, Total: {total}/{maxSize} ({utilizationRate:P1})";
            }
        }

        [System.Serializable]
        public class PoolDebugInfo
        {
            public string role;              // "Host", "Client"  
            public int activeBullets;        // 현재 활성 총알 수
            public int pooledBullets;        // 풀에 있는 총알 수
            public int totalBullets;         // 총 총알 수
            public float utilization;        // 사용률

            // 호스트용 상세 정보 (디버깅용)
            public int serverLogicBullets;   // 서버 연산 총알 (호스트만)
            public int visualBullets;        // 시각 총알

            public override string ToString()
            {
                if (role == "Host")
                {
                    return $"{role}: Active {activeBullets}, Pooled {pooledBullets}, Total {totalBullets}/500 ({utilization:P1}) [Logic:{serverLogicBullets}, Visual:{visualBullets}]";
                }
                else
                {
                    return $"{role}: Active {activeBullets}, Pooled {pooledBullets}, Total {totalBullets}/500 ({utilization:P1})";
                }
            }
        }

        // 🔍 디버깅: 총알 생명주기 로그
        [System.Serializable]
        public class BulletLifecycleLog
        {
            public uint bulletId;
            public float createTime;
            public float lifetime;
            public System.Collections.Generic.List<string> events;
        }
        
        #endregion

        #region Debug Helper Methods
        
        private void LogBulletEvent(uint bulletId, string eventMsg)
        {
            if (bulletLifecycleLogs.TryGetValue(bulletId, out BulletLifecycleLog log))
            {
                float elapsed = Time.time - log.createTime;
                log.events.Add($"[{Time.time:F2}] ({elapsed:F2}s) {eventMsg}");
            }
        }

        private void PrintBulletLifecycle(uint bulletId)
        {
            if (bulletLifecycleLogs.TryGetValue(bulletId, out BulletLifecycleLog log))
            {
                float totalElapsed = Time.time - log.createTime;
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                sb.AppendLine($"📋 총알 생명주기 [ID:{bulletId}]");
                sb.AppendLine($"   생존시간: {totalElapsed:F2}s / {log.lifetime:F2}s");
                sb.AppendLine($"   이벤트 수: {log.events.Count}");
                sb.AppendLine($"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                foreach (string evt in log.events)
                {
                    sb.AppendLine($"   {evt}");
                }
                sb.AppendLine($"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                LogManager.Log(LogCategory.Projectile, sb.ToString(), this);
                
                // 로그 삭제
                bulletLifecycleLogs.Remove(bulletId);
            }
        }

        [ContextMenu("Print All Active Bullet Lifecycles")]
        public void PrintAllBulletLifecycles()
        {
            LogManager.Log(LogCategory.Projectile, $"═══════════════════════════════════════", this);
            LogManager.Log(LogCategory.Projectile, $"📊 전체 총알 생명주기 출력 (총 {bulletLifecycleLogs.Count}개)", this);
            LogManager.Log(LogCategory.Projectile, $"═══════════════════════════════════════", this);
            
            foreach (var kvp in bulletLifecycleLogs)
            {
                PrintBulletLifecycle(kvp.Key);
            }
        }

        [ContextMenu("Check Orphaned Visual Bullets")]
        public void CheckOrphanedVisualBullets()
        {
            int orphanedCount = 0;
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"🔍 고아 비주얼 총알 검사 시작");
            sb.AppendLine($"   활성 비주얼 총알: {activeVisualBullets.Count}개");
            sb.AppendLine($"   매핑된 총알: {visualBulletsById.Count}개");
            sb.AppendLine($"   코루틴: {bulletCoroutines.Count}개");
            sb.AppendLine($"───────────────────────────────────");

            foreach (var kvp in visualBulletsById)
            {
                uint id = kvp.Key;
                GameObject bullet = kvp.Value;
                
                bool hasCoroutine = bulletCoroutines.ContainsKey(id);
                bool inActiveSet = activeVisualBulletsSet.Contains(bullet);
                bool isActive = bullet != null && bullet.activeInHierarchy;
                
                if (!hasCoroutine || !inActiveSet || !isActive)
                {
                    orphanedCount++;
                    sb.AppendLine($"❌ ID:{id} - 코루틴:{hasCoroutine}, Set:{inActiveSet}, Active:{isActive}");
                }
            }

            sb.AppendLine($"───────────────────────────────────");
            sb.AppendLine($"총 고아 총알: {orphanedCount}개");
            
            LogManager.Log(LogCategory.Projectile, sb.ToString(), this);
        }
        
        #endregion
    }

    [System.Serializable]
    public class ServerBullet
    {
        #region Fields
        
        // 총알 속성
        public uint bulletId;
        public Vector3 position;
        public Vector3 direction;
        public float speed;
        public float damage;
        public float lifetime;
        public float elapsed;
        public float size;
        public float piercing;
        public uint ownerNetworkId;
        public GameObject ownerGameObject;

        public Vector3 prevPosition;
        private readonly HashSet<int> hitIds = new HashSet<int>();

        public BulletOwnerType ownerType;

        private static uint nextBulletId = 1;
        
        #endregion

        #region Enums
        
        public enum BulletOwnerType
        {
            Player,     // 플레이어가 발사한 총알
            Enemy,      // 적군이 발사한 총알
            Neutral     // 중립 (환경 등)
        }
        
        #endregion

        #region Initialization Methods
        
        // 일반 총알 초기화 함수
        public void InitializeWithConnection(Vector3 startPos, float angle, float speed, float damage, float lifetime,float size,float piercing, NetworkConnection shooter)
        {
            this.bulletId = nextBulletId++; // 고유 ID 할당
            this.position = startPos;
            this.direction = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad), 0);
            this.speed = speed;
            this.damage = damage;
            this.lifetime = lifetime;
            this.size = size;
            this.piercing = piercing;
            this.elapsed = 0f;
        
            // FishNet 공식 권장: 안전한 Owner ID 추출
            this.ownerNetworkId = (uint)(shooter?.ClientId ?? 0);  // OwnerId 대신 ClientId 사용
        
            // 발사자 타입 자동 감지
            this.ownerType = DetermineOwnerType(shooter);

            this.ownerGameObject = shooter?.FirstObject.gameObject;
        }

        // 적군 총알 초기화 함수
        public void InitializeForEnemy(Vector3 startPos, float angle, float speed, float damage, float lifetime,float size,float piercing, GameObject enemyObject)
        {
            this.bulletId = nextBulletId++;
            this.position = startPos;
            this.direction = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad), 0);
            this.speed = speed;
            this.damage = damage;
            this.lifetime = lifetime;
            this.size = size;
            this.piercing = piercing;
            this.elapsed = 0f;

            // 적군은 NetworkConnection 대신 GameObject 참조 저장
            this.ownerNetworkId = 111; // 적군은 NetworkConnection이 없으므로 111
            this.ownerType = BulletOwnerType.Enemy;

            this.ownerGameObject = enemyObject;
        }
        
        #endregion

        #region Helper Methods
        
        //발사자의 타입을 재정립 후 반환
        private BulletOwnerType DetermineOwnerType(NetworkConnection shooter)
        {
            if (shooter == null) return BulletOwnerType.Neutral;
        
            // 적군 AI는 NetworkConnection이 null이거나 FirstObject가 비어있을 수 있음
            // 이 경우 GameObject를 직접 확인해야 함
            GameObject shooterObj = null;
        
            if (shooter.FirstObject)
            {
                shooterObj = shooter.FirstObject.gameObject;
            }
            else
            {
                // FirstObject가 비어있는 경우, 다른 방법으로 발사자 확인
                LogManager.Log(LogCategory.Projectile, 
                    $"발사자 NetworkConnection의 FirstObject가 비어있음. ClientId: {shooter.ClientId}");
            
                return BulletOwnerType.Enemy; // 기본적으로 적군으로 가정
            }
        
            if (shooterObj)
            {
                // 적군 컴포넌트 확인
                if (shooterObj.CompareTag("Enemy") || shooterObj.TryGetComponent(out EnemyControll controller))
                {
                    return BulletOwnerType.Enemy;
                }
            
                // 플레이어 컴포넌트 확인
                if (shooterObj.CompareTag("Player"))
                {
                    return BulletOwnerType.Player;
                }
            }


            return BulletOwnerType.Neutral;
        }

        //발사자 오브젝트 반환
        private GameObject GetOwnerGameObject()
        {
            if (ownerNetworkId > 0)
            {
                // FishNet ServerManager를 통한 Connection 조회
                if (InstanceFinder.ServerManager.Clients.TryGetValue((int)ownerNetworkId, out NetworkConnection conn))
                {
                    return conn.FirstObject?.gameObject;
                }
            }
            return null;
        }

        // 생명주기 반환
        public bool IsExpired()
        {
            return elapsed >= lifetime;
        }

        // 진행 방향 반환
        public Vector2 GetDirection()
        {
            return direction;
        }

        // 총알 초기화
        public void Reset()
        {
            bulletId = 0;
            position = Vector3.zero;
            direction = Vector3.zero;
            speed = 0f;
            damage = 0f;
            lifetime = 0f;
            elapsed = 0f;
            ownerNetworkId = 0;
            ownerType = BulletOwnerType.Neutral;
            hitIds.Clear();
        }
        
        #endregion

        #region Update & Collision Detection
        
        // 총알 위치 업데이트
        public void Update(float deltaTime)
        {
            prevPosition = position;
            position += direction * (speed * deltaTime);
            elapsed += deltaTime;

            SweepBoxEnter();
        }

        //RayCast 충돌 확인
        private void SweepBoxEnter()
        {
            LayerMask targetLayers = LayerMask.GetMask("Player","Enemy","Wall","WallSide","DestroyAbleObject","Spawner","Shield");

            Vector2 start = prevPosition;
            Vector2 move = (Vector2)(position - prevPosition);
            float dist = move.magnitude;
            if (dist <= 0f) return;

            Vector2 dir = move / dist;

            // 박스 캐스트 사이즈(가로=전방 길이 여유, 세로=총알 두께)
            float thickness = size * 0.2f;
            float lengthScale = 1.5f; // 전방 여유(터널링 방지 보정)
            Vector2 boxSize = new Vector2(thickness * lengthScale, thickness);

            // 이동 방향 각도로 회전(도 단위)
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            RaycastHit2D[] hits = Physics2D.BoxCastAll(start, boxSize, angle, dir, dist, targetLayers);
            if (hits == null || hits.Length == 0) return;

            for (int i = 0; i < hits.Length; i++)
            {
                var h = hits[i];
                var go = h.collider.gameObject;

                int id = go.GetInstanceID();
                if (hitIds.Contains(id)) continue;

                GameObject ownerObject = GetOwnerGameObject();
                if (ownerObject)
                {
                    if (go == ownerObject) continue;
                    var shield = go.GetComponent<Shield>();
                    if (shield && shield.context && shield.context.gameObject == ownerObject) continue;
                }

                if (go.CompareTag("Wall"))
                {
                    BulletManager.Instance.OnBulletHit(this, go, h.point);
                    return;
                }

                if (!ShouldHitTarget(go)) continue;

                hitIds.Add(id);
                BulletManager.Instance.OnBulletHit(this, go, h.point);

                // 매니저가 반납했는지 확인(Reset되면 bulletId=0)
                if (bulletId == 0) return;

            }
        }

        // 발사자에 따른 충돌 가능 유무 확인
        private bool ShouldHitTarget(GameObject target)
        {
            // 플레이어가 발사한 총알
            if (ownerType == BulletOwnerType.Player)
            {
                // 플레이어 총알은 적군과 플레이어 모두에게 데미지 (팀킬 가능)
                bool shouldHit = target.CompareTag("Player") || target.CompareTag("Enemy") || target.CompareTag("DestroyAbleObject")|| target.CompareTag("Spawner") || target.CompareTag("Shield");
                if (target == ownerGameObject)
                    shouldHit = false;
                if (shouldHit)
                {
                    LogManager.Log(LogCategory.Projectile, 
                        $"플레이어 총알 -> {target.tag} 허용 (팀킬 가능)");
                }
                return shouldHit;
            }
            // 적군이 발사한 총알
            if (ownerType == BulletOwnerType.Enemy)
            {
                // 적군 총알은 오직 플레이어에게만 데미지
                bool shouldHit = target.CompareTag("Player") || target.CompareTag("Shield") || target.CompareTag("DefenceObject");
                if (shouldHit)
                {
                    LogManager.Log(LogCategory.Projectile, 
                        $"적군 총알 -> {target.tag} 허용 (플레이어만)");
                }
                else if (target.CompareTag("Enemy"))
                {
                    LogManager.Log(LogCategory.Projectile, 
                        $"적군 총알 -> {target.tag} 차단 (적군끼리 맞지 않음)");
                }
                return shouldHit;
            }
            // 중립 총알 (환경 등)
            if (ownerType == BulletOwnerType.Neutral)
            {
                // 중립 총알은 모든 대상에게 데미지
                bool shouldHit = target.CompareTag("Player") || target.CompareTag("Enemy")  || target.CompareTag("Shield");
                if (shouldHit)
                {
                    LogManager.Log(LogCategory.Projectile, 
                        $"중립 총알 -> {target.tag} 허용 (모든 대상)");
                }
                return shouldHit;
            }

            return false;
        }
        
        #endregion
    }
}