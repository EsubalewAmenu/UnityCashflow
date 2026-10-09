# Multiplayer milestone 1 — local prototype

Open the profession screen and choose **LOCAL 2 PLAYERS**, then **NEW MATCH**. Pass the device after resolving the active player's card and selecting **END TURN**. The header identifies the active player. The other player's position is shown by a sphere; the moving pawn represents the active player. Random professions and dreams are assigned at creation.

Implemented: isolated per-player finances, investments, debts, positions and decisions; two-player turn rotation; shared deal-deck progression at handoff; local match save/resume with checksum and backup; eliminated-player skipping; no full-screen turn ads or rewarded charity ads in this mode. Existing solo saves remain in their original file. Local matches use `cashflow-local-match-v1.json` in Unity's persistent data folder. The board menu saves and returns to the opening screen.

This is a transitional adapter over the existing financial controller, not a completed online engine. `ApplyPlayerState` now centralizes state restoration for both modes. Payday arithmetic and bankruptcy rules are extracted into pure `CashflowRules`. Other rules still use the controller and require further extraction before server hosting. `LocalMatch` holds independent deep-copied player snapshots and transfers shared deck state at turn boundaries.

Scope: two players on one device. No online connection, room service, public lobby, credentials, matchmaking, or backend charges have been introduced. Names/colors, explicit setup choices, online command IDs, server-generated randomness and server-authoritative validation remain future milestones. Tokens and lobby presentation are prototype quality. The existing version-6 release bundle is unchanged; this prototype is not included in it.

Validation: `LocalMatchReview.Run` checks cash/investment/debt isolation, state handoff, independent save/resume, premature End Turn rejection, shared finance rules and local Payday without a fullscreen ad. `UnitySecurityReview.Run` exercises the existing solo save and fake-ad regressions. No physical-device or visual layout approval is claimed for the new multiplayer screen.

Next milestone: extract command-based match rules and stable player/card IDs, then add a tested server adapter and private room creation/joining. Keep single-player and local play operational through that migration.

## Command rules foundation

`CashflowCommands.cs` adds a Unity-independent domain with match/player IDs, revisions, typed requests, decision IDs, receipts, and roll/movement/decision/complete phases. It validates authenticated identity separately from the request, turn ownership, revision and the offered decision. Exact retries return the original receipt; reusing an action ID with different data is rejected. The trusted board adapter supplies dice, board rules, Payday amounts and landing decisions; clients cannot submit a dice result or price. State mutation happens only after successful calculation. A future service must serialize requests per match and persist state plus receipts atomically.

The reducer covers roll, purchase with optional shortfall borrowing, standalone borrowing, required payment, passing, movement/Payday settlement and turn completion. Match serialization preserves action history. The local mode now stores stable generated player and match IDs and routes End Turn through this reducer. Existing local saves without command metadata initialize it when loaded. Single-player and local purchase, required-payment and bank-borrowing paths use the same extracted checked arithmetic.

This does not yet move all UI decisions into the reducer: local dice physics, card drawing, charity, market sales, dreams and some controller event handling remain in the legacy adapter. Financial action retries are tested in the new reducer, but the live purchase UI still uses its existing per-dialog resolution guard. Do not expose the legacy adapter or editable local snapshots as an online API. Complete that adapter migration before connecting a backend.

Validation: `CommandRulesReview.Run` covers 25 assertions including identity spoofing, wrong turns, stale requests, duplicate rolls/purchases/bills/end-turn, retry after serialization, insufficient funds, overflow with no partial mutation, and skipped/eliminated players. The local and single-player suites are rerun for the integrated changes. No online service or new release bundle is created in this milestone.

## Card/event rules extraction — 9 October 2026

`CashflowEventRules` now supplies the live controller's donation cost/payment, market-sale balances and linked-loan settlement, dream purchase eligibility, child count cap, Fast Track loss amounts, and shuffle/draw behavior. Deck draws still use the local random source through an injected function; the financial/event calculations have no Unity dependency.

The command reducer adds typed charity, market, dream, baby, loss and deck-choice decisions, with Donate/Sell/BuyDream/Acknowledge/DrawSmall/DrawBig actions. Purchases now retain owned investment records, allowing a market sale to verify ownership and remove only that asset and its debt. Charity's bonus is consumed only on the next actual roll; dream completion ends the match. Deals are drawn from trusted persisted decks and receive a unique encounter ID. Empty decks require the trusted adapter to refill them; no client supplies card prices or effects.

The expanded rule suite has 41 assertions including repeat donation, repeat sale, invalid dream purchase, victory retries, child-event retries, saved deck draws, and shuffle-cycle behavior. Live UI callbacks use the shared effect functions but are not all dispatched through the command reducer yet. The complete controller-to-command adapter, content catalog/board resolver, server persistence and networking remain future work. Existing local matches and solo saves continue to use their current formats. No AAB or online deployment is made here.

Resume End Turn fix: normalize Unity's empty deserialized command-decision placeholder outside Decision phase. Confirmed the regression fails before the fix and passes afterward, including an isolated copy of the reported saved match and duplicate button clicks. The original user save was not modified by the test.
