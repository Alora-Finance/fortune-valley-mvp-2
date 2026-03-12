# Fortune Valley — Proof of Concept (Claude Rules)

## 0. Primary Objective (Non-Negotiable)
This project is a Unity proof-of-concept to test ONE hypothesis:

"A game that requires financial knowledge to progress causes students to learn."

All decisions must optimize for:
- Conceptual clarity
- Learning signal strength
- Development speed
- Low complexity

This is NOT a production build.

---

## 1. Game Summary
Genre: 2.5D idle-clicker / strategy  
Perspective: Isometric / 2.5D  
Player Role: Restaurant owner expanding into the city  
Core Tension: Spend money now vs invest to grow money over time

Win Condition:
- Player acquires all city lots before rival AI

Lose Condition:
- Rival AI acquires all city lots first

---

## 2. Core Learning Outcomes (Must Be Explicit)
The game must surface these concepts clearly and repeatedly:

- Opportunity cost
- Compound interest
- Time value of money
- Risk vs reward

The player should be able to verbally explain:
- WHY an investment decision helped or hurt them
- WHY waiting sometimes beats immediate spending

---

## 3. Core Systems (POC Scope Only)

### 3.1 Restaurant Income System
- Generates currency every X seconds
- Predictable, low-risk baseline income
- Serves as the "safe but slow" option

### 3.2 Investment System (Learning Core)
- Supports multiple financial instruments
- Explicit compounding logic
- Player can see:
  - Principal
  - Gains / losses
  - Time invested
  - Rate of return
- Outcomes must be explainable in plain language

### 3.3 Rival Expansion System
- AI competitor buying city lots
- Applies time pressure
- Difficulty increases per level
- Forces meaningful trade-offs

---

## 4. Architectural Principles (Strict)

- Favor **clarity over abstraction**
- Prefer **event-driven systems** over Update polling
- Use **ScriptableObjects** for tunable data
- Avoid global singletons unless explicitly justified
- Keep systems loosely coupled and testable
- Code must be readable by a junior Unity developer

### 4.1 Layer Structure (Required)
All new code must be assigned to one of these layers:

| Layer | Namespace | Folder Path | Rule |
|-------|-----------|-------------|------|
| Enums | `FortuneValley.Domain.Enums` | `Assets/Scripts/Domain/Enums/` | Pure C# enums only. No logic. |
| Entities | `FortuneValley.Domain.Entities` | `Assets/Scripts/Domain/Entities/` | Pure C# classes. No UnityEngine imports. |
| Managers | `FortuneValley.Managers` | `Assets/Scripts/Managers/` | MonoBehaviour orchestrators only. |
| Systems | `FortuneValley.Core` | `Assets/Scripts/Core/` | MonoBehaviour game systems. |
| UI | `FortuneValley.UI` | `Assets/Scripts/UI/` | UI components and panels. |

> **Migration note:** `Assets/Scripts/V2.0/` is a legacy path. All domain files must live under `Assets/Scripts/Domain/`. The V2.0 folder must be fully migrated before any feature that references it is merged.

### 4.2 One Type Per File
- Every class, enum, or interface gets its own `.cs` file
- File name must match the type name exactly
- No bundling multiple types in one file (the existing GameState-inside-GameManager.cs pattern must not be repeated)

### 4.3 Domain Layer Must Be Unity-Free
- Files in `FortuneValley.Domain.*` must NOT import UnityEngine
- No MonoBehaviour, no Mathf, no Debug, no Vector types
- This ensures domain logic is unit-testable without booting Unity

### 4.4 No Duplicate Types
- Never leave two versions of the same class or enum coexisting
- If migrating a type, remove the old one in the same PR/commit
- Coexisting duplicates (e.g., two ActiveInvestment classes) are a hard block on merging

### 4.5 Layer Dependency Rules
Layers may only import from layers below them. No upward or circular imports. Ever.

```
UI            --> Core, Domain
Managers      --> Core, Domain
Core          --> Domain
Domain        --> (nothing)
```

