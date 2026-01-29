# LoadingStatusUI 설정 가이드

## 📋 개요
로딩 씬에서 진행 상태와 오류를 실시간으로 표시하는 UI 시스템입니다.

## 🎯 주요 기능
- **실시간 상태 표시**: FishNet 연결, 방 생성, 플레이어 대기 상태
- **진행률 표시**: 프로그레스 바와 퍼센트로 진행 상황 표시
- **오류 처리**: 연결 실패 시 오류 메시지와 재시도 버튼 제공
- **상세 정보**: 네트워크 상태, 서버/클라이언트 정보 표시

## 🛠️ UI 구성 요소

### 필수 UI 컴포넌트
```
LoadingStatusUI (GameObject)
├── StatusText (TextMeshProUGUI) - 현재 상태 메시지
├── ProgressText (TextMeshProUGUI) - 진행률 퍼센트
├── ProgressBar (Slider) - 진행률 바
├── ErrorPanel (GameObject) - 오류 패널
│   ├── ErrorText (TextMeshProUGUI) - 오류 메시지
│   └── RetryButton (Button) - 재시도 버튼
```

### UI 설정 방법

1. **Canvas 생성**
   - 로딩 씬에 Canvas 추가
   - Canvas Scaler: Scale With Screen Size
   - Reference Resolution: 1920x1080

2. **LoadingStatusUI 오브젝트 생성**
   - 빈 GameObject 생성 후 `LoadingStatusUI` 스크립트 추가

3. **StatusText 설정**
   - TextMeshProUGUI 컴포넌트 추가
   - Font Size: 24
   - Alignment: Center
   - Color: White

4. **ProgressText 설정**
   - TextMeshProUGUI 컴포넌트 추가
   - Font Size: 18
   - Alignment: Center
   - Text: "0%"

5. **ProgressBar 설정**
   - Slider 컴포넌트 추가
   - Min Value: 0, Max Value: 1
   - Fill Area의 Fill 이미지 색상 설정

6. **ErrorPanel 설정**
   - Panel 오브젝트 생성
   - 기본적으로 비활성화 상태
   - 반투명 배경 (Alpha: 0.8)

7. **ErrorText 설정**
   - TextMeshProUGUI 컴포넌트 추가
   - Font Size: 16
   - Color: Red
   - Word Wrapping: Enabled

8. **RetryButton 설정**
   - Button 컴포넌트 추가
   - Text: "로비로 돌아가기"

## 🔧 스크립트 설정

### LoadingStatusUI 컴포넌트 설정
```csharp
[Header("UI Components")]
statusText = StatusText 오브젝트 할당
progressText = ProgressText 오브젝트 할당
errorText = ErrorText 오브젝트 할당
progressBar = ProgressBar 오브젝트 할당
errorPanel = ErrorPanel 오브젝트 할당
retryButton = RetryButton 오브젝트 할당

[Header("Settings")]
statusUpdateInterval = 0.5f (상태 업데이트 간격)
normalColor = White (일반 상태 색상)
warningColor = Yellow (경고 상태 색상)
errorColor = Red (오류 상태 색상)
```

## 📊 상태 표시 예시

### 정상 진행 상태
1. **초기화 중** (10%) - "네트워크 초기화 중..."
2. **Relay 연결 중** (30%) - "Unity Relay 연결 중..."
3. **방 생성 중** (50%) - "방을 생성하는 중..."
4. **플레이어 대기** (70%) - "플레이어 대기 중... (호스트)"
5. **게임 시작** (90%) - "게임 시작 중..."
6. **완료** (100%) - "로딩 완료!"

### 오류 상태
- **연결 실패**: "Unity Relay 연결 실패"
- **방 생성 실패**: "방 생성에 실패했습니다"
- **타임아웃**: "연결 시간이 초과되었습니다"

## 🎨 UI 디자인 권장사항

### 색상 팔레트
- **배경**: 어두운 색상 (#1A1A1A)
- **텍스트**: 밝은 색상 (#FFFFFF)
- **진행률 바**: 파란색 (#4A90E2)
- **경고**: 노란색 (#F5A623)
- **오류**: 빨간색 (#D0021B)

### 애니메이션
- 진행률 바: 부드러운 전환 애니메이션
- 텍스트: 페이드 인/아웃 효과
- 오류 패널: 슬라이드 인 애니메이션

## 🔍 디버깅 정보

### 표시되는 상세 정보
```
FishNet: 연결됨/연결 안됨
서버: 호스트/클라이언트
TCP: 연결됨/연결 안됨
상태: [현재 연결 상태]
```

### 로그 출력
- 모든 상태 변경은 LogManager를 통해 로그 출력
- 오류 발생 시 상세한 오류 메시지 기록

## 🚀 사용 방법

1. 로딩 씬에 LoadingStatusUI 프리팹 배치
2. 자동으로 네트워크 상태 추적 시작
3. 오류 발생 시 사용자에게 재시도 옵션 제공
4. 로딩 완료 시 자동으로 다음 씬으로 전환

## ⚠️ 주의사항

- LoadingStatusUI는 로딩 씬에서만 사용
- FishNetConnector와 TCPNetworkManager가 필요
- UI 컴포넌트가 null인 경우 해당 기능 비활성화
- 재시도 버튼은 로비 씬으로 돌아가기 기능 제공
