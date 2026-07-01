# Prison Social & Reputation System (v1.2)

This document is the project reference for **NPC affinity**, **prison-wide reputation**, **anti-spam (phase cooldown)**, **gift variety**, **+50 personality perks (hooks)**, and the **Favor (mini-quest) system** built on the schedule. Code paths live under `Assets/Scripts/Shared/Prison/`.

---

## 1. Core loop

- Each NPC has an **affinity** in **[-100, +100]**.
- **Global reputation** is the **average** affinity of registered prisoners (optionally normalized to an expected count of 8).
- **Greeting** uses a **per-NPC, per–schedule phase cooldown** (no more affinity from a second press in the *same* phase; UI shows **“Busy…”** and **F** is disabled for that case).
- **Gifts** use **soft cap** for positive gains (unchanged) plus a **category repeat** rule (see below).
- **Favor** is the **highest gain** when the player delivers a **specific item** for that NPC’s current request; requests refresh when the **schedule phase** changes.
- **Personality perks** (design) unlock when **one NPC’s** affinity first reaches **+50**; the game code fires a **C# event** you can wire to (Broker secret stock, Narc shakedown warning, Bully guard bump, etc.).

---

## 2. Schedule phase lock (Greeting)

**Intent:** Stops F-spam in one sitting; resets automatically when the prison moves to the next activity.

- **Data:** `SocialManager` stores `lastGreetedPhase[cellIndex] = PrisonEventType` *after* a successful Greeting that actually applied affinity.
- **Check:** `IsGreetingBlockedByPhaseCooldown(cellIndex)` is true if `lastGreetedPhase[cell] == PrisonTimeManager.Instance.CurrentEvent`.
- **If blocked:** `ChangeAffinity(Greeting)` returns current affinity (no change, no new events to tier/perk for that call).
- **If no `PrisonTimeManager`:** the cooldown is **off** (useful in empty test scenes).
- **UI:** `PrisonerSocialPresenter` shows **`[F] Busy…`** and `CanInteract` is **false** so `PlayerInteractor` will not run `Interact` (prompt still shows).

**Hook:** `PrisonTimeManager.OnEventChanged` (existing); no extra per-minute timer.

---

## 3. Gift: variety and favored items

**Base gift amount** is still the configurable default in `SocialManager` (`defaultGiftAmount`).

- **Favored list:** If the gifted `ItemData` is in `NPCPersonalityData.favoredItems`, the *base* gift delta is doubled (`SocialMath.FavoredGiftMultiplier = 2`).
- **Category repeat (anti trash-spam):** `SocialManager` stores `lastGiftCategory[cellIndex]`. If the *new* gift’s `ItemData.category` equals the **last** category given to *that* NPC, the *post-favored* gift delta is multiplied by `SocialMath.GiftSameCategoryRepeatMultiplier` (**0.5**), **unless** the item is a **favored** gift (favored bypasses the variety penalty, per v1.2 spec).

**Order of operations in code (Gift):** base gift → ×2 if favored → ×0.5 if same non-favored category as last time → then soft-cap / multiplier / clamp via `ApplyAffinityChange`.

Logically that matches: “double for favored, then half for repeat category (when not the favored exception).”

---

## 4. Favor system (objectives, not random guessing)

### 4.1 Data: `FavorOfferDefinition` (ScriptableObject)

- **Create:** *Assets → Create → Prison → Social → Favor Offer*
- **Fields (main):** `requestLabel`, `requiredItem` (`ItemData`), `affinityReward` (default **+15**), `activeDuringPhases` (empty = any phase), `onlyForPersonalities` (empty = any NPC; otherwise only those `NPCPersonalityData`).

### 4.2 Runtime: `SocialManager`

- **Inspector:** *Favor system → Favor Offer Table* — list of `FavorOfferDefinition` assets. If **empty**, favors are **disabled** (dict cleared on phase).
- On **Start** and on **`PrisonTimeManager.OnEventChanged`**, the manager runs **`RebuildFavorsForPhase(PrisonEventType phase)`**:
  - For **each** registered `cellIndex`, filter the table: phase must match (or no phase filter) and **personality** filter must pass.
  - From matching entries, **one** is chosen at **random** and stored in `_activeFavorByCell[cell]`.
- **Query:** `GetActiveFavorInfo(cellIndex)` → `ActiveFavorInfo { HasFavor, Definition }`.
- **Complete:** `TryCompleteFavor(cellIndex, playerInventory, out newAffinity)`:
  - Requires `PlayerInventory.HasItem` / `RemoveItem` for the **required** item.
  - Applies affinity via `ChangeAffinity(..., SocialActionType.Favor, customBaseAmount: definition.affinityReward)`.
  - Clears the active favor for that cell.

