---
name: POCO over MonoBehaviour preference
description: User strongly prefers plain C# (POCO) classes over MonoBehaviour, except when Unity engine callbacks force it.
type: feedback
originSessionId: 2cd4a679-6803-495d-b8d9-b9cc8e71fbc7
---
For all game-logic classes in this Unity project, default to plain C# classes (POCO). Use MonoBehaviour only when forced by Unity engine: OnTriggerEnter/Exit, OnCollisionEnter, OnEnable/OnDisable, Animation Events, or when [SerializeField] inspector exposure of children/refs is the only practical option.

**Why:** User finds POCO easier to trace through code ("코드 추적이 쉬워서"). Mono proliferation also adds inspector clutter, GameObject overhead, and ambiguous lifecycle. They explicitly rejected an earlier structure that had many small Mono controllers per Player as wasteful.

**How to apply:**
- Player/Prisoner/Worker-style entities: one Mono "facade" component on the root that owns POCO subsystem instances and delegates Update to them.
- Engine-callback components (trigger detectors, animation event receivers): keep as Mono on a dedicated child GameObject and expose plain C# events upward so the entity facade subscribes.
- Movers, controllers, sub-system logic: POCO with constructor DI.
- Folder names like `Controllers/` are kept for semantic grouping even when their contents are POCO (not Mono).
- Avoid creating a new MonoBehaviour just to hold a few [SerializeField] fields if those fields can live on the entity facade with the logic in a POCO subsystem.
