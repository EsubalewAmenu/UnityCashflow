# Live-ad release — 12 September 2026

Version **1.0 (4)**, package `com.herma.Cashflowgame`, built with patched Unity **2022.3.62f3**. Signed non-development AAB, ARM64, minimum API 24, target API 36.

## Deployment files

- AAB: `work/production-v4/CashflowKingdom.aab`
- Matching native symbols: `work/production-v4/CashflowKingdom-1.0-v4-IL2CPP.symbols.zip`

Upload the AAB to the desired Play release and attach this ZIP as its native debug symbols. Do not use the version-3 symbols with version 4. Java minification remains disabled, so no R8/ProGuard mapping file is generated. The separate mapping warning is optional for this configuration.

## Production ads

- App ID: `ca-app-pub-8011674951494696~4721468852`
- Rewarded: `ca-app-pub-8011674951494696/7902165966`
- Banner: `ca-app-pub-8011674951494696/7156060505`
- `useTestAds: 0`; no registered test-device overrides.

The owner explicitly requested the production-ID artifact after being informed that publisher privacy setup is pending. `productionBuildAuthorized: 1` records that choice separately; `productionPrivacyReviewed: 0` is retained rather than falsely certifying review. Runtime UMP consent gating is unchanged: the SDK initializes and requests ads only when `ConsentInformation.CanRequestAds()` permits it. Production IDs do not guarantee ad availability or account approval.

The previously identified privacy-page update and AdMob consent-message setup remain pending. No publication or new on-device live-ad test was performed in this task.

## Verification

- Unity build succeeded with zero errors.
- JAR signature verified using the existing upload key.
- All three native symbol build IDs match their libraries extracted from the AAB:
  - libmain: `b127d7fea2226dab34fde9fe0aacd14ecd7ab220`
  - libunity: `a8df61f82177e7be`
  - libil2cpp: `b3a5f5e3bc12d35e144fbeb0a0f464bb7a34a63b`
- AAB SHA-256: `3E49B876C31C86DDBC228BCE1DAF174662CD7D6DC1E6A162364D574CB5CC5F97`
- Symbols SHA-256: `A23DE63A37522017C34782916DFCCEE6BE92095EFFC235F966D4F7B8C7B41083`

Build command: **Cashflow > Build Production Bundle (Live Ads)**. Increase the Android version code for subsequent uploads and archive each bundle with its own symbols. Existing internal version 3 remains a test-ad artifact.
