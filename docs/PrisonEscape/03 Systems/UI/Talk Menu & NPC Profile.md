# Talk Menu & NPC Profile

Real-time interaction overlay for inmates and guards. Design of record for the Talk surface of [[Social Ecosystem & Gangs]] (v3). Theme rules: [[UI Theme & Style Guide]].

**Status:** Implemented in review (PRs #46–#47).
**Opens from:** raycast interact on an NPC (`PrisonerSocialPresenter` rework → Talk entry point).
**Pause:** **none** — world keeps running (Escapists-style). Player movement locked while menu is open; look still works enough to cancel with Escape / interact-again.

## Why it exists

v1 greets were a single prompt line. Players need a readable **profile** (who is this?), **actions** (chat / gift / trade / favors / threat), and immediate feedback on Trust/Respect — without opening the notebook mid-conversation.

## Layout

Centered bottom-third panel (does not cover vitals or routine strip). Paper/ink styling per theme.

```
┌─ Eddie "Wires" Malone ──────────── Vipers · Soldier ─┐
│ [Profile] [Talk] [Gift] [Trade] [Favors] [Threat]     │
│                                                        │
│  Trust ████████░░  +42      Respect ██████░░░░  +28   │
│  Band: Friendly             Gang: Vipers (Member?)     │
│                                                        │
│  (tab body — see below)                                │
└────────────────────────────────────────────────────────┘
```

Header always shows: display name, gang badge (or Independent), archetype short label, Standing **band** name + color chip.

### Tabs

| Tab | Visible when | Body |
|---|---|---|
| **Profile** | Always | Trait blurb (1 sentence from archetype), cell/shift, known gift prefs (fog until discovered), last memory line *they* might reference about you (if any Direct/Witnessed weight ≥ 4) |
| **Talk** | Always | **Chat** button (disabled if already used this phase); last dialogue line; intel flavor text when trust bands unlock |
| **Gift** | Always (inmates + guards who accept gifts) | One item slot (from inventory / held) + optional cash stepper; **Give**; shows predicted Trust delta if prefs known |
| **Trade** | Archetype has stock **and** gang standing &gt; −25 (rivals refuse below) | Seller stock list + your offer (cash or barter); prices live from [[Social Ecosystem & Gangs]] §8 |
| **Favors** | They have an open offer **or** you can Ask | List: Do favor (accept/decline/maybe) · Ask favor (Lookout / Distraction / … gated) |
| **Threat** | Inmates only | **Intimidate** button + success chance hint (respect + Strength vs their nerve). Fail = respect loss + report risk |
| **Bribe** | Guards only, archetype **Corrupt**, and you have discovered them | $25 / $40 / $60 options ([[Social Ecosystem & Gangs]] §8) |
| **Snitch** | Guards only | Pick a named inmate you know → tip their cell. Confirms with warning about social fallout |

Unavailable tabs are **hidden**, not greyed (keeps the panel clean).

## World markers (Escapists readability)

Overhead, above nameplate, when relevant:

| Marker | Meaning |
|---|---|
| Green `!` | Open favor they want you to do |
| Coin | Has trade stock today |
| Small gift icon | (Optional) they favor an item category you currently hold — low priority |

## Nameplates

`CharacterNameLabel` tints by Standing **band** toward the player:

| Band | Tint |
|---|---|
| Enemy | Strong red |
| Hostile | Orange |
| Neutral | Theme default (near-white / ink) |
| Friendly | Soft green |
| Ally | Clear green |
| Confidant | Bright green + subtle underline |

Guards use the same band colors (Trust-heavy Standing). See [[Status & World UI]].

## Guards vs inmates

| | Inmate | Guard |
|---|---|---|
| Header | Gang · archetype | Role · shift |
| Trade | Hustler / gang store | Rare (only if design adds commissary later — out of scope v3) |
| Threat | Intimidate | Hidden |
| Bribe / Snitch | Hidden | Bribe if Corrupt+known; Snitch always available once you know ≥1 inmate name |

## Feedback

- Trust/Respect deltas float via retitled `AffinityFloatPopup` (e.g. `Trust +2`, `Respect −10`).
- Closing the menu does not consume the Chat charge if Chat was not pressed.
- Busy NPCs (mid-escort, locked in count formation) show a one-liner and no tabs.

## Widgets / files (planned)

| Piece | Role |
|---|---|
| `SocialInteractionMenu` | Root overlay, tab host |
| `NpcProfileTab` / `NpcTalkTab` / … | Tab bodies |
| `SocialOverheadMarker` | `!` / coin |
| `CharacterNameLabel` | Band tint (extend existing) |
| `PrisonerSocialPresenter` | Rework: opens menu instead of greet chain |

## Related

[[Social Ecosystem & Gangs]] · [[Social Dossier — Relationships & Gangs]] · [[Status & World UI]] · [[Player & Interaction]] · [[UI Theme & Style Guide]] · [[Loot & Economy]]
