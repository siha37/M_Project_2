# NetworkEnemyManager 씬 설정 가이드

## 📋 설정 단계

### 1단계: NetworkEnemyManager 오브젝트 생성
1. MainScene.unity 열기
2. Hierarchy에서 빈 GameObject 생성
3. 이름을 "NetworkEnemyManager"로 변경
4. NetworkEnemyManager.cs 스크립트 추가
5. NetworkObject 컴포넌트 추가

### 2단계: NetworkObject 설정
NetworkObject 컴포넌트에서 다음 설정:
- ✅ `Is Networked`: true
- ✅ `Is Spawnable`: true  
- ✅ `Is Global`: true (중요! 씬 전환 시에도 유지됨)
- ✅ `Initialize Order`: 0 (우선 초기화)
- ✅ `Prevent Despawn On Disconnect`: true

### 3단계: 초기화 순서 보장
- NetworkEnemyManager는 스포너들보다 먼저 초기화되어야 함
- FishNet GlobalObject로 설정하면 자동으로 우선 초기화됨
- 다른 NetworkBehaviour들이 OnStartServer에서 NetworkEnemyManager.Instance에 접근 가능

### 4단계: 테스트 확인사항
- 호스트 시작 시 NetworkEnemyManager가 서버에서 먼저 생성되는지 확인
- 게스트 연결 시 동기화된 적 수량 정보를 받는지 확인
- NetworkSpawnerObject가 NetworkEnemyManager를 직접 호출하는지 확인

## 🔧 디버그 콘솔 확인사항

정상 작동 시 다음 로그들이 순서대로 출력되어야 함:

```
[NetworkEnemyManager] 인스턴스 생성 완료
[NetworkEnemyManager] 서버 초기화 완료
[NetworkEnemyManager] 클라이언트 동기화 설정 완료
[NetworkSpawnerObject] NetworkEnemyManager 연결 완료
[NetworkEnemyManager] 스포너 추가됨. 최대 적 수량: 5
[NetworkEnemyManager] 적 생성됨. 현재 적 수량: 1/5
```

## ⚠️ 주의사항

1. **DontDestroyOnLoad 사용 금지**: NetworkBehaviour에서는 FishNet GlobalObject 기능 사용
2. **초기화 순서**: NetworkEnemyManager가 스포너들보다 먼저 초기화되어야 함
3. **씬 전환**: GlobalObject 설정으로 씬 전환 시에도 유지됨
4. **직접 호출**: EnemyManager 중간 레이어 없이 NetworkEnemyManager 직접 호출

## 🎯 아키텍처 개요

### **단순화된 구조**
```
NetworkSpawnerObject → NetworkEnemyManager (직접 호출)
                    → InstanceFinder.ServerManager (스폰용)
                    → PlayerManager (타겟용)
```

### **장점**
- ✅ **중간 레이어 제거**: 불필요한 EnemyManager 제거
- ✅ **성능 향상**: 직접 호출로 오버헤드 감소
- ✅ **명확한 구조**: 의존성이 직접적이고 명확함
- ✅ **멀티플레이어 전용**: 단일 목적으로 단순화

## 🚀 다음 단계

NetworkEnemyManager 설정 완료 후:
1. NetworkSpawnerObject 구현 및 테스트
2. 전체 시스템 통합 테스트
3. 멀티플레이어 환경에서 성능 검증 