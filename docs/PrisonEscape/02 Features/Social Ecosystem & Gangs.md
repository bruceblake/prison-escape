# Social Ecosystem & Gangs

**Status:** Implemented in review — Social micro-stack PRs #58�#72 (stack onto `dev`). Playtest after merge.
**System notes:** [[Social & Reputation]] (v1 deprecated; rewrite after implement) · [[Talk Menu & NPC Profile]] · [[Social Dossier — Relationships & Gangs]] · [[Prisoner AI & NPCs]] · [[Guard AI]] · [[Security, Heat & Alerts]] · [[Loot & Economy]] · [[Inventory & Items]]
**Branch:** `feat/social-ecosystem`
**Specced:** v2 7/14/2026 · **v3** 7/16/2026 (research basis, friends/enemies bands, complete gang membership, Talk + dossier UI). Decisions below are the design of record unless overridden in this note.

## What it is

The prison becomes a living social ecosystem. Every NPC — prisoner **and** guard — has a generated identity and personality that changes how they act, talk, and treat you. NPCs **remember** what you do (and what they *hear* you did), hold **respect** and **trust** toward you and toward each other, belong to **gangs** with territory, friends, and enemies, and offer a real interaction surface: tabbed Talk Menu, gifting, **trading**, favors **in both directions**, intimidation, bribing corrupt guards — and some of them **snitch**. Standing bands (Enemy → Confidant) and a notebook **social dossier** make the whole web readable. Social standing is protection, contraband access, intel, and cover for your escape.

## Why it exists

"Every system feeds escape" ([[Game Vision & Core Loop]]). v1 social (greet + one-way favors → affinity average) never delivered that: allies should *source tools, hold your stash through shakedowns, distract guards, warn you, and keep their mouths shut* — or rat you out if you make the wrong enemies. Gangs give the mid-game a progression ladder (Outsider → Trusted) and the map a social geography (territories, rivals) that makes free time as much a puzzle as the schedule is.

## Research basis (v3)

Competitive and real-world inputs that shaped this design. We did not copy any one game wholesale.

### Real prison social order

Inmates create extralegal governance when staff cannot fully police life inside (Skarbek; classic "inmate code"). Early order runs on **norms** — don't snitch, don't steal from peers, show toughness — enforced by gossip, ostracism, and reputation. As populations grow and anonymity rises, **gangs** centralize that governance: protection, dispute resolution, and enforcement of contraband markets. Shot-callers sit at the top; membership is sticky; exit is costly.

**Game translation:** Respect (status/fear) and Trust (who keeps quiet / holds deals) stay as **separate axes**. Gangs govern territory and trade access. Snitches break the code and get punished socially. Leaving a gang = Traitor lockout.

### The Escapists / The Escapists 2

| Mechanic | Their game | Ours |
|---|---|---|
| Single **Opinion** + name colors | Chat, gifts, favors; green/neutral/red tags | World **nameplate tints** by Standing band; two axes instead of one Opinion |
| **Profile tabs** (stats / gift / shop / favor) | Interact → profile overlay | Tabbed [[Talk Menu & NPC Profile]] |
| Favors (`!` marker) | Fetch, deliver, beat-up, craft | Fetch, delivery/mule, protection, lookout, sabotage — **no beat-up** (no combat) |
| Trade (coin marker) | Stock + opinion discounts | Stock + trust discount + gang modifiers |
| Guard opinion | Fewer desk searches / less following | Bribes + per-guard detection modifiers |
| Gangs | Essentially absent | **Beyond** Escapists — exclusive membership (Back to Dawn direction) |
| Recruit allies to fight | Opinion ≥ 80 | **Skipped** — combat out of scope |

### Back to the Dawn

| Mechanic | Their game | Ours |
|---|---|---|
| Rapport + gift prefs | Chat once, then gifts; love 2× / like 1.5×; prefs reveal over time | Gift categories + discovery fog; soft-cap on positives kept |
| **Exclusive gangs** (3) | Errands → join one → cut off others; gang shops; territory facilities | **2 gangs** at MinSec; exclusive join; Syndicate under-bed store; territory corners |
| Separate relationship / faction UIs | `N` / `Y` keys + inmate info cards | Diegetic notebook [[Social Dossier — Relationships & Gangs]] (Tab) |
| Bond → unique skills | RPG specialty tree | **Deferred** — escape-feeding favors first |
| Extortion / intel sales | Prestige skills | Soft-match: Intimidate + Chat intel bands |

