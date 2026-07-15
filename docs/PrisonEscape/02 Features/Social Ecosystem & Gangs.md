# Social Ecosystem & Gangs

**Status:** Specced (v2) — full replacement of the v1 social system. Awaiting your review; not yet in implementation.
**System notes:** [[Social & Reputation]] (will be rewritten to match this) · [[Prisoner AI & NPCs]] · [[Guard AI]] · [[Security, Heat & Alerts]] · [[Loot & Economy]] · [[Inventory & Items]]
**Branch:** `feat/social-ecosystem`
**Specced:** 7/14/2026. Decisions below are the design of record unless overridden in this note.

## What it is

The prison becomes a living social ecosystem. Every NPC — prisoner **and** guard — has a generated identity and personality that changes how they act, talk, and treat you. NPCs **remember** what you do (and what they *hear* you did), hold **respect** and **trust** toward you and toward each other, belong to **gangs** with territory, friends, and enemies, and offer a real interaction surface: dynamic talk menu, gifting, **trading**, favors **in both directions**, intimidation, bribing corrupt guards — and some of them **snitch**. Social standing is no longer a number on a HUD; it's protection, contraband access, intel, and cover for your escape.

## Why it exists

"Every system feeds escape" ([[Game Vision & Core Loop]]). v1 social (greet + one-way favors → affinity average) never delivered that: allies should *source tools, hold your stash through shakedowns, distract guards, warn you, and keep their mouths shut* — or rat you out if you make the wrong enemies. Gangs give the mid-game a progression ladder (Outsider → Member → Trusted) and the map a social geography (territories, rivals) that makes free time as much a puzzle as the schedule is.

## Teardown of v1 (what "get rid of everything" means)

| v1 piece | Fate |
|---|---|
| `SocialMath` single-axis affinity | **Delete** — replaced by two-axis `RelationshipMath` (the tested positive soft-cap curve `× (1 − value/100)` is kept and moved there) |
| `SocialManager` (average-affinity reputation, greet cooldown, favor rolls) | **Delete** — replaced by the services below |
| `FavorOfferDefinition` | **Delete** — superseded by directional `FavorDefinition` |
| `NPCPersonalityData` (multipliers, no assets ever authored) | **Delete** — superseded by `ArchetypeDefinition` + rolled traits |
| `PrisonerSocialPresenter` interaction priority | **Rework** — becomes the entry point for the Talk Menu |
| `PrisonSocialRowUI` affinity bars | **Delete** — replaced by Relationships notebook page |
| `AffinityFloatPopup` | **Keep** — retitled to show Trust/Respect deltas |
| `SocialBalanceSimulatorWindow` | **Rebuild** against the new math |
| `Assets/Docs/Prison_Social_And_Reputation_System.md` | **Superseded** by this note (vault wins) |

## Design details

### 1. NPC identity — everyone is someone

At world boot (`GameManager`, seeded by `worldSeed`) every NPC gets an `NPCIdentity`:

- **Name** from a curated pool (first + nick: "Eddie 'Wires' Malone") — same seed = same prison
- **Archetype** (see below), rolled **traits**, **gang affiliation** (or Independent)
- Prisoners: cell assignment. Guards: role + shift (existing spawn table)

Population target (Minimum Security, 16 cells): **15 NPC inmates** = 2 gangs × 5 + 5 independents; **all spawned guards** participate in the social layer.

### 2. Personality — traits, not multipliers

Five trait axes, 0–100, rolled per NPC inside archetype ranges:

| Trait | Drives |
|---|---|
| **Aggression** | Escalation speed, intimidation attempts, ambient arguments |
| **Loyalty** | Gang stickiness, betrayal resistance, stash-holding honesty |
| **Greed** | Trade prices, bribability, favor payment demands |
| **Sociability** | Chat frequency, gossip spread, how fast trust builds |
| **Nerve** | Willingness to break rules; **low nerve + low loyalty = snitch risk** |

