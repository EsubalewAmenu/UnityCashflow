# Cashflow Kingdom AdMob setup

Update 24 September 2026: gameplay top banner and shared Payday/Downsized interstitial included in the signed version-5 AAB using the new owner-provided production IDs. See [GAMEPLAY_ADS.md](GAMEPLAY_ADS.md) and [RELEASE_V5.md](RELEASE_V5.md). The old version-4 AAB does not contain these changes.

Update 12 September 2026: version 4 now uses the production IDs below with test mode disabled, as explicitly requested by the owner. See [LIVE_AD_RELEASE.md](LIVE_AD_RELEASE.md). Runtime UMP gating remains enabled; the earlier publisher privacy tasks below remain pending.

Updated 9 September 2026. Android app and two ad units created and verified in AdMob. Test ads remain enabled; live monetization is not enabled.

| Setting | Value |
| --- | --- |
| Android AdMob App ID | `ca-app-pub-8011674951494696~4721468852` |
| Charity - Two Dice Bonus (rewarded) | `ca-app-pub-8011674951494696/7902165966` |
| Menu and Results - Adaptive Banner | `ca-app-pub-8011674951494696/7156060505` |
| Intended audience, confirmed by owner | Adults only, 18+ |
| Support email | datascienceplc@gmail.com |
| Supplied privacy URL, needs app disclosures | https://datascienceplc.com/privacy-policy |

The native Android App ID is saved in `Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset`. Production unit IDs are saved in `Assets/Resources/CashflowAdSettings.asset`, with `useTestAds: 1` and `productionPrivacyReviewed: 0`. Runtime continues selecting Google's sample rewarded unit `/5224354917` and banner `/6300978111`, both under publisher `ca-app-pub-3940256099942544`. iOS production setup is pending.

The reward is **1 Two-dice roll**, matching the existing next-roll bonus, with no cash deduction. Ad pods are disabled. Both units use Google's optimized floor with All prices; banner refresh is Google optimized. Banners appear only on menu/results, with no interstitial or app-open units created.

The current audience flags remain `childDirected: 0` and `underAgeOfConsent: 0`; the existing SDK integration maps these to unspecified request tags and sends false to UMP's under-age parameter. These flags are not an age check or a Play Console audience declaration. The G-rated ad-content filter remains in place; an adult audience does not require mature ads.

## Remaining before live ads

1. Update the public privacy policy to cover Cashflow Kingdom and its advertising SDK. The supplied page currently describes website comments, cookies and accounts, without app/AdMob disclosures. Include the app operator/contact, local game saves, advertising data and purposes, consent/privacy controls, and applicable retention/deletion practices. Google's SDK disclosure describes IP address, interactions, diagnostics and device/account identifiers used for ads, analytics and fraud prevention: https://developers.google.com/admob/unity/privacy/play-data-disclosure . Verify any additional services used by the final release.
2. Create and publish the appropriate AdMob consent messages, then test production UMP using test devices. The European regulations editor returned **Can't load page** twice, including after reload; no message was saved or published. US state privacy setup also remains pending. Keep test mode on while this is unresolved.
3. Complete Play's ads, Data safety and 18+ target-audience declarations for the final app, then link the published store app in AdMob. AdMob currently shows no store link and Requires review.
4. Validate consent acceptance/refusal, privacy changes, rewarded completion/early close and banner layout on a device before enabling production IDs. Only then mark the privacy review complete and disable test mode for a release build.

The account's Policy center lists five other apps with issues; Cashflow Kingdom was not listed. No changes were made to those other apps.

## Verification and build scope

Unity imported and compiled the updated build validator. It checks IDs for the selected platform, so pending iOS sample settings no longer reject an otherwise complete Android configuration. Sample/malformed IDs and incomplete privacy review still block production builds.

Configuration checks confirmed the saved Android IDs, test-mode switch and privacy gate. No new APK was built or installed during this account-configuration task. The previous installed development APK still has the sample App ID; build again to include the newly saved native App ID. Earlier gameplay/ad lifecycle test results are under `work/admob-review` and are not new device tests of these account settings.
