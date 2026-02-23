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
