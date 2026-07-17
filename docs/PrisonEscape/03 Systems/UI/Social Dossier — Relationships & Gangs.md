# Social Dossier — Relationships & Gangs

Diegetic notebook pages that show the **entire social system** at a glance: who likes you, who hates you, which gangs run the yard, and where you stand. Design of record for [[Social Ecosystem & Gangs]] (v3). Hosted by `StolenNotebookUI` (Tab). Theme: [[UI Theme & Style Guide]].

**Status:** Implemented (merged to `dev`).
**Access:** Notebook (Tab) → sub-tabs **Relationships** | **Gangs** (replaces the empty "social" placeholder). No separate `N`/`Y` keys (Back to Dawn had those; we stay inside the stolen notebook). Optional hotkey jump later.

## Why it exists

The Talk Menu is for **one person in front of you**. The dossier is for **strategy**: plan gifts, avoid enemies before yard time, track initiation, and see territory before you walk into a warn-off. Inspired by Escapists profiles + Back to Dawn relationship/faction screens, adapted to paper-notebook UX.

## Page A — Relationships

Two-column spread (list left, detail right) on the notebook paper.

### List (left)

**Filter chips** (multi-select off — one active):

| Chip | Shows |
|---|---|
| All | Every NPC you have met **or** heard of by name |
| Inmates | Prisoners only |
| Guards | Guards only |
| Friends | Standing band Friendly / Ally / Confidant |
| Enemies | Hostile / Enemy |
| Gang | Members of your current gang (or empty if Outsider) |

**Sort:** Name · Standing (desc) · Trust · Respect · Recently interacted.

**Each row:**

- Band color bar (left edge)
- Display name
- Gang badge (Vipers / Syndicate / —)
- Mini Trust + Respect bars
- Icons: open favor `!` · trade coin · Traitor mark if applicable

Unknown NPCs (gossip-only name, never met) appear greyed with "Heard of" — detail pane limited until first Talk.

### Detail (right)

On row select:

| Block | Content |
|---|---|
| Header | Name, archetype, gang, cell/shift, Standing band label |
| Bars | Full Trust / Respect / derived Standing |
| About | One-line archetype blurb |
| Gifts | Known favored categories; unknown slots show `???` until gifted or gossip-revealed |
| Memory | Up to **3** events *you* know about involving them (from your own notes — not their full buffer): e.g. "Did a lookout for you · Day 3", "Caught you with contraband · Day 2" |
| Favors | Open Do-favor + Ask-favor availability summary |
| Last seen | Location label if known this phase (optional; fog if not) |
| Actions | **Find on map** (if map page exists) · hint text "Talk to them in person to Chat / Gift / Trade" |

No Chat/Gift/Trade from the dossier — those stay in-world via [[Talk Menu & NPC Profile]] (keeps social acts diegetic and interruptible by guards).

## Page B — Gangs

### Top: your status

- Current rank with your gang (or **Unaffiliated**)
- Warning if Traitor-locked
- Career reputation tag if any (from [[Prison Career Ladder]] carry)

### Gang cards (Vipers | Syndicate)

Each card:

| Element | Content |
|---|---|
| Name + flavor line | Muscle / smugglers |
| Your standing meter | Average Standing with members |
| Your rank vs them | Outsider / Associate / Member / Trusted / Locked |
| Territory | "Yard — weight pit" / "Cafeteria — back tables" + link note to [[Locations, Zones & Cells]] |
| Roster | Known members (fog: unknown faces until met/gossiped); Shot-Caller starred |
| Store | Syndicate: "Member shop — delivers under bed after count" when unlocked |
| Active quest | Initiation favor or gang favor tracker (objective one-liner) |

**Independents** strip: count of known unaffiliated inmates (Old-Timers, Loners, etc.).

### Rival / warn-off callouts

If Vipers standing &lt; 0 and you are not a member → ink stamp: "Vipers don't want you in their corner." Same for Syndicate tables. Matches territory rules in [[Social Ecosystem & Gangs]] §5.

## Fog of war rules

| Info | Revealed when |
|---|---|
| Name + band | First Chat, or gossip names them |
| Gift prefs | Successful gift of that category, or Chat/gossip hint |
| Full roster face | Met them, or Shot-Caller/Associate gossip |
| Corrupt guard flag | Inmate gossip or successful bribe discovery path |
| Snitch label | Old-Timer Chat trust ≥ 50 (known snitches), or you catch them tipping |

## Header strip (both pages)

Prison-wide reputation tier (Outsider / Associate / Respected / Kingpin) from average Standing + gang rank bonus — same names as legacy v1 tiers, new math.

## Widgets / files (planned)

| Piece | Role |
|---|---|
| `NotebookRelationshipsPage` | List + detail |
| `NotebookGangsPage` | Cards + meters |
| `SocialDossierEntry` | Row view-model |
| `StolenNotebookUI` | Hosts sub-tabs (extend existing map/social/workbench/schedule) |

## Milestone mapping

- **M2** — Relationships list + bars + basic detail; nameplate sync.
- **M3** — Gangs page + territory + rank.
- **M5** — Memory snippets + gift fog unlocks fully wired.

## Related

[[Social Ecosystem & Gangs]] · [[Talk Menu & NPC Profile]] · [[Notebook & Crafting UI]] · [[UI & HUD]] · [[Status & World UI]] · [[Prison Career Ladder]] · [[Locations, Zones & Cells]]
