# Cashflow board redesign

Implemented directly in the UnityCashflow project, retaining the existing runtime scenes and financial event handlers.

- BoardTrackGenerator builds two connected, constant-width stadium tracks with 24 spaces each. Curved spaces share radial edges; straight lengths adapt to wide landscape views. Tile hit testing follows the segment surface, and player movement uses the same path.
- Screen-space UI framing replaces the former small ellipse layout. The central purple mat has a sliced rounded gold border. Fonts remain upright.
- SafeBoardLayout fits the interface inside the device safe area and guards stale simulator dimensions during resizing.
- Cash balance and the passive-income bar now update from gameplay values. HUD spacing, tile hover text, the Roll Dice control, a crisp dice icon, and the P1 marker were improved. SET opens backdrop settings; FIT restores board framing.
- Board texture imports use uncompressed textures up to 4096 pixels. This preserves source detail but does not add detail missing from the original artwork.
- Charity rolls now use two independent six-sided dice; the previous code selected uniformly from 1 to 12.

- Scene routing now re-subscribes after Unity script reloads, preventing an empty board during editor iteration.

## Review

Verified in Unity 2022.3.62f1 at 1280×720, 1920×1080, 2340×1080, and 1024×768. All four passed segment-count, shared-seam, hit-target, and HUD-bound checks. Rolling passed the token-position check. Final captures were visually inspected at 1280×720 and 2340×1080.

Open GameBoard and enter Play mode. Cashflow > Review Board runs the included editor review and writes screenshots/results to work/board-review. It changes the active play scene and Game view resolution. Checks cover 48 spaces, seams, tile hit targets, HUD bounds, and the token position after rolling.

The project remains a 2D screen-space game. The reference video's perspective 3D camera, animated 3D pawns, and physical dice are not recreated by this UI redesign. Android hardware testing is still recommended for touch sizing and device-specific safe areas.

The ZIP contains the changed Assets files; the project folder already contains these changes. Existing uncommitted CashflowGame edits were retained. A pre-edit snapshot is kept in work/CashflowGame.before.cs.txt.
