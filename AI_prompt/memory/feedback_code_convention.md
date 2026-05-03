---
name: C# code conventions for this project
description: Naming and style rules to follow in all C# code authored for this Unity project.
type: feedback
originSessionId: 2cd4a679-6803-495d-b8d9-b9cc8e71fbc7
---
- Classes, public methods, public members, constants: **PascalCase**
- Private fields and local variables: **camelCase** (no underscore prefix)
- Method parameters: **`_camelCase`** (underscore + camelCase), e.g. `_amount`, `_targetPosition`
- Variable names: prefer descriptive names; longer names are fine if clarity improves (`oreStackCount` over `cnt`).
- **`private` 접근 제어자는 항상 명시**. C# 의 default-private 생략 규칙에 의존하지 말 것. 필드·메서드 모두 적용.
  ```csharp
  // ✗ 생략 금지
  NavMeshAgent navMeshAgent;
  void DoSomething() { ... }

  // ✓ 명시
  private NavMeshAgent navMeshAgent;
  private void DoSomething() { ... }
  ```
- **`var` 지양** — 타입을 명시적으로 적기. 예외적으로 LINQ 익명 타입처럼 표기 불가능한 경우만 허용.
  ```csharp
  // ✗ 지양
  var inventory = new InventoryModel(initialCapacities);
  var lookDirection = new Vector3(_velocity.x, 0f, _velocity.z);

  // ✓ 권장
  InventoryModel inventory = new InventoryModel(initialCapacities);
  Vector3 lookDirection = new Vector3(_velocity.x, 0f, _velocity.z);
  ```

**Why:** 매개변수 `_` 접두는 사용자가 필드 vs 매개변수 구분을 한눈에 보기 위함. `private` 명시는 접근 제어 의도가 코드 리뷰·평가 시 모호하지 않게 드러나도록 사용자가 명시적으로 요구함.

**How to apply:** 모든 신규 파일·기존 코드 편집 시 적용. 기존 코드에 누락된 `private` 키워드를 발견하면 같이 추가. 룰 위반은 프로젝트 전역 적용이므로 파일 단위로 예외 두지 말 것.