**Prisoner archetypes** (`ArchetypeDefinition` SO: trait ranges + dialogue voice + behavior flags + favored gift categories + trade stock table):

| Archetype | Sketch |
|---|---|
| **Shot-Caller** | Gang leader; high loyalty/aggression; gateway to membership |
| **Soldier** | Gang muscle; follows shot-caller's disposition toward you |
| **Hustler** | The trader; high greed/sociability; best stock, hears everything |
| **Old-Timer** | Independent; high nerve, low aggression; escape lore & route intel |
| **Bruiser** | Independent muscle; respect-driven; intimidation both ways |
| **Snitch** | Low loyalty/nerve; friendly face, reports crimes to guards |
| **Loner** | Low sociability; hard to reach, but immune to gossip about you |

**Guard archetypes:**

| Archetype | Sketch |
|---|---|
| **By-the-Book** | Baseline; no bribes |
| **Corrupt** | Bribable (see §8); discovered via inmate gossip |
| **Rookie** | Vision cone 90° → **75°**; chats schedule hints |
| **Veteran** | Proximity spot 6 → **8 m**; immune to the Distraction favor |

### 3. Relationships — Respect and Trust

Every relationship (NPC↔player **and** NPC↔NPC) is a sparse record with two axes in **[-100, +100]**, start 0:

- **Respect** — status. Earned by hard favors, intimidation, gang rank, standing up for someone. Gates: intimidation success, big favor asks, gang invitations.
- **Trust** — warmth. Earned by chat, gifts, favors, time. Gates: trading discounts, warnings, stash-holding, *not* snitching on you.
- **Standing** (derived, for gate checks) = `0.6 × trust + 0.4 × respect`.

Base deltas (before modifiers):

| Action | Trust | Respect |
|---|---|---|
| Chat (1/phase/NPC) | +2 | — |
| Gift (favored ×2, repeat category ×0.5) | +5 | — |
| Favor completed for them | +10…+20 (per def) | +5 |
| Risky favor (mule, lookout) | +15 | +10 |
| Intimidation success / fail | −10 / −15 | +15 / −10 |
| They catch you stealing / you snitch on them | −40 | −20 |
| Protection favor (stood near them, it mattered) | +10 | +20 |

Modifiers, applied in order: personality (sociability scales positive trust ±25%; loyalty scales betrayal penalty ×1–2) → gang relation (see §5) → **positive soft cap** `× (1 − current/100)` (kept from v1, negatives never capped) → clamp.

**NPC↔NPC seeding:** at world gen, gang mates start +40 trust; each NPC also gets 1–2 seeded friends (+30) and 0–1 enemy (−30), never across allied lines. This is the pre-existing web you walk into.

###NOT YET: 4. Memory — NPCs remember what you did

Per-NPC ring buffer of **16** `SocialEvent`s: `{ type, actor, target, day+phase, weight 1–10, source: Direct | Witnessed | Heard }`.

- **Direct**: it happened to them (gift 2, favor 4, betrayal 10).
- **Witnessed**: they saw it — any social/crime event within **12 m + line of sight** is recorded by bystanders (crime witnessed = weight 6: contraband visible, restricted-zone entry, vent tampering, theft).
- **Heard** (gossip): copies arrive at **half weight** (see §7).
- **Decay**: −1 weight per in-game day (at Morning Roll Call); at 0 → forgotten. **Grudges** (negative events with weight ≥ 8) decay at −1 per **3** days.
- Memory feeds dialogue ("Saw you crawl out of that vent, man"), snitch rolls, trade prices, and favor availability. Buffer full → lowest-weight event evicted.

### 5. Gangs

Two gangs at Minimum Security (+ Independents):

| Gang | Territory (claim zone) | Flavor |
|---|---|---|
| **Vipers** | Yard — weight pit corner | Muscle; respect-first culture |
| **Syndicate** | Cafeteria — back corner tables | Smugglers/traders; trust-and-greed culture |


