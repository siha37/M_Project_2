# M-Project Prototype 2

![Unity](https://img.shields.io/badge/Unity-6-000000?style=for-the-badge&logo=unity&logoColor=white)
![C#](https://img.shields.io/badge/C%23-12-239120?style=for-the-badge&logo=c-sharp&logoColor=white)
![Python](https://img.shields.io/badge/Python-3.13+-3776AB?style=for-the-badge&logo=python&logoColor=white)

## 프로젝트 영상

### 게임플레이 데모
[![게임플레이 영상](https://img.youtube.com/vi/HGdqI_D9mH8/maxresdefault.jpg)](https://youtu.be/HGdqI_D9mH8)

---
## 학습 목표 및 시도 (Learning & Experience)

본 프로젝트는 다음과 같은 기술적 도전과 경험을 중심으로 구축되었습니다.

- **모던 네트워킹 스택의 이해와 숙달**: 
  - 네트워킹 라이브러리인 **FishNet**을 사용하여 상태 동기화 및 RPC 구조를 학습했습니다.
  - **Unity Gaming Services (UGS)**의 인증, 로비, 릴레이 시스템을 사용하여 클라우드 기반 인프라를 시도했습니다.
- **서버 아키텍처의 최적화 및 통합 시도**:
  - 초기에는 Python 커스텀 서버와 Unity P2P 방식을 병행하는 하이브리드 구조를 시도했으나, **실제 출시 환경에서의 유지보수 효율과 네트워크 동기화의 일관성**을 위해 **FishNet 전용 호스팅 방식**으로 전환했습니다.
- **모듈화 및 확장성 설계 경험**:
  - 퀘스트 시스템, 플레이어 역할 시스템 등을 독립적인 모듈로 설계하여, 거대해지는 프로젝트에서 코드의 의존성을 줄이고 재사용성을 높이는 아키텍처 설계 능력을 길렀습니다.
- **현업 수준의 데이터 워크플로우 시도**:
  - 구글 시트와 Unity 에디터를 연동하여 기획 데이터가 코드로 즉시 변환되는 파이프라인을 직접 구축하며 효율적인 협업 툴의 중요성을 체감했습니다.

## 핵심 학습 포인트

### 1. 네트워크 동기화 및 최적화
- **FishNet**을 사용하여 서버-클라이언트 간의 시간 동기화를 구현하고 필요한 만큼의 동기화를 하고자 시도했습니다.
- **UGS Lobby & Relay**를 통해 일반적으로 동적 IP를 고정적으로 유지 연결할 수 있는 법을 익혔습니다.

### 2. 복합 시스템 설계
- **Global Quest System**: 분산된 플레이어들이 하나의 목표를 공유할 때 발생하는 레이스 컨디션과 동기화 이슈를 처리하며 분산 시스템의 기본 원리를 학습했습니다.
- **Object Pooling**: 대규모 네트워크 환경에서 GC(Garbage Collector) 부하를 줄이기 위한 풀링 기법을 적용했습니다. NetworkObject의 경우 FishNet 전용 Object Polling을 이용했습니다.

### 3. Python을 통한 서버 구축 시도
- 자신의 컴퓨터를 메인 서버로 사용하여 Lobby 연결 처리를 해주는 서버를 구축했습니다. 이후 유지보수와 보안성이 우려되어 UGS의 Lobby, Relay 서비스를 이용했습니다.


## 프로젝트 구조

```text
root/
├── Assets/
│   └── MyFolder/
│       └── 1. Scripts/
│           ├── 0. System/      # 프로젝트의 심장부 (초기화 및 부트스트랩)
│           ├── 1. UI/          # 복잡한 네트워크 상태를 사용자에게 투명하게 전달하는 UI
│           ├── 4. Network/     # FishNet & UGS 통합의 핵심 로직
│           ├── 6. GlobalQuest/ # 협동 멀티플레이의 핵심 시스템
│           └── 7. PlayerRole/  # 역할 분담 및 상호작용 시스템
├── Server/                     # 초기 프로토타입용 Python 백엔드 (현재는 FishNet 호스팅으로 변경)
└── ProjectSettings/            # Unity의 다양한 설정값
```

## 사용 플러그인
- **Steamworks Integration**: Steam API를 통한 사용자 인증(Login) 및 프로필 연동.
- **Firebase Crashlytics**: 실시간 버그 리포팅 및 크래시 분석을 통한 서비스 안정성 관리.


## 사용된 기술
- **Client**: Unity 6 (6000.0.x), FishNet (Networking), UGS (Auth, Lobby, Relay, Vivox)
- **Backend**: FishNet Dedicated Hosting (Primary), Python 3.13 (Initial Prototype)
- **Tooling**: Google Sheets API Integration, Unity Editor Scripting
- **Services**: Unity Gaming Services, Firebase (Bug Reporting), Steamworks (Auth)

## 신경 쓴 점
- **Clean Architecture**: 기능을 모듈별로 분리하여 유지보수가 용이하게 설계.
- **Log Categorization**: 모든 네트워크 패킷과 로직에 카테고리별 로그를 남겨 디버깅 생산성 향상.
- **Consistent Convention**: C#의 PascalCase을 준수하여 가독성 유지.
---
이 프로젝트는 저의 기술적 한계를 마주하고 이를 극복하는 과정 자체가 소중한 자산이 된 학습 기록입니다.