**Note:** When the **phase** changes, favors are **re-rolled**; an unfinished request from a previous phase is **replaced** (good for “breakfast only” type offers by constraining `activeDuringPhases`).

### 4.3 UI / interaction: `PrisonerSocialPresenter`

- If a favor is active and the player has the item: **Deliver** line and **F** runs `TryCompleteFavor`.
- If a favor is active and the item is missing: prompt explains need; `CanInteract` is **false** (no F spam).
- If no favor, falls back to **Greet** or **Busy** for phase cooldown (see order in code).

**Priority when aiming at the NPC:** Favor (with item) > Favor (missing item) > Greet busy > Greet.

---

## 5. Personality perks at +50 (hooks)

**Data:** `SocialManager.PersonalityPerkAffinityThreshold` = **50** (public const).

- When affinity crosses from **&lt; 50** to **≥ 50** in one `RunAffinityApply` (any action: greet, favor, gift, etc.), and `RegisterPrisoner` did not already mark the cell as “already +50+”, the event fires:
  - **`OnPersonalityPerkUnlocked(int cellIndex, NPCPersonalityData personality)`**
- `RegisterPrisoner` with `initialAffinity >= 50` pre-adds the cell to an internal set so the event does **not** spuriously fire for save-loaded games unless you change that policy.

**Design examples (not fully implemented; subscribe to the event):**

| At +50 | Suggested hook |
|--------|----------------|
| **Broker** | Open “legendary” row in your trade / black-market UI. |
| **Narc** | If next schedule includes shakedown, show a **30s** warning (listen to time manager + this perk). |
| **Bully** | On escort/aggro, if a guard is within 5m of the Bully, **bump** stun. |

Create a small `PersonalityPerkHandler` (you add) and subscribe in `OnEnable` / unsubscribe in `OnDisable`.

---

## 6. Math: `SocialMath` (v1.2 defaults)

- **Greeting base:** 2 (can be overridden on personality).
- **Favor base constant:** `FavorBase` = **15** (Favor from table uses `FavorOfferDefinition.affinityReward` in practice).
- **Betrayal / theft / snitch** base: -50 (personality can override `betrayalPenalty`).
- **Favored gift:** ×2 to base before repeat-category rule.
- **Same category again:** ×0.5 to that gift’s delta, unless favored.
- **Positive soft cap** toward +100: unchanged (`1 - currentAffinity/100` for positive only).

`NPCPersonalityData.favorGain` default is **15** to match.

Editor tool: **Tools → Prison → Social Balance Simulator** (uses the same `SocialMath` for preview; it does not simulate phase clock or favor table by default).

---

## 7. Key types and files

| Piece | File |
|-------|------|
| Phase cooldown, gifts, perks, favors | `SocialManager.cs` |
| Math | `SocialMath.cs` |
| Favor SO | `Assets/Scripts/Shared/Prison/FavorOfferDefinition.cs` (method: `IsValidFor`) |
| Personality SO | `NPCPersonalityData.cs` |
| World label + F | `PrisonerSocialPresenter.cs` |
| Schedule | `PrisonTimeManager.cs`, `PrisonEventType` |
| Spawn + registry | `GameManager.cs` (personalities, optional label names) |
| Interact / F | `PlayerInteractor.cs` (unchanged contract) |

---

## 8. Unity setup (quick)

1. **Scene:** `SocialManager` (assign **Favor Offer Table** if you want dynamic favors).  
2. **GameManager:** `availablePersonalities` filled.  
3. **Favor content:** create *Favor Offer* assets, reference **ItemData** and e.g. **Breakfast** in `activeDuringPhases` for “breakfast” requests.  
4. **Prisoner prefab:** `PrisonerAI` + `PrisonerSocialPresenter`.  
5. **Time:** `PrisonTimeManager` with a `PrisonSchedule` for phases to change.

---

## 9. Changelog summary (this implementation pass)

- Phase-based **Greet** cooldown (per NPC, per `PrisonEventType`).
- **Gift** same-**category** repeat penalty, **favored** bypass.
- **Favor** table + re-roll on phase, inventory delivery, **+15** style rewards via `affinityReward` on the asset.
- **+50** **OnPersonalityPerkUnlocked** event (hook traits per personality in separate scripts).
- **Presenter** priority: Favor / Busy / Greet; **F** only when `CanInteract` (deliver, or greet, not on Busy).

*Last updated: v1.2*