### 6. Interactions — the Talk Menu

Raycast-interact on any NPC opens a real-time **Talk Menu** (no pause). Options appear/hide by relationship, archetype, gang, and phase:

| Option | Gate | Effect |
|---|---|---|
| **Chat** | 1/phase/NPC | +2 trust; pulls a dialogue line from their memory/gossip — this is the **intel channel** (see bands below) |
| **Gift** | item in hand | Trust per §3 |
| **Trade** | archetype has stock | Barter/cash UI (§8) |
| **Ask favor** | per-favor gates (below) | You spend cash/standing, they act |
| **Do favor** | they have one open | Evolved v1 favors: fetch, delivery/mule, protection, sabotage |
| **Intimidate** | Respect roll: your (respect + strength stat) vs their nerve | Success: +15 respect, they comply / shut up (§9); fail: −10 respect, they may report you |
| **Snitch** | targeting another inmate, told to a guard | Guard shakes down the target's cell; +10 guard trust; witnesses/gossip wreck inmate trust (−40 with target's friends) |

**Chat intel bands** (per NPC trust): < 25 flavor only · ≥ 25 schedule/guard hints · ≥ 50 loot & route hints · ≥ 75 escape lore ("vent behind cell 12 ain't welded").

**Asking favors** (player → NPC) — the new direction:

| Favor | Gate | Cost | Delivery |
|---|---|---|---|
| **Lookout** | trust ≥ 25 | $10 or item | Warns you when a guard is within 20 m, one phase |
| **Distraction** | respect ≥ 25 | $15 | Fake argument pulls the nearest guard ~30 s (Veterans immune) |
| **Source item** | trust ≥ 40, Hustler or gang | 1.5 × item price | Requested category delivered in 1–2 days |
| **Hold stash** | trust ≥ 60, not a Snitch | free | Holds 2 items through your shakedown; **loyalty < 40 → 25% they keep one** |
| **Silence a snitch** | gang **Trusted** | gang standing −10 | Member intimidates a named snitch: mute for 3 days |

### 7. Gossip — the prison telephone

During meals and Free Time, each NPC with sociability ≥ 50 shares its **highest-weight memory** with one nearby NPC whose trust ≥ 25. Copies land at half weight, source `Heard`, and can hop again (quarter weight, max 2 hops). Consequences: your deeds (good and bad) spread through friendship networks — **and a Snitch archetype who *hears* about your crime can report it** (§9). Gossip is also how you learn things: chat can surface *who snitched on you*, *which guard is corrupt*, and *who's friends with whom*.

### 8. Trading & bribes — the wallet finally works

