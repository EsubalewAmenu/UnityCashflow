# Gameplay advertising — 24 September 2026

Included in the signed version-5 AAB in `work/production-v5/`, together with the new finance logo. See `RELEASE_V5.md`. The existing version-4 AAB predates these placements. No upload or publication was performed.

## Placements

- Gameplay adaptive banner: `ca-app-pub-8011674951494696/6021118406`, anchored at the top, above the header. Safe-area layout reserves its reported height plus separation from controls. The board camera adjusts to the remaining viewport. Gameplay popups retain the banner. Native full-screen ads and consent forms temporarily hide it; it returns afterward. No-fill leaves the space available to gameplay.
- Shared Payday/Downsized interstitial: `ca-app-pub-8011674951494696/5841069288`. Preloaded after consent, expires after 55 minutes, retries with bounded backoff. No blocking wait for a load and no delayed ad after the player has continued.
- Existing menu/results banner and optional rewarded charity placements retain their IDs.
- A 90-second cooldown after any closed full-screen ad prevents consecutive interstitials. Configurable in `CashflowAdSettings`; it does not restrict voluntarily requested rewarded ads.

## Turn flow

Payday settles its financial effects once when movement finishes. The salary/cashflow-day popup and optional interstitial appear only when the player lands on that space. Passing it settles the payment and immediately resolves the destination without a break. Older saves paused at a passing-Payday break also continue directly without another payment. OK on a landing popup opens a ready interstitial if eligible, otherwise continues play. Negative net cashflow is shown as a deduction; game-over handling takes priority. This landing-only refinement is included in the version-6 AAB; see RELEASE_V6.md.

Downsized first shows the existing expense/borrowing decision. After payment, the short-break popup offers the same OK flow. Skipped turns do not show more ads. No ad changes salary, expenses, borrowing or skipped-turn counts.

The checkpoint preserves whether a salary break is awaiting acknowledgment or its destination is awaiting resolution. Resuming cannot collect salary again, and resuming after acknowledgment resolves the tile without replaying the ad. Audio and time scale are restored after close or show failure. Duplicate confirmation/callbacks are ignored.

## Validation

Unity 2022.3.62f3 compiled and ran the isolated save/resume and expanded ad suites successfully. Tests use fake ads, never click or request production ads for verification. Covered reward lifecycle, missing interstitial, show exception, duplicate close/confirm, cooldown, finance invariants, salary resume and layout snapshots at 1280x720, 2532x1170 and 2340x1080. The Pixel 5 popup screenshot was visually inspected. Evidence is in `work/gameplay-ads-review/`.

Real SDK delivery, consent and native banner placement still require a device test. Production IDs are configured and test mode remains disabled as requested; existing publisher privacy status is unchanged. Use sample ads or registered test devices for further device validation.
