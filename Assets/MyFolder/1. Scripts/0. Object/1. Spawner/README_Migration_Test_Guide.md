# 멀티플레이어 적 스포너 시스템 테스트 가이드

## 🎯 시스템 개요

**NetworkEnemyManager + NetworkSpawnerObject**를 사용한 서버 기반 적 스폰 시스템

✅ **완료된 구성요소:**
1. ✅ NetworkEnemyManager 구현 (서버 기반 적 관리)
2. ✅ NetworkSpawnerObject 구현 (서버 전용 스폰)
3. ✅ 기존 EnemyManager 제거 (직접 NetworkEnemyManager 호출)

## 📋 테스트 절차

### **사전 준비사항 ✅**
- [x] Enemy.prefab에 NetworkObject 컴포넌트 확인됨
- [x] Enemy.prefab 설정: `_isNetworked: 1`, `_isSpawnable: 1`
- [x] NetworkEnemyManager.cs 스크립트 준비 완료
- [x] NetworkSpawnerObject.cs 스크립트 준비 완료

### **1단계: NetworkEnemyManager 씬에 배치**

**MainScene.unity에서:**
1. 빈 GameObject 생성 → 이름: "NetworkEnemyManager"
2. NetworkEnemyManager.cs 스크립트 추가
3. NetworkObject 컴포넌트 추가
4. **NetworkObject 설정:**
   - ✅ `Is Networked`: true
   - ✅ `Is Spawnable`: true  
   - ✅ `Is Global`: **true** (중요!)
   - ✅ `Initialize Order`: 0
   - ✅ `Prevent Despawn On Disconnect`: true

### **2단계: 테스트용 NetworkSpawnerObject 배치**

**MainScene.unity에서:**
1. 빈 GameObject 생성 → 이름: "TestNetworkSpawner"
2. NetworkSpawnerObject.cs 스크립트 추가
3. NetworkObject 컴포넌트 추가
4. **NetworkObject 설정:**
   - ✅ `Is Networked`: true
   - ✅ `Is Spawnable`: true
   - ❌ `Is Global`: false (일반 네트워크 객체)
5. **NetworkSpawnerObject 설정:**
   - `Enemy Prefab`: Enemy 프리팹 할당
   - `Spawn Interval`: 5초
   - `Spawn Delay`: 2초  
   - `Max Spawn Count`: 3 (테스트용 소량)
   - `Enable Debug Logs`: true

### **3단계: 테스트 실행**

**호스트 테스트:**
1. Play 모드 시작
2. 호스트로 게임 시작 (Ready 또는 MainScene에서)
3. **콘솔 로그 확인순서:**
   ```
   [NetworkEnemyManager] 인스턴스 생성 완료
   [NetworkEnemyManager] 서버 초기화 완료
   [NetworkSpawnerObject - TestNetworkSpawner] 서버에서 네트워크 스포너 시작
   [NetworkSpawnerObject - TestNetworkSpawner] NetworkEnemyManager 연결 완료
   [NetworkEnemyManager] 스포너 추가됨. 최대 적 수량: 5
   [NetworkSpawnerObject - TestNetworkSpawner] 초기 지연 대기: 2초
   [NetworkSpawnerObject - TestNetworkSpawner] 네트워크 스폰 완료: Enemy(Clone)
   [NetworkSpawnerObject - TestNetworkSpawner] 적 스폰 및 초기화 완료: 1/3
   [NetworkEnemyManager] 적 생성됨. 현재 적 수량: 1/5
   ```

**게스트 테스트 (별도 빌드 또는 Editor + Build):**
1. 게스트로 호스트에 연결
2. **게스트 콘솔 확인:**
   ```
   [NetworkEnemyManager] 클라이언트 동기화 설정 완료
   [NetworkSpawnerObject - TestNetworkSpawner] 클라이언트에서 스포너 비활성화
   [NetworkEnemyManager] 현재 적 수량 변경: 0 → 1 (서버: false)
   ```

### **4단계: 검증 항목**

**✅ 성공 확인사항:**
- [ ] 호스트에서만 적이 스폰됨 (서버 전용)
- [ ] 게스트에서도 스폰된 적이 동기화되어 보임
- [ ] 적 수량 카운터가 양쪽에서 동일하게 표시됨
- [ ] 적이 올바른 타겟(플레이어)을 추적함
- [ ] 최대 3마리 제한이 올바르게 작동함
- [ ] Scene View에서 스포너 기즈모가 올바른 색상으로 표시됨 (서버=녹색, 클라이언트=빨간색)

**❌ 실패 시 확인사항:**
- NetworkEnemyManager가 GlobalObject로 설정되었는지
- Enemy 프리팹에 모든 필요한 컴포넌트가 있는지 (EnemyControll, NetworkObject 등)
- FishNet NetworkManager가 씬에 올바르게 설정되었는지
- 콘솔에 에러 로그가 없는지

## 🐛 예상 문제 및 해결방안

### **문제 1: "NetworkEnemyManager 초기화 타임아웃"**
- **원인**: NetworkEnemyManager가 GlobalObject로 설정되지 않음
- **해결**: NetworkObject의 `Is Global` 체크박스 확인

### **문제 2: "ServerManager를 찾을 수 없습니다"**
- **원인**: FishNet NetworkManager가 씬에 없거나 초기화 안됨
- **해결**: NetworkManager 오브젝트가 씬에 있는지 확인

### **문제 3: "Enemy 프리팹에 NetworkObject 컴포넌트가 없습니다"**
- **원인**: Enemy.prefab에 NetworkObject 컴포넌트 누락
- **해결**: Enemy.prefab 열어서 NetworkObject 컴포넌트 추가

### **문제 4: 적이 생성되지만 AI가 동작하지 않음**
- **원인**: EnemyControll의 초기화 실패 또는 타겟 설정 실패
- **해결**: PlayerManager.Instance.GetPlayer() 반환값 확인

## 🎯 다음 단계

테스트 성공 후:
1. 기존 로컬 스포너들을 NetworkSpawnerObject로 교체
2. 전체 시스템 통합 테스트
3. 멀티플레이어 환경에서 대량 적 스폰 테스트
4. 성능 모니터링 및 최적화

## 🏗️ 시스템 아키텍처

### **최종 구조 (단순화됨)**
```
멀티플레이어 적 스폰 시스템

NetworkEnemyManager (GlobalObject)
├─ 적 수량 전역 관리
├─ 네트워크 동기화 (SyncVar)
└─ 서버 권한 관리

NetworkSpawnerObject (개별 스포너)  
├─ 개별 스포너 로직
├─ NetworkEnemyManager 직접 호출
└─ InstanceFinder.ServerManager.Spawn()
```

### **호출 흐름**
```
NetworkSpawnerObject → NetworkEnemyManager (직접 호출)
                    → InstanceFinder.ServerManager (스폰용)
                    → PlayerManager (타겟용)
```

**→ 매우 단순하고 명확한 멀티플레이어 전용 구조!** 🎉 