# Feature Spec Template

Copy this structure into a new note when designing a feature. Delete sections that don't apply, but keep the numbers.

---

## <Feature Name>

**Status:** Idea / Specced / In Progress / Implemented / Shipped
**System note:** link to the `03 Systems/` note this belongs to (or create one)
**Branch:** `feat/...`

### What it is
One paragraph: what the player experiences.

### Why it exists
How it serves the core loop (escape, compliance, social, economy — see [[Game Vision & Core Loop]]).

### Design details
- Concrete mechanics with numbers (timings, ranges, costs, thresholds)
- Diagrams / canvas embeds for anything spatial
- Edge cases and rules

### Systems it touches
- [[Time & Schedule]] / [[Guard AI]] / [[Inventory & Items]] / ... (link every affected system note)

### Data & tuning
Which values should be designer-tunable (serialized fields / ScriptableObjects)?

### Test plan
- Pure logic → EditMode test targets
- Scene behavior → manual playtest steps or PlayMode

### Out of scope
What this feature deliberately does NOT include.

---

_After implementation: update this note if the design changed, mark status Implemented, and log it in [[Prison Escape Devlog Dashboard]]._
