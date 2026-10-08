# Multiplayer milestone 1 — local prototype

Open the profession screen and choose **LOCAL 2 PLAYERS**, then **NEW MATCH**. Pass the device after resolving the active player's card and selecting **END TURN**. The header identifies the active player. The other player's position is shown by a sphere; the moving pawn represents the active player. Random professions and dreams are assigned at creation.

Implemented: isolated per-player finances, investments, debts, positions and decisions; two-player turn rotation; shared deal-deck progression at handoff; local match save/resume with checksum and backup; eliminated-player skipping; no full-screen turn ads or rewarded charity ads in this mode. Existing solo saves remain in their original file. Local matches use `cashflow-local-match-v1.json` in Unity's persistent data folder. The board menu saves and returns to the opening screen.

This is a transitional adapter over the existing financial controller, not a completed online engine. `ApplyPlayerState` now centralizes state restoration for both modes. Payday arithmetic and bankruptcy rules are extracted into pure `CashflowRules`. Other rules still use the controller and require further extraction before server hosting. `LocalMatch` holds independent deep-copied player snapshots and transfers shared deck state at turn boundaries.

Scope: two players on one device. No online connection, room service, public lobby, credentials, matchmaking, or backend charges have been introduced. Names/colors, explicit setup choices, online command IDs, server-generated randomness and server-authoritative validation remain future milestones. Tokens and lobby presentation are prototype quality. The existing version-6 release bundle is unchanged; this prototype is not included in it.

Validation: `LocalMatchReview.Run` checks cash/investment/debt isolation, state handoff, independent save/resume, premature End Turn rejection, shared finance rules and local Payday without a fullscreen ad. `UnitySecurityReview.Run` exercises the existing solo save and fake-ad regressions. No physical-device or visual layout approval is claimed for the new multiplayer screen.

Next milestone: extract command-based match rules and stable player/card IDs, then add a tested server adapter and private room creation/joining. Keep single-player and local play operational through that migration.
