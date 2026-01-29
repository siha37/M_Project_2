# Unity 멀티플레이 UI 프리팹 가이드

## 📁 폴더 구조 예시
```
/1. UI/
├── NetworkUI/
│   ├── NetworkLobbyCanvas.prefab      # 메인 로비 캔버스 프리팹
│   ├── RoomItemPrefab.prefab          # 방 목록 아이템 프리팹
│   ├── NetworkButton.prefab           # 재사용 버튼 프리팹
│   ├── NetworkInputField.prefab       # 입력 필드 프리팹(선택)
│   ├── NetworkDropdown.prefab         # 드롭다운 프리팹(선택)
│   └── Scripts/
│       └── RoomItemController.cs      # 방 아이템 제어 스크립트
```

## 🎮 전체 UI 구조

```
NetworkLobbyCanvas (Canvas)
├── Header (타이틀/연결상태)
├── Main Panel (방 생성/방 목록)
│   ├── Left Panel (방 생성/관리)
│   └── Right Panel (방 목록/스크롤뷰)
├── Bottom Panel (상태/버튼)
└── Loading Overlay (로딩 표시)
```

## 🧩 주요 프리팹 설명

### 1. NetworkLobbyCanvas.prefab
- 전체 UI의 루트 캔버스
- Header, Main Panel, Bottom Panel, Loading Overlay 포함
- NetworkUIManager 등 관리 스크립트 연결

### 2. RoomItemPrefab.prefab
- 방 목록에 반복적으로 사용되는 아이템
- RoomItemController.cs로 데이터 바인딩
- 방 이름, 게임타입, 호스트 정보, 참가 버튼 포함

### 3. NetworkButton.prefab
- 통일된 스타일의 버튼
- TextMeshPro 텍스트 포함
- 다양한 UI에서 재사용

### 4. NetworkInputField/Dropdown (선택)
- 입력/선택 UI의 통일성 제공

## 🛠️ Inspector 연결 가이드

- **NetworkUIManager**
  - Room List Content: RoomItemPrefab이 추가될 부모 오브젝트
  - Room Item Prefab: RoomItemPrefab 연결
  - 각종 버튼/입력필드/텍스트: Inspector에서 직접 드래그
- **RoomItemController**
  - Room Name Text, Game Type Text, Host Info Text, Join Button, Background Image 연결

## 🚀 사용 예시

```csharp
// RoomItemPrefab을 Room List Content에 동적으로 추가
var item = Instantiate(roomItemPrefab, roomListContent);
var controller = item.GetComponent<RoomItemController>();
controller.SetRoomData(roomInfo);
controller.OnRoomJoinRequested += (roomId, info) => {
    // FishNet 등 네트워크 연결 처리
};
```

## 🎨 디자인 팁
- Layout Group, Content Size Fitter 적극 활용
- 색상 팔레트/폰트 스타일 통일
- 반응형 Canvas Scaler 설정
- TextMeshPro 사용 권장

## ⚠️ 참고/주의사항
- 프리팹 내 스크립트 연결 누락 주의
- RoomItemPrefab은 반드시 RoomItemController와 함께 사용
- 네트워크 매니저/로비 매니저 등과 UI 연결 필요

---

자세한 구조와 예시는 `UI_STRUCTURE_VISUALIZATION.md`를 참고하세요! 