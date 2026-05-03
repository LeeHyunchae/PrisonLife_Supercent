---
name: 시설 잠금 해제 (Gate) 룰
description: 4종 PurchaseZone 의 등장 조건. 코드(Facilities/Gates/) 와 매핑됨.
type: project
originSessionId: 2d1a4930-9bd1-4707-86bb-e20d2f74db92
---
각 PurchaseZone 의 등장 트리거. `Facilities/Gates/` 의 Gate 컴포넌트와 1:1 매핑.

| 시설 (PurchaseZone) | 등장 트리거 | Gate 클래스 | 시네마 |
|---|---|---|---|
| 무기 강화칸 | **첫 돈 획득** (`Wallet.Balance > 0`) | `FirstMoneyEarnedGate` | ✅ ON |
| 광부 일꾼칸 | **무기 1단계 도달** (`WeaponUpgradeStage >= 1`) | `WeaponStageReachedGate` | ❌ |
| 죄수 일꾼칸 | **광부 구매 완료** (`OnPurchaseCompleted`) | `PurchaseCompletedGate` | ❌ |
| 감옥 확장칸 | **감옥 만석** (`!Prison.HasFreeSlot`) | `PrisonFullGate` | ❌ |

설계: `FacilityGateBase` (추상 Template Method) → 자식이 `SubscribeToTrigger / IsConditionMet` 만 정의. 활성화 / 시네마 / 구독 해제는 base 가 처리.

**Why:** 4개 unlock 룰이 서로 다른 모델을 봐야 해서 중앙 manager 로 짤 수도 있었지만, **평가 맥락(추상화·OCP 어필)** 때문에 추상 base + 서브클래스 패턴 채택. `feedback_eval_context.md` 참조.

**How to apply:**
- 새 unlock 조건 추가 시 `FacilityGateBase` 상속한 서브클래스 1개만 추가 (기존 코드 무수정)
- 시네마 효과는 `playCinematicOnUnlock` 인스펙터 toggle. 본 구현은 `CameraDirector` 가 아직 미작성 — 작성 시 `IFacilityRevealEffect` 인터페이스 도입 검토.
- 각 PurchaseZone GameObject 에 `PurchaseZone` + 해당 `XxxGate` 컴포넌트를 함께 부착해 인스펙터에서 조립.
