---
name: 평가 과제 맥락 — 추상화·확장성 패턴 선호
description: Supercent 평가용 과제라 코드 품질이 점수에 직결. 작은 규모에도 추상화·OCP·SRP 보여주는 패턴을 단순함보다 우선.
type: feedback
originSessionId: 2d1a4930-9bd1-4707-86bb-e20d2f74db92
---
이 프로젝트는 **Supercent 평가용 과제**야. 동작뿐 아니라 코드 품질·구조·확장성도 채점 대상.

설계 선택 시 우선순위:

- **추상화 / OCP / SRP** 가 드러나는 패턴 > 단순함
- 클래스 수가 늘더라도 **확장 포인트가 명확하면 OK**
- "지금 4개니까 중앙 관리자로 충분"보다 "추상 base + 서브클래스로 새 조건 추가가 무수정 1파일이면 됨"을 선호

대표 사례 — 시설 잠금 해제(unlock) 4종 설계:
- 1차안: 중앙 `FacilityUnlockManager` 한 클래스에 4개 룰 다 박음 → **사용자 거절**
- 채택안: `FacilityGateBase` 추상 클래스 + 4개 구현(`FirstMoneyEarnedGate`, `WeaponStageReachedGate`, `PurchaseCompletedGate`, `PrisonFullGate`) → **OCP 만족, 확장성 어필**

**Why:** 평가자가 코드 리뷰할 때 "1인 실용주의로 god class 만들었네"보다 "추상 패턴 활용했네"가 점수 높음. 사용자가 명시적으로 "더 높은 점수를 받으려면 모듈/확장화가 가능한 추상클래스를 쓰는게 낫지않을까?" 라고 판단 기준을 제시함.

**How to apply:** 새 시스템 설계 시 옵션 비교에서 단순/실용 안과 추상 패턴 안을 둘 다 제시하되, **추상 패턴 안을 기본 추천**. 이 프로젝트 한정 룰이고, 일반 1인 프로젝트에선 반대로 가야 함. `feedback_poco.md` 의 "over-engineering 경계"와 충돌하지 않음 — POCO 룰은 클래스 수 자체가 아니라 MonoBehaviour 남용을 경계하는 것.
