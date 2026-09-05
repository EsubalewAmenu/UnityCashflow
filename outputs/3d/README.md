# Cashflow 3D update

The Unity project is updated in place. Open Assets/Scenes/GameBoard.unity and enter Play mode.

## Presentation
- A perspective 3D board, raised base, physical card stacks, metallic player pawn, rounded physical dice, and a small city setting.
- Existing connected track geometry is printed in world space, with scalable illustrations and numbered spaces.
- OVERVIEW fits the board; FOLLOW tracks the player; ROTATE and ZOOM adjust the camera. Drag on the board to orbit, use the mouse wheel or pinch to zoom.
- A live overhead minimap restores the overview when tapped.
- The camera shows the dice while rolling and follows the pawn during movement.
- Safe-area HUD, financial statement, event dialogs, scenery toggle, dice sound toggle, and reduced-motion setting.

## Gameplay
Dice results come from the uppermost physical faces. One die is used normally, two after charity. Movement animates one space at a time before resolving the existing financial event. Dialogs block further rolls. The existing professions, deals, tile events and financial calculations remain in the controller.

## Code
BoardWorld handles the camera, physical board, decks, pawn, sound and minimap. BoardDie handles rounded dice geometry, physics and face values. BoardTrackGenerator supplies connected spaces and movement coordinates. BoardTileIcon supplies vector illustrations. CashflowGame retains rules and HUD. The former screen-only board framing, flat center decks and sprite dice presentation were removed.

## Verification
Cashflow > Review Board runs the editor checks and writes captures/results to work/3d-review. The final results are included alongside this document. Testing covers four landscape sizes, space count, shared seams, world-space tile hit targets, HUD bounds, physical results and pawn landing, the live minimap, and modal roll blocking.

This is an original procedural 3D presentation based on the supplied video. Physical-device performance and touch behavior have not been tested on Android hardware.

The ZIP contains the complete Assets, Packages and ProjectSettings folders plus this README; generated Library and temporary files are excluded. The original project already contains the changes; no archive import is required on this computer.

## Environment update
The board now has a track-shaped physical base and narrow gold rim, removing the old filled rectangular corners. Slate replaces the green surround. The flat green ground and sphere trees have been replaced by a batched city plaza with stone paving, sidewalks, crosswalks, lane markings, pastel storefronts, striped awnings, windows, roof details, cars, lamps and benches. BoardEnvironment builds the scenery; BoardPlateGraphic supplies the curved perimeter. The four-resolution and one-/two-dice regression checks passed after these changes.

## Mobile controls and framing
Removed the right-side zoom/rotate controls and the minimap camera/render texture. Settings now lives in the bottom bar; its Camera View section switches between Overview and Follow. The scene renders across the full display width while the HUD retains safe-area padding. Perspective framing fits the curved board perimeter dynamically for different aspect ratios. Editor review includes 2532x1170 (iPhone 12) and 2340x1080 (Pixel 5), plus 1280x720, 1920x1080 and 1024x768. These are editor resolution checks, not physical-device tests.

## Deal cards
Landing on Deal now offers separate Small Deal and Big Deal decks using the existing investment amounts. Cards show down payment, income, available cash, and affordability; Buy updates balances exactly once and Pass closes the modal correctly. A shared cream-and-gold presentation improves event cards too. Deals flip from an opportunity card back with gold sparkles; purchases show a brief celebration. Reduced Motion uses a short fade. Deal-flow tests passed alongside the five-resolution board regression.