### Synthesis

```
Real life (governance, code, shot-callers)
        + Escapists (talk / gift / trade / favors / nameplates)
        + Back to Dawn (exclusive gangs, dossier depth)
        → Respect+Trust · Memory+Gossip · 2 exclusive gangs · Talk Menu · Notebook dossier
```

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

Population target (Minimum Security / Dev Sandbox, 16 cells): **15 NPC inmates** = 2 gangs × 5 + 5 independents; **all spawned guards** participate in the social layer.

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
| **Old-Timer** | Independent; high nerve, low aggression; escape lore & route intel; can name known Snitches at Chat trust ≥ 50 |
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

### 3. Relationships — Respect, Trust, and Standing bands

Every relationship (NPC↔player **and** NPC↔NPC) is a sparse record with two axes in **[-100, +100]**, start 0:

- **Respect** — status. Earned by hard favors, intimidation, gang rank, standing up for someone. Gates: intimidation success, big favor asks, gang invitations.
- **Trust** — warmth. Earned by chat, gifts, favors, time. Gates: trading discounts, warnings, stash-holding, *not* snitching on you.
- **Standing** (derived, for gate checks and UI bands) = `0.6 × trust + 0.4 × respect`.

#### Friends / Enemies bands (player-facing)

| Band | Standing | Player-facing |
|---|---|---|
| **Enemy** | ≤ −50 | Red nameplate; may refuse Talk / warn others / tip more readily |
| **Hostile** | −49…−25 | Orange; bad trade prices; territory tension |
| **Neutral** | −24…+24 | Default |
| **Friendly** | +25…+49 | Chat intel unlocks start (≥ 25 trust band for schedule hints) |
| **Ally** | +50…+74 | Lookout / better favors / trade discount |
| **Confidant** | ≥ +75 | Stash-hold, silence, escape lore |

Filters on the Relationships dossier page use: **Friends** = Friendly+Ally+Confidant · **Enemies** = Hostile+Enemy.

Base deltas (before modifiers):

| Action | Trust | Respect |
|---|---|---|
| Chat (1/phase/NPC) | +2 | — |
| Gift (favored ×2, liked ×1.5, repeat category ×0.5) | +5 base | — |
| Favor completed for them | +10…+20 (per def) | +5 |
| Risky favor (mule, lookout) | +15 | +10 |
| Intimidation success / fail | −10 / −15 | +15 / −10 |
| They catch you stealing / you snitch on them | −40 | −20 |
| Protection favor (stood near them, it mattered) | +10 | +20 |

Modifiers, applied in order: personality (sociability scales positive trust ±25%; loyalty scales betrayal penalty ×1–2) → gang relation (see §5) → **positive soft cap** `× (1 − current/100)` (kept from v1, negatives never capped) → clamp.

**NPC↔NPC seeding:** at world gen, gang mates start +40 trust; each NPC also gets 1–2 seeded friends (+30) and 0–1 enemy (−30), never across allied lines. This is the pre-existing web you walk into.

Prison-wide **reputation tier** (kept from v1 names): average Standing across known inmates + gang rank bonus → Outsider &lt; 25 · Associate ≥ 25 · Respected ≥ 50 · Kingpin ≥ 75. Shown on the dossier header; used for arrival treatment under [[Prison Career Ladder]].

### 4. Memory — NPCs remember what you did

Per-NPC ring buffer of **16** `SocialEvent`s: `{ type, actor, target, day+phase, weight 1–10, source: Direct | Witnessed | Heard }`.

- **Direct**: it happened to them (gift 2, favor 4, betrayal 10).
- **Witnessed**: they saw it — any social/crime event within **12 m + line of sight** is recorded by bystanders (crime witnessed = weight 6: contraband visible, restricted-zone entry, vent tampering, theft).
- **Heard** (gossip): copies arrive at **half weight** (see §7).
- **Decay**: −1 weight per in-game day (at Morning Roll Call); at 0 → forgotten. **Grudges** (negative events with weight ≥ 8) decay at −1 per **3** days.
- Memory feeds dialogue ("Saw you crawl out of that vent, man"), snitch rolls, trade prices, and favor availability. Buffer full → lowest-weight event evicted.

### 5. Gangs

