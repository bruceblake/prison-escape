# Social & Reputation

**Status:** Implemented (v3) on `dev` — Social micro-stack #58–#72 + Career↔Social bridge. Design of record: **[[Social Ecosystem & Gangs]]**. Player UI: [[Talk Menu & NPC Profile]] · [[Social Dossier — Relationships & Gangs]].

> v1 (affinity / `SocialManager` / `SocialMath`) was **deleted**. Do not resurrect those types. This note is the implemented-system reference.

## What exists in code today

Runtime hub: **`SocialWorld`** (`Assets/Scripts/Shared/Social/`). Builds a seeded roster of prisoners and guards, owns relationships, gangs, memory/gossip tickers, and wires Talk / Dossier / trade / favors / snitch.

### Relationships

| Concept | Implementation |
|---|---|
| Axes | **Respect** + **Trust** per directed pair, clamped [-100, +100] (`RelationshipStore` / `RelationshipMath`) |
| Soft cap | Positive deltas shrink as the axis rises; negatives never soft-capped |
| Standing bands | Derived from axes — Enemy → Confidant (`StandingBandUI` helpers) |
| Prison reputation tier | Standing average across inmates + gang-rank bonus → Outsider / Associate / Respected / Kingpin (`SocialWorld` + `RelationshipMath.ComputeTier`) |
| Memory & gossip | `SocialMemory` + `GossipSystem` — direct / witnessed / gossiped events with decay |

### Gangs

- Two exclusive gangs (Vipers, Syndicate) via `GangManager` / `GangDefinition` catalog (code fallbacks until `Resources/Social/` assets are installed).
- Membership ladder Outsider → Trusted; Traitor lockout.
- Career bridge: `SocialWorld.ApplyCareerGangTag` + Respect arrival seed from `GameManager.BuildSocialWorld` / `CareerSession`.

### Player surfaces

| Surface | Role |
|---|---|
| [[Talk Menu & NPC Profile]] | `SocialInteractionMenu` — chat/intel, gifts, trade, bribes, two-way favors, intimidation, snitch |
| [[Social Dossier — Relationships & Gangs]] | `SocialDossierUI` — notebook Relationships + Gangs pages |
| Wallet | Live: `TradingService`, bribes, `PrisonJobPaymaster`, favor costs (`PlayerWallet`) |
| Snitch tips | `SnitchSystem` → targeted shakedown hooks ([[Security, Heat & Alerts]]) |

### Career / facility hooks

- Arrival Respect seed scales with facility difficulty.
- Escape end-screen reputation reads Social standing (not deleted affinity).
- `CareerSession.DetectionRangeMult` / shakedown strictness apply in guard/shakedown systems.

## Key files

| Path | Role |
|---|---|
| `Assets/Scripts/Shared/Social/SocialWorld.cs` | Runtime hub |
| `RelationshipStore.cs` / `RelationshipMath.cs` / `SocialActs.cs` | Axes, soft cap, act deltas |
| `GangManager.cs` / `GangDefinition.cs` / `GangTerritoryMonitor.cs` | Gangs |
| `SocialMemory.cs` / `GossipSystem.cs` / `SocialSimulationTicker.cs` | Memory + tick |
| `SocialInteractionMenu.cs` / `SocialDossierUI.cs` / `StandingBandUI.cs` | UI |
| `TradingService.cs` / `TradeMath.cs` / `FavorService.cs` / `SnitchSystem.cs` | Economy & consequences |
| `ArchetypeDefinition.cs` / `SocialRosterBuilder.cs` / `SocialNameGenerator.cs` | Identities |
| `Assets/Tests/Editor/Social*.cs` | EditMode coverage for math / standing / gangs / trade |

## Remaining polish (not blockers for “Implemented”)

- [ ] Run Social asset installer → commit `Assets/Resources/Social/` ScriptableObjects (catalogs currently use code fallbacks)
- [ ] Overhead Talk markers (`!` / coin) and richer dossier widgets called out in UI notes
- [ ] Fuller per-guard Trust → detection scaling beyond facility/career multipliers
- [ ] Balance pass in Editor social simulator (rebuild for v3 math)

Related: [[Prisoner AI & NPCs]] · [[Guard AI]] · [[Inventory & Items]] · [[Loot & Economy]] · [[Time & Schedule]] · [[Talk Menu & NPC Profile]] · [[Social Dossier — Relationships & Gangs]] · [[Prison Career Ladder]] · [[Testing & QA]]