- Domain is the foundation. It imports nothing.
- Core may use Domain types but not Managers or UI.
- Managers may use Core and Domain but not UI.
- UI may use Core and Domain. UI must NOT call into Managers directly -- use events.
- If you need to cross a boundary that this table forbids, the answer is an event, not an import.

### 4.6 Naming Conventions
Consistent naming is required so any developer can navigate the project without a guide.

| Type | Convention | Example |
|------|------------|---------|
| Classes | PascalCase | `ActiveInvestment` |
| Enums | PascalCase | `GameState` |
| Enum values | PascalCase | `GameState.Playing` |
| Interfaces | `I` prefix + PascalCase | `IInvestmentProvider` |
| Private fields | `_camelCase` | `_currentBalance` |
| Events / Actions | `On` prefix + PascalCase | `OnBalanceChanged` |
| ScriptableObjects | Descriptive + `Config` or `Data` suffix | `RestaurantConfig`, `StockData` |
| Files | Must match the type name exactly | `ActiveInvestment.cs` |

### 4.7 Assembly Definitions (Required for Scalability)
Each layer must have its own `.asmdef` file. This turns the dependency rules in 4.5 from
a convention into a compile-time hard error.

| Layer | Assembly Name | File Location |
|-------|---------------|---------------|
| Domain | `FortuneValley.Domain` | `Assets/Scripts/Domain/` |
| Core | `FortuneValley.Core` | `Assets/Scripts/Core/` |
| Managers | `FortuneValley.Managers` | `Assets/Scripts/Managers/` |
| UI | `FortuneValley.UI` | `Assets/Scripts/UI/` |

Without `.asmdef` files: dependency rules are enforced by code review (easy to miss).
With `.asmdef` files: a forbidden import is a compile error (impossible to ship).

---

## 5. Unity-Specific Rules

- Use C#
- Use MonoBehaviour appropriately
- Use serialized private fields, not public fields
- Do NOT modify:
  - `.unity` scene files
  - prefab files
  - ProjectSettings
  unless explicitly instructed
- Assume mobile-friendly performance constraints

---

## 6. Claude Behavior Rules

- Ask before making architectural changes
- Do NOT add features beyond scope
- Explain financial logic in simple, student-friendly terms
- When writing code:
  - Include short comments explaining intent
  - Avoid clever or dense implementations
- When unsure, propose options with trade-offs
- Never use em dashes in any output, comments, copy, or documentation

---

## 7. POC Success Criteria (Anchor All Decisions)

This POC is successful if:
1. A student can explain opportunity cost using the game
2. A student can describe compound interest effects they observed
3. Winning or losing clearly correlates with financial decisions

If a feature does not support these outcomes, it should be excluded.

---

## 8. Interaction Guidelines

- Design systems before writing code
- Build one system at a time
- Keep changes small and reviewable
- Optimize for learning clarity, not content volume

---

## 9. Known Issues & Deprecation Fixes

### 9.1 Namespace Conflicts
| Issue | Fix | Rationale |
|-------|-----|-----------|
| `FortuneValley.Camera` conflicts with `UnityEngine.Camera` | Renamed to `FortuneValley.CameraControl` | Unity types take precedence |

**Prevention Rule**: Never create namespaces matching Unity type names (Camera, Input, UI, Physics, etc.)

### 9.2 Input System Migration
- Project uses **New Input System** exclusively
- `InputSystemFixer.cs` auto-replaces deprecated `StandaloneInputModule` with `InputSystemUIInputModule`
- Use `Mouse.current`, `Keyboard.current`, `Touchscreen.current` - not legacy `Input` class

### 9.3 Modern Unity APIs
| Deprecated | Use Instead |
|------------|-------------|
| `FindObjectOfType<T>()` | `FindFirstObjectByType<T>()` |
| `FindObjectsOfType<T>()` | `FindObjectsByType<T>(FindObjectsSortMode.None)` |

### 9.4 Full Namespace Qualification
When inside FortuneValley namespace, use full qualification for ambiguous types:
```csharp
private UnityEngine.Camera _camera;  // Good - explicit
```
