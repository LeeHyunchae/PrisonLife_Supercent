---
name: 프로젝트 아키텍처 합의 (MVP + NavMesh + UniTask 등)
description: Prison Life 모작의 코어 패턴·라이브러리·DI 전략 합의. 신규 코드는 이 결정을 따라야 함.
type: project
originSessionId: 2d1a4930-9bd1-4707-86bb-e20d2f74db92
---
확정된 아키텍처 결정:

- **패턴**: MVP 통일 (UI + 게임플레이 동일 패턴)
  - View 는 passive (dumb 인터페이스 + Set... 메서드)
  - Presenter 가 (Model, View) 1쌍 중재. 작게 쪼개기, god Presenter 금지
  - 거대 트랜잭션은 Service 계층(`ItemFlowService` 등)으로 격리
- **이동**: NavMesh — `com.unity.ai.navigation` 1.1.5 패키지. 직선 이동 폐기
  - Player 는 `NavMeshAgent.Move()` (조이스틱 입력)
  - AI(일꾼/죄수)는 `SetDestination` + `UniTask.WaitUntil(arrived)`
  - 감옥 확장 시 `NavMeshSurface.BuildNavMesh()` 런타임 재베이크
- **비동기**: UniTask (Cysharp). 코루틴 안 씀.
- **반응성**: 자체 `ReactiveProperty<T>` (30~50줄 자체 구현). 부족 시 R3 로 교체 가능하게 인터페이스 호환.
- **DI**: `SystemManager` 단일 진입점 (`[DefaultExecutionOrder(-1000)]` 정적 `Instance`)
  - **POCO Model/Service**: `SystemManager.Awake()` 에서 코드로 `new`. 인스펙터 노출 X.
  - **MonoBehaviour Service / View / Presenter**: 씬에 배치, 인스펙터 `[SerializeField]` 주입.
  - 인스펙터 DI 컨테이너(Zenject 등) 안 씀.
- **에셋 위치 분리**:
  - SO 클래스 정의: `Assets/Project/Scripts/Configs/`
  - SO 인스턴스(.asset): `Assets/Configs/` (스크립트와 격리)

**Why:** 1차 MVVM 검토에서 Controller 책임이 god class 가 될 위험을 사용자가 지적. MVP 로 통일하면서 Presenter 가 작게 쪼개지도록 가이드. NavMesh / UniTask / SystemManager DI 는 사용자가 명시 합의.

**How to apply:**
- 새 시설/액터 추가 시 Model + View + Presenter 3종 구조 따름
- View 는 ReactiveProperty 를 직접 알지 못하게 — Presenter 가 구독 후 `view.SetXxx()` 호출
- 두 Model 동시 변경은 Service 로 빼기
- 정적 `SystemManager.Instance.X` 접근은 Spawn 헬퍼 / 임시 도구에만, Presenter 내부에선 `Init(...)` 으로 받은 참조 사용
