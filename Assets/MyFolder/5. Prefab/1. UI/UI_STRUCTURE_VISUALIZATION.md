# Unity UI Prefab 구조 시각화

## 🎮 전체 UI 구조

```
📁 NetworkLobbyCanvas (Canvas)
├── 📁 Background
│   └── 🖼️ Background Image
├── 📁 Header
│   ├── 🏷️ Title Text ("멀티플레이 로비")
│   └── 📊 Connection Status Text ("연결 상태: 연결됨")
├── 📁 Main Panel
│   ├── 📁 Left Panel (방 생성/관리)
│   │   ├── 🏷️ Section Title ("방 생성")
│   │   ├── 📝 Room Name Input ("방 이름 입력")
│   │   ├── 📝 Max Players Input ("최대 플레이어 수")
│   │   ├── 📋 Game Type Dropdown ("게임 타입")
│   │   ├── 🔘 Create Room Button ("방 생성")
│   │   ├── 🔘 Delete Room Button ("방 삭제")
│   │   └── 🏷️ Current Room Text ("현재 방: R1234567890")
│   │
│   ├── 📁 Right Panel (방 목록)
│   │   ├── 🏷️ Section Title ("방 목록")
│   │   ├── 🔘 Refresh Button ("새로고침")
│   │   ├── 📁 Room List ScrollView
│   │   │   ├── 📁 Viewport
│   │   │   │   └── 📁 Content (Room Items Container)
│   │   │   │       ├── 📁 Room Item Prefab
│   │   │   │       │   ├── 🖼️ Background Image
│   │   │   │       │   ├── 🏷️ Room Name Text ("테스트 방 (3/8)")
│   │   │   │       │   ├── 🏷️ Host Info Text ("192.168.1.100:7777")
│   │   │   │       │   └── 🔘 Join Button ("참가")
│   │   │   │       └── 📁 Room Item Prefab (반복...)
│   │   │   └── 📁 Scrollbar
│   │   └── 🏷️ Room Count Text ("총 5개 방")
│   │
│   └── 📁 Bottom Panel (상태/정보)
│       ├── 🏷️ Status Text ("방 목록을 불러오는 중...")
│       ├── 🏷️ Heartbeat Status ("하트비트: 15.2초 전")
│       └── 🔘 Connect Button ("서버 연결")
└── 📁 Loading Overlay (비활성화)
    ├── 🖼️ Background Image
    └── 🏷️ Loading Text ("로딩 중...")
```

## 🎨 UI 컴포넌트 상세 구조

### 📁 NetworkLobbyCanvas
```
Canvas (Screen Space - Overlay)
├── Canvas Scaler (UI Scale Mode: Scale With Screen Size)
├── Graphic Raycaster
└── NetworkUIManager Script
```

### 📁 Header Section
```
Header Panel (Horizontal Layout Group)
├── Title Text (TextMeshPro)
│   ├── Font: Roboto-Bold
│   ├── Font Size: 32
│   └── Color: White
└── Connection Status Text (TextMeshPro)
    ├── Font: Roboto-Regular
    ├── Font Size: 16
    └── Color: Green/Red (연결 상태에 따라)
```

### 📁 Left Panel - 방 생성
```
Left Panel (Vertical Layout Group)
├── Section Title
│   ├── Text: "방 생성"
│   ├── Font Size: 24
│   └── Font Style: Bold
├── Room Name Input (TMP_InputField)
│   ├── Placeholder: "방 이름을 입력하세요"
│   ├── Character Limit: 20
│   └── Validation: Not Empty
├── Max Players Input (TMP_InputField)
│   ├── Placeholder: "최대 플레이어 수"
│   ├── Content Type: Integer Number
│   ├── Min Value: 2
│   └── Max Value: 16
├── Game Type Dropdown (TMP_Dropdown)
│   ├── Options: ["마피아", "레이싱", "FPS", "RPG"]
│   └── Default Value: 0
├── Create Room Button (Button)
│   ├── Text: "방 생성"
│   ├── Image: Clean Button Style
│   └── Interactable: false (초기)
├── Delete Room Button (Button)
│   ├── Text: "방 삭제"
│   ├── Image: Clean Button Style
│   └── Interactable: false (초기)
└── Current Room Text (TextMeshPro)
    ├── Text: "현재 방: 없음"
    ├── Font Size: 14
    └── Color: Gray
```

