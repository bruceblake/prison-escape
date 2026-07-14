# Social & Reputation

Per-NPC affinity in [-100, +100]; prison-wide reputation is the average across registered inmates. Social standing exists to unlock help for escapes (favors, perks, allies).

Design depth doc: `Assets/Docs/Prison_Social_And_Reputation_System.md` (v1.2 — secondary to this vault).

## Affinity math (`SocialMath`, fully unit-tested)

| Action | Base delta |
|---|---|
| Greeting | **+2** |
| Favor (fallback; usually `affinityReward`) | **+15** |
| Gift (default) | **+5** |
| Betrayal / Theft / Snitch | **-50** (or personality `betrayalPenalty`) |

Modifiers:
- **Favored gift** × 2 (item in the NPC's `favoredItems`)
- **Same-category gift repeat** × 0.5 (favored items exempt)
- **Positive soft cap:** `effective = base × gainMultiplier × (1 − affinity/100)` — gains shrink as affinity rises; negatives are never capped
- Final clamp to [-100, +100]

## Reputation tiers (average affinity)

| Tier | Threshold |
|---|---|
| Outsider | < 25 |
| Associate | ≥ 25 |
| Respected | ≥ 50 |
| Kingpin | ≥ 75 |

Thresholds serialized on `SocialManager`. **Personality perk** unlocks per-NPC at affinity ≥ **50** (`OnPersonalityPerkUnlocked` event — Broker/Narc/Bully perk handlers not yet implemented).

## Mechanics

- **Greeting cooldown:** one successful greet per NPC per schedule phase (anti-spam)
- **Favors:** on start/phase change, each NPC may roll a valid `FavorOfferDefinition` (filtered by phase + personality). Deliver the required item → consume 1 from inventory → `affinityReward` (default +15). Unfinished favors re-roll next phase.
- **Interaction priority** (`PrisonerSocialPresenter`): Deliver favor item → "needs item" → "Busy…" (cooldown) → Greet

## UI

- `PrisonSocialRowUI` — affinity bar, fill = (affinity+100)/200, snitch hint text
- `AffinityFloatPopup` — +N float popup (1.1 s)
- `SocialBalanceSimulatorWindow` — **Tools → Prison → Social Balance Simulator** editor preview of the exact math

## Implementation gaps (facts)

- Gift / Betrayal / Theft / Snitch are **API-ready but have no in-world UI** — only Greet and Favor delivery are playable
- **No `FavorOfferDefinition` or `NPCPersonalityData` assets authored yet** — content work needed
- Global reputation averages **registered** prisoners (not normalized to 8 as the old doc suggests)

## Key files

| File | Role |
|---|---|
| `Assets/Scripts/Shared/Prison/SocialMath.cs` | Pure math (tested) |
| `Assets/Scripts/Shared/Prison/SocialManager.cs` | Orchestration singleton |
| `Assets/Scripts/Shared/Prison/FavorOfferDefinition.cs` | Favor SO |
| `Assets/Scripts/Shared/Prison/PrisonerSocialPresenter.cs` | In-world interaction |
| `Assets/Scripts/Editor/SocialBalanceSimulatorWindow.cs` | Balance tool |

Related: [[Prisoner AI & NPCs]] · [[Inventory & Items]] · [[Time & Schedule]] · [[Testing & QA]]
