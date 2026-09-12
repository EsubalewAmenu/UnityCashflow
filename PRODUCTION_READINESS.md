# Production readiness — 10 September 2026

Update 12 September: owner-authorized live-ad version 4 and matching symbols are now built locally. See [LIVE_AD_RELEASE.md](LIVE_AD_RELEASE.md) for artifact locations and verification. The notes below describe the earlier version-3 status; publisher privacy setup is still pending.

Public release has not been submitted. The existing production draft contains version 3 with test ads. The same version is available in internal and closed Alpha testing. The owner confirmed the Unity vulnerability rejection was resolved.

## Completed

- Rebuilt version 3 with Unity 2022.3.62f3; Android API 36, ARM64, signed with the existing upload key. Prior security-task save/resume and advertising/layout regression suites passed.
- Packaged native symbols for version 3 and uploaded them in Play Console. Play confirms **This app bundle has the following file attached: native debug symbols**, including in the production draft.
- Verified each symbol ELF build ID against the corresponding library extracted from the uploaded AAB:
  - libunity: a8df61f82177e7be
  - libil2cpp: e75843102de870fb6353df11af35822d6c719e79
  - libmain: b127d7fea2226dab34fde9fe0aacd14ecd7ab220
- Symbol archive: `work/production-readiness/CashflowKingdom-v3-native-symbols.zip`. It applies only to version 3; generate matching symbols for future bundles.
- Enabled public native symbol generation in AndroidInternalBuild for future builds.
- The patched Unity editor compiled the updated script and exited successfully. The first restricted run could not reach the licensing service; the subsequent run with licensing access passed. No new phone test was performed for this setting-only change.

Java minification is disabled (`AndroidMinifyRelease: 0`), so this build has no R8/ProGuard mapping file. Do not upload a fabricated or unrelated mapping. Native IL2CPP symbols serve a different purpose. See [Google's crash symbolication documentation](https://support.google.com/googleplay/android-developer/answer/9848633?hl=en).

## Live ads and remaining work

The production Android App ID and rewarded/banner IDs are already saved (see ADMOB_SETUP.md), but `useTestAds: 1` and `productionPrivacyReviewed: 0` remain in effect. Version 3 therefore does not monetize through these live units.

The owner asked to retain https://datascienceplc.com/privacy-policy. A fresh browser inspection confirms it still describes website comments, cookies and accounts, without Cashflow Kingdom or AdMob disclosures. Add the reviewed app notice in PRIVACY_POLICY_APP_ADDENDUM.md to that page; no URL change is needed.

The European consent-message builder in AdMob again returned **Can't load page**, including after reload and retry. No consent message was published. Resolve this account/service issue, configure the applicable consent messages, review final Play Data safety declarations, then test UMP acceptance/refusal and privacy changes on a registered test device before switching to live ads.

After those checks, create a new signed non-development bundle with a new version code, live mode and matching symbols, validate it on the internal track, replace version 3 in the production draft and submit for review. Do not simply promote the current test-ad bundle expecting ad revenue.

The current website was inspected through its public page. No website content or account-wide AdMob policies were changed. No new live-ad bundle has been built or published in this task.