### 📁 Right Panel - 방 목록
```
Right Panel (Vertical Layout Group)
├── Section Header (Horizontal Layout Group)
│   ├── Section Title
│   │   ├── Text: "방 목록"
│   │   ├── Font Size: 24
│   │   └── Font Style: Bold
│   └── Refresh Button (Button)
│       ├── Text: "새로고침"
│       ├── Icon: Refresh Icon
│       └── Interactable: true
├── Room List ScrollView (ScrollRect)
│   ├── Viewport (Mask)
│   │   └── Content (Vertical Layout Group)
│   │       ├── Room Item Prefab (Button)
│   │       │   ├── Background Image
│   │       │   ├── Room Info Panel (Vertical Layout Group)
│   │       │   │   ├── Room Name Text
│   │       │   │   │   ├── Text: "테스트 방 (3/8)"
│   │       │   │   │   ├── Font Size: 18
│   │       │   │   │   └── Font Style: Bold
│   │       │   │   └── Host Info Text
│   │       │   │       ├── Text: "호스트: 192.168.1.100:7777"
│   │       │   │       ├── Font Size: 14
│   │       │   │       └── Color: Gray
│   │       │   └── Join Button (Button)
│   │       │       ├── Text: "참가"
│   │       │       ├── Image: Clean Button Style
│   │       │       └── Size: Small
│   │       └── Room Item Prefab (반복...)
│   └── Scrollbar (Vertical)
└── Room Count Text (TextMeshPro)
    ├── Text: "총 0개 방"
    ├── Font Size: 14
    └── Color: Gray
```

### 📁 Bottom Panel - 상태 정보
```
Bottom Panel (Horizontal Layout Group)
├── Status Text (TextMeshPro)
│   ├── Text: "서버에 연결되지 않음"
│   ├── Font Size: 16
│   └── Color: Orange
├── Heartbeat Status (TextMeshPro)
│   ├── Text: "하트비트: 비활성"
│   ├── Font Size: 14
│   └── Color: Gray
└── Connect Button (Button)
    ├── Text: "서버 연결"
    ├── Image: Clean Button Style
    └── Interactable: true
```

## 🎯 Room Item Prefab 상세 구조

```
📁 Room Item Prefab (Button)
├── 🖼️ Background Image
│   ├── Color: White (기본)
│   ├── Hover Color: Light Blue
│   └── Selected Color: Blue
├── 📁 Content (Horizontal Layout Group)
│   ├── 📁 Info Panel (Vertical Layout Group)
│   │   ├── 🏷️ Room Name Text
│   │   │   ├── Text: "테스트 방 (3/8)"
│   │   │   ├── Font: Roboto-Bold
│   │   │   ├── Font Size: 18
│   │   │   └── Color: Black
│   │   ├── 🏷️ Game Type Text
│   │   │   ├── Text: "게임 타입: 마피아"
│   │   │   ├── Font: Roboto-Regular
│   │   │   ├── Font Size: 14
│   │   │   └── Color: Gray
│   │   └── 🏷️ Host Info Text
│   │       ├── Text: "호스트: 192.168.1.100:7777"
│   │       ├── Font: Roboto-Regular
│   │       ├── Font Size: 12
│   │       └── Color: Dark Gray
│   └── 🔘 Join Button (Button)
│       ├── 🏷️ Button Text
│       │   ├── Text: "참가"
│       │   ├── Font: Roboto-Bold
│       │   ├── Font Size: 16
│       │   └── Color: White
│       ├── 🖼️ Button Image
│       │   ├── Color: Green
│       │   ├── Hover Color: Light Green
│       │   └── Pressed Color: Dark Green
│       └── Button Component
│           ├── OnClick: JoinRoom(roomId)
│           └── Interactable: true
└── 📁 Scripts
    └── RoomItemController
        ├── roomId: string
        ├── roomInfo: RoomInfo
        └── OnJoinButtonClicked()
```