- **Stock**: refreshed daily at Morning Count from the archetype's `TradeStockDefinition` (Hustler 4–6 items incl. rare/contraband; Soldiers/others 0–2). Trading naturally concentrates in the **17:00–21:00 yard & recreation block** — the commissary window of the new schedule ([[Time & Schedule]]).
- **Price** = base value × greed factor (0.8–1.5) × trust discount (up to −25% at trust ≥ 75) × gang modifier (member 0.85; **rivals refuse to trade** below standing −25) × **contraband markup ×2**.
- Pay with **cash** (`PlayerWallet` — first real sink/source, see [[Loot & Economy]]) or barter items of matching value.
- **Bribes** (Corrupt guards only): **$25** clear one crime tip against you · **$40** skip your cell in the next shakedown · **$60** blind-eye (that guard's detection off for one phase). Bribing in view of witnesses creates a `BribeWitnessed` memory — leverage for snitches.
## Systems it touches

[[Time & Schedule]] (phase-gated interactions, daily decay/stock ticks) · [[Prisoner AI & NPCs]] (identity, archetypes replace `NPCPersonalityData`) · [[Guard AI]] (guard archetypes, per-player detection modifiers, tips) · [[Security, Heat & Alerts]] (tips → targeted shakedown + heat) · [[Roll Call & Shakedown]] (targeted/skipped cells) · [[Inventory & Items]] (trade, gifts, stash holding) · [[Loot & Economy]] (wallet live: trade, bribes, favor fees) · [[UI & HUD]] (Talk Menu, Relationships notebook page, barks) · [[Escape Routes & Mechanics]] (intel bands, sourced tools, lookouts/distractions as escape enablers)

## Data & tuning (designer-facing, all SO/serialized)

`ArchetypeDefinition` (trait ranges, voice, stock, favored gifts, snitch base) · `GangDefinition` (territory, relations, initiation pool) · `FavorDefinition` (direction, gates, costs, timers) · `TradeStockDefinition` · `DialogueTable` (template lines with token substitution, keyed archetype voice × relationship band × memory type — no LLM) · relationship deltas & soft cap · memory size/decay/witness radius · gossip rates · snitch propensities · bribe prices · tier thresholds.

## Planned architecture (new, `Assets/Scripts/Shared/Social/`)

`NPCIdentity` · `PersonalityTraits` · `ArchetypeDefinition` · `RelationshipStore` (sparse pair records) · `RelationshipMath` (pure, tested) · `SocialMemory` (ring buffer, pure decay logic) · `SocialEventBus` (all systems publish; memory/gossip subscribe) · `GossipSystem` · `GangDefinition`/`GangManager` · `TradingService` · `FavorService` (two-way) · `SnitchSystem` · `SocialSimulationTicker` · `SocialInteractionMenu` (UI). Deterministic from `worldSeed`.

## Milestones

1. **M1 Foundation** — v1 teardown; identity/traits/archetypes; `RelationshipStore` + math; seeding. *(EditMode-heavy)*
2. **M2 Talking** — Talk Menu; Chat/Gift; memory buffer; dialogue tables; Relationships notebook page.
3. **M3 Gangs** — gang SOs, territories + warn-offs, standing propagation, membership ladder + initiation favors.
4. **M4 Economy** — trading UI + stock; wallet wiring; bribes.
5. **M5 Consequences** — favors both directions; gossip; snitching + tips → targeted shakedown.
6. **M6 Guards & balance** — guard archetypes + trust modifiers; ambient ticker; simulator window rebuild; balance pass.

Each milestone is playable and individually shippable to `dev`.

## Test plan

- **EditMode (pure seams):** relationship delta pipeline + soft cap, standing formula, memory decay/eviction/grudges, gossip weight halving + hop cap, snitch propensity modifiers, trade price formula, gang standing propagation ×0.5/×1.0, membership gate checks, tier computation, favor gate/cost validation.
- **Manual/PlayMode per milestone:** chat intel bands unlock at 25/50/75; territory warn-off at standing < 0; initiation flow → Member; buy contraband → shakedown finds it → snitch tip loop; bribe skips cell; Traitor lockout.

## Out of scope (v2)

- Fights/combat (intimidation reads the existing Strength stat; no melee system)
- Gang wars / scripted events between gangs
- Guard corruption investigations, guard↔guard social sim
- Romance; multiplayer social layer; LLM/generative dialogue
- More than 2 gangs (Medium Security can add a third)

## Open questions for you

1. **Gang names/flavor** — "Vipers" & "Syndicate" are placeholders; happy to rename.
2. **Intimidation fail** — current spec: fail risks them reporting you; do you want a fight/scuffle stub instead (even without combat)?
3. **Snitch visibility** — should the Snitch archetype ever be *guaranteed* discoverable (e.g. Old-Timer always knows), or gossip-only?
4. **Cash sources** — trading/bribes need income; current implied sources are selling loot to Hustlers and gang payouts for favors. Enough, or add prison jobs?

---

_After implementation: update this note if the design changed, mark status Implemented, and log it in [[Prison Escape Devlog Dashboard]]._