Two gangs at Minimum Security / Dev Sandbox (+ Independents). Working names — flavor rename later is fine.

| Gang | Territory (claim zone) | Flavor |
|---|---|---|
| **Vipers** | Yard — weight pit corner | Muscle; respect-first culture |
| **Syndicate** | Cafeteria — back corner tables | Smugglers/traders; trust-and-greed culture |

#### Gang standing

- **Gang standing** toward the player = average Standing with living members of that gang.
- Propagation when you help/hurt a member: other members shift by **×0.5** (Outsider / Associate) or **×1.0** (once you are Member/Trusted of that gang).
- Rivals refuse to trade below gang standing **−25**.

#### Membership ladder (exclusive)

| Rank | Gate | Perks |
|---|---|---|
| **Outsider** | Default | Territory warn-off if gang standing &lt; 0 |
| **Associate** | Gang standing ≥ 25 | Shot-Caller may offer initiation; soft territory tolerance |
| **Member** | Complete initiation favor for Shot-Caller | Free use of gang territory amenity; member trade price 0.85; gang favors unlock |
| **Trusted** | Member + gang standing ≥ 60 + **2** completed gang favors | Silence-a-snitch; best store stock; strongest propagation |

**Exclusive join (Back to Dawn model):** you may be Associate of both, but **Member of only one**. Joining Vipers locks Syndicate membership (and vice versa) for the rest of this facility run. You can still Chat / Gift rivals; Trade refuses below −25 standing.

**Initiation:** Shot-Caller offers a directional favor (mule, lookout, or silence). Accept → complete within timer → Member. Refuse or fail → cool-down **2 in-game days** before re-offer; small Respect hit (−5) with Shot-Caller.

**Traitor lockout:** leaving after Member (or betraying the gang — snitching on a member, stealing from gang stash, attacking Shot-Caller socially) → **Traitor**: −80 Trust and −80 Respect with all ex-gang members; rival gang standing **+20**; **no rejoin** that facility run. Career carry still records a Traitor tag (see Career carry).

**Territory:**

- Standing &lt; 0 as non-member → ambient **warn-off** bark when entering claim zone; Soldiers may Intimidate once per phase.
- Members: free use of amenity (Vipers weight pit corner / Syndicate back tables) without warn-off.

**Gang store (Syndicate first):** Member+ can buy from gang stock via Talk → Trade (or dossier link). Items deliver **under your bed next morning after headcount** (Back to Dawn pattern). Vipers store can share the same pipeline later with muscle/contraband skew. Dev Sandbox first; other facilities get chapter-specific `GangDefinition`s.

### 6. Interactions — the Talk Menu

Raycast-interact on any NPC opens a real-time **Talk Menu** (no pause). Full UI layout: [[Talk Menu & NPC Profile]].

Options appear/hide by relationship, archetype, gang, and phase:

| Option | Gate | Effect |
|---|---|---|
| **Chat** | 1/phase/NPC | +2 trust; pulls a dialogue line from their memory/gossip — this is the **intel channel** (see bands below) |
| **Gift** | item in hand or gift slot | Trust per §3 |
| **Trade** | archetype has stock | Barter/cash UI (§8) |
| **Ask favor** | per-favor gates (below) | You spend cash/standing, they act |
| **Do favor** | they have one open | Evolved v1 favors: fetch, delivery/mule, protection, sabotage |
| **Intimidate** | Respect roll: your (respect + strength stat) vs their nerve | Success: +15 respect, they comply / shut up (§9); **fail: −10 respect + risk they report you** (no fight stub) |
| **Snitch** | targeting another inmate, told to a **guard** | Guard shakes down the target's cell; +10 guard trust; witnesses/gossip wreck inmate trust (−40 with target's friends) |

**Chat intel bands** (per NPC trust): &lt; 25 flavor only · ≥ 25 schedule/guard hints · ≥ 50 loot & route hints · ≥ 75 escape lore ("vent behind cell 12 ain't welded").

**Asking favors** (player → NPC) — the new direction:

| Favor | Gate | Cost | Delivery |
|---|---|---|---|
| **Lookout** | trust ≥ 25 | $10 or item | Warns you when a guard is within 20 m, one phase |
| **Distraction** | respect ≥ 25 | $15 | Fake argument pulls the nearest guard ~30 s (Veterans immune) |
| **Source item** | trust ≥ 40, Hustler or gang | 1.5 × item price | Requested category delivered in 1–2 days |
| **Hold stash** | trust ≥ 60, not a Snitch | free | Holds 2 items through your shakedown; **loyalty &lt; 40 → 25% they keep one** |
| **Silence a snitch** | gang **Trusted** | gang standing −10 | Member intimidates a named snitch: mute for 3 days |