## 🎨 색상 팔레트

### 기본 색상
- **Primary Blue**: #2196F3
- **Success Green**: #4CAF50
- **Warning Orange**: #FF9800
- **Error Red**: #F44336
- **Background Gray**: #F5F5F5
- **Text Dark**: #212121
- **Text Gray**: #757575

### 상태별 색상
- **Connected**: #4CAF50 (Green)
- **Disconnected**: #F44336 (Red)
- **Connecting**: #FF9800 (Orange)
- **Heartbeat Active**: #2196F3 (Blue)
- **Heartbeat Inactive**: #757575 (Gray)

## 📱 반응형 레이아웃

### 화면 크기별 설정
```
📱 Mobile (320px - 768px)
├── Header: 60px 높이
├── Main Panel: Flex (1:1 비율)
├── Left Panel: 100% 너비
├── Right Panel: 100% 너비
└── Bottom Panel: 50px 높이

💻 Desktop (768px+)
├── Header: 80px 높이
├── Main Panel: Flex (1:2 비율)
├── Left Panel: 300px 고정 너비
├── Right Panel: Flex (나머지)
└── Bottom Panel: 60px 높이
```

## 🔧 Inspector 설정 가이드

### NetworkUIManager 컴포넌트 설정
```
NetworkUIManager (Script)
├── UI References
│   ├── Connect Button: [Connect Button]
│   ├── Create Room Button: [Create Room Button]
│   ├── Refresh Button: [Refresh Button]
│   ├── Delete Room Button: [Delete Room Button]
│   ├── Room Name Input: [Room Name Input]
│   ├── Max Players Input: [Max Players Input]
│   ├── Game Type Dropdown: [Game Type Dropdown]
│   ├── Status Text: [Status Text]
│   ├── Room ID Text: [Room ID Text]
│   ├── Connection Status Text: [Connection Status Text]
│   ├── Room List Content: [Room List Content]
│   └── Room Item Prefab: [Room Item Prefab]
└── Settings
    ├── Auto Refresh: true
    └── Refresh Interval: 5
```

### Canvas 설정
```
Canvas
├── Render Mode: Screen Space - Overlay
├── Pixel Perfect: true
├── Sort Order: 0
├── Target Display: Display 1
├── Plane Distance: 100
├── Additional Shader Channels: None
├── Canvas Scaler
│   ├── UI Scale Mode: Scale With Screen Size
│   ├── Reference Resolution: 1920 x 1080
│   ├── Screen Match Mode: Match Width Or Height
│   └── Match: 0.5
└── Graphic Raycaster
    ├── Blocking Objects: None
    ├── Blocking Mask: Everything
    └── Event Camera: None
```

## 🎮 사용자 경험 (UX) 고려사항

### 시각적 피드백
- **버튼 호버**: 색상 변화 + 스케일 애니메이션
- **로딩 상태**: 스피너 애니메이션
- **연결 상태**: 실시간 색상 변경
- **오류 메시지**: 빨간색 텍스트 + 아이콘

### 접근성
- **키보드 네비게이션**: Tab 키로 이동
- **스크린 리더**: 적절한 레이블 설정
- **색상 대비**: WCAG 2.1 AA 기준 준수
- **터치 타겟**: 최소 44x44px

### 성능 최적화
- **오브젝트 풀링**: Room Item 재사용
- **레이아웃 그룹**: 효율적인 UI 배치
- **이미지 압축**: 적절한 텍스처 압축
- **폰트 아틀라스**: 텍스트 렌더링 최적화 