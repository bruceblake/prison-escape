# Social & Reputation

> ⚠️ **v1 is deprecated — full replacement specced (v3).** The design of record is **[[Social Ecosystem & Gangs]]** (research-backed Respect/Trust, Standing bands Enemy→Confidant, memory & gossip, exclusive gangs + membership ladder, trading & bribes, two-way favors, snitching, guard personalities). Player UI: [[Talk Menu & NPC Profile]] · [[Social Dossier — Relationships & Gangs]]. Everything below documents what is *currently implemented* and is **slated for teardown** per the spec's teardown table. Do not extend v1.

## v1 — what exists in code today (to be removed/reworked)

Per-NPC affinity in [-100, +100]; prison-wide reputation is the average across registered inmates.

### Affinity math (`SocialMath`, unit-tested)

| Action | Base delta |
|---|---|
| Greeting | +2 |
| Favor (fallback; usually `affinityReward`) | +15 |
| Gift (default) | +5 |
| Betrayal / Theft / Snitch | -50 (or personality `betrayalPenalty`) |

Modifiers: favored gift ×2 · same-category repeat ×0.5 · **positive soft cap** `effective = base × gainMultiplier × (1 − affinity/100)` (negatives never capped) · clamp to [-100, +100].

> The soft-cap curve is the one piece of v1 math that **survives into v2** (moves into `RelationshipMath`).

### Reputation tiers (average affinity)

Outsider < 25 · Associate ≥ 25 · Respected ≥ 50 · Kingpin ≥ 75. Thresholds serialized on `SocialManager`. Tier names/thresholds are **kept** in v2; the score computation changes (standing average + gang rank bonus).

### v1 mechanics

- Greeting cooldown: one greet per NPC per phase
- One-way favors: NPCs roll a `FavorOfferDefinition` per phase; deliver item → `affinityReward`
- Interaction priority (`PrisonerSocialPresenter`): deliver favor → "needs item" → "Busy…" → Greet
- Gift/Betrayal/Theft/Snitch were API-only (no in-world UI); no personality or favor assets were ever authored

### v1 key files and their fate (from the [[Social Ecosystem & Gangs#Teardown of v1 (what "get rid of everything" means)|teardown table]])

| File | Fate |
|---|---|
| `Assets/Scripts/Shared/Prison/SocialMath.cs` | Delete (soft cap moves to `RelationshipMath`) |
| `Assets/Scripts/Shared/Prison/SocialManager.cs` | Delete |
| `Assets/Scripts/Shared/Prison/FavorOfferDefinition.cs` | Delete (→ directional `FavorDefinition`) |
| `Assets/Scripts/Shared/Prison/NPCPersonalityData.cs` | Delete (→ `ArchetypeDefinition` + traits) |
| `Assets/Scripts/Shared/Prison/PrisonerSocialPresenter.cs` | Rework → Talk Menu entry point |
| `PrisonSocialRowUI` / `AffinityFloatPopup` | Delete / keep-retitle |
| `Assets/Scripts/Editor/SocialBalanceSimulatorWindow.cs` | Rebuild for v2 math |
| `Assets/Docs/Prison_Social_And_Reputation_System.md` | Superseded by the vault spec |

## v3 — where the system is going

See **[[Social Ecosystem & Gangs]]** for the full design (v3). One-paragraph summary: every prisoner and guard gets a generated identity, archetype, and five personality traits; relationships are sparse two-axis records (**Respect** + **Trust**) with named Standing **bands** (Enemy → Confidant); NPCs hold a decaying **memory** of direct, witnessed, and gossiped events; two **gangs** (Vipers, Syndicate) claim territory with exclusive membership (Outsider → Trusted) and Traitor lockout; the [[Talk Menu & NPC Profile]] provides chat/intel, gifts, **trading** (wallet goes live), **favors both ways**, intimidation, and snitching; the notebook [[Social Dossier — Relationships & Gangs]] shows the whole web; snitches feed guard **tips** that trigger targeted shakedowns; corrupt guards take **bribes**.

This note will be rewritten as the implemented-system reference once v3 milestones land.

Related: [[Prisoner AI & NPCs]] · [[Guard AI]] · [[Inventory & Items]] · [[Loot & Economy]] · [[Time & Schedule]] · [[Talk Menu & NPC Profile]] · [[Social Dossier — Relationships & Gangs]] · [[Testing & QA]]