### 7. Gossip — the prison telephone

During meals and Free Time, each NPC with sociability ≥ 50 shares its **highest-weight memory** with one nearby NPC whose trust ≥ 25. Copies land at half weight, source `Heard`, and can hop again (quarter weight, max 2 hops). Consequences: your deeds (good and bad) spread through friendship networks — **and a Snitch archetype who *hears* about your crime can report it** (§9). Gossip is also how you learn things: chat can surface *who snitched on you*, *which guard is corrupt*, and *who's friends with whom*.

**Snitch discovery (locked):** gossip-first. Old-Timer Chat at trust ≥ 50 can name known Snitch archetypes you have already met or heard of — not a guaranteed always-on reveal of every rat.

### 8. Trading & bribes — the wallet finally works

- **Stock**: refreshed daily at Morning Count from the archetype's `TradeStockDefinition` (Hustler 4–6 items incl. rare/contraband; Soldiers/others 0–2). Trading naturally concentrates in the **17:00–21:00 yard & recreation block** — the commissary window of the new schedule ([[Time & Schedule]]).
- **Price** = base value × greed factor (0.8–1.5) × trust discount (up to −25% at trust ≥ 75) × gang modifier (member 0.85; **rivals refuse to trade** below standing −25) × **contraband markup ×2**.
- Pay with **cash** (`PlayerWallet` — first real sink/source, see [[Loot & Economy]]) or barter items of matching value.
- **Bribes** (Corrupt guards only): **$25** clear one crime tip against you · **$40** skip your cell in the next shakedown · **$60** blind-eye (that guard's detection off for one phase). Bribing in view of witnesses creates a `BribeWitnessed` memory — leverage for snitches.

**Cash sources (locked):** sell loot to Hustlers + favor payouts + **one light job** wired when the economy milestone lands (enough for bribes/trade without a full job career sim). Coin item wires to wallet at M4.

**Gift preference discovery:** favored categories start hidden. Revealed when you successfully gift that category, or when gossip/Chat surfaces a hint. Shown on Talk Profile and dossier detail (fog until known) — Escapists gift tab + Back to Dawn prefs.

### 9. Snitching & tips

Snitch propensity = f(low loyalty, low nerve, crime memory weight, Standing toward player). Tips feed [[Security, Heat & Alerts]] → targeted shakedown of your cell (or the named target's). Guards with high Trust toward you are less likely to act on weak tips; Corrupt guards may sell you a tip-clear bribe first.

### 10. Career carry ([[Prison Career Ladder]])

| Carries globally (world save) | Resets per facility entry |
|---|---|
| Career Respect (seeds arrival Standing) | Local membership rank (re-prove yourself) |
| Gang **reputation tag** (e.g. "ex-Vipers Associate", "Traitor — Syndicate") | Per-NPC relationship records (new cast) |
| Cash, stats, recipes | Local gang standing meters |

Arrival seed (tunable, from Career Ladder): Career Respect &lt; 25 → baseline; 25–50 → +10 Standing seed with non-rivals; 50–75 → +20 and faster Associate path; 75+ → +30 but guards prioritize shaking *you* down (fame cuts both ways). Traitor tag makes the matching chapter's shot-caller start Hostile.

### 11. Social dossier (notebook)

Full visual system: [[Social Dossier — Relationships & Gangs]]. Notebook Tab → Relationships / Gangs. Lists every known NPC with band colors, Trust/Respect bars, gang badges; Gangs page shows rank, standing meters, roster fog-of-war, territory callouts, active initiation/favor.

## Systems it touches

[[Time & Schedule]] (phase-gated interactions, daily decay/stock ticks) · [[Prisoner AI & NPCs]] (identity, archetypes replace `NPCPersonalityData`) · [[Guard AI]] (guard archetypes, per-player detection modifiers, tips) · [[Security, Heat & Alerts]] (tips → targeted shakedown + heat) · [[Roll Call & Shakedown]] (targeted/skipped cells) · [[Inventory & Items]] (trade, gifts, stash holding) · [[Loot & Economy]] (wallet live: trade, bribes, favor fees, light job) · [[UI & HUD]] ([[Talk Menu & NPC Profile]] · [[Social Dossier — Relationships & Gangs]] · nameplate bands) · [[Escape Routes & Mechanics]] (intel bands, sourced tools, lookouts/distractions as escape enablers) · [[Prison Career Ladder]] (carry + arrival seed)

## Data & tuning (designer-facing, all SO/serialized)

`ArchetypeDefinition` (trait ranges, voice, stock, favored gifts, snitch base) · `GangDefinition` (territory, relations, initiation pool, store stock) · `FavorDefinition` (direction, gates, costs, timers) · `TradeStockDefinition` · `DialogueTable` (template lines with token substitution, keyed archetype voice × relationship band × memory type — no LLM) · relationship deltas & soft cap · Standing band thresholds · memory size/decay/witness radius · gossip rates · snitch propensities · bribe prices · tier thresholds · initiation cool-down.

## Planned architecture (new, `Assets/Scripts/Shared/Social/`)

`NPCIdentity` · `PersonalityTraits` · `ArchetypeDefinition` · `RelationshipStore` (sparse pair records) · `RelationshipMath` (pure, tested) · `StandingBands` · `SocialMemory` (ring buffer, pure decay logic) · `SocialEventBus` (all systems publish; memory/gossip subscribe) · `GossipSystem` · `GangDefinition`/`GangManager` · `TradingService` · `FavorService` (two-way) · `SnitchSystem` · `SocialSimulationTicker` · `SocialInteractionMenu` (Talk UI) · notebook pages for Relationships/Gangs. Deterministic from `worldSeed`.

## Milestones

1. **M1 Foundation** — v1 teardown; identity/traits/archetypes; `RelationshipStore` + math + Standing bands; seeding. *(EditMode-heavy)*
2. **M2 Talking** — Talk Menu (Profile/Talk/Gift); memory buffer; dialogue tables; nameplate band colors; Relationships notebook page (list + bars + detail fog).
3. **M3 Gangs** — gang SOs, territories + warn-offs, standing propagation, membership ladder + initiation; **Gangs notebook page**.
4. **M4 Economy** — Trade tab + stock; Gift prefs discovery; wallet wiring; bribes; light job cash source.
5. **M5 Consequences** — favors both directions; gossip; snitching + tips → targeted shakedown; dossier memory snippets unlock.
6. **M6 Guards & balance** — guard archetypes + trust modifiers; ambient ticker; simulator window rebuild; balance pass.

Each milestone is playable and individually shippable to `dev`.

## Test plan

- **EditMode (pure seams):** relationship delta pipeline + soft cap, standing formula + band thresholds, memory decay/eviction/grudges, gossip weight halving + hop cap, snitch propensity modifiers, trade price formula, gang standing propagation ×0.5/×1.0, membership gate checks, initiation/Traitor rules, tier computation, favor gate/cost validation.
- **Manual/PlayMode per milestone:** chat intel bands unlock at 25/50/75; nameplate colors match bands; territory warn-off at standing &lt; 0; initiation flow → Member; exclusive join locks rival; buy contraband → shakedown finds it → snitch tip loop; bribe skips cell; Traitor lockout; dossier filters Friends/Enemies.

## Out of scope (v3)

- Fights/combat (intimidation fail = report risk only; no melee / recruit-to-fight)
- Gang wars / scripted events between gangs
- Guard corruption investigations, guard↔guard social sim
- Romance; multiplayer social layer; LLM/generative dialogue
- Bond → unique skill RPG tree (Back to Dawn) — deferred
- More than 2 gangs (Medium Security can add a third)
- Full prison-job career sim (only one light cash job at M4)

## Locked decisions (was: open questions)

| Question | Decision |
|---|---|
| Gang names | **Vipers** & **Syndicate** working titles; flavor rename later OK |
| Intimidation fail | Report risk + respect loss — **no fight stub** |
| Snitch visibility | Gossip-first; Old-Timer Chat trust ≥ 50 can name known Snitches |
| Cash sources | Hustler sales + favor payouts + one light job at M4 |
| UI depth | Full dossier in notebook + tabbed Talk Menu |
| Join model | Exclusive Member; Traitor lockout |

---

_After implementation: update this note if the design changed, mark status Implemented, and log it in [[Prison Escape Devlog Dashboard]]._
