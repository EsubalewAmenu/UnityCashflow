# Internal testing release

Published 10 September 2026. Google Play Console confirms **Active** and **Available to internal testers**.

- Release: 1.0 - Internal test - Test ads
- Version: 1.0 (version code 2)
- Package: com.herma.Cashflowgame
- Target API: 36; minimum API: 24; ARM64
- Tester link: https://play.google.com/apps/internaltest/4701390841343525746
- Bundle: `work/internal-testing/CashflowKingdom.aab`
- SHA-256: `6681D52E5B9AE886DF2470058523FDBD74112BA9341737CB5E0820A8623FF088`

Unity build succeeded with zero errors. Bundle signature verified. Google Play validation has no blocking errors; it reports missing deobfuscation and native debug-symbol files. The app is not reviewed and may appear under the temporary name `com.herma.Cashflowgame (unreviewed)`. Google says publication changes normally appear within one hour, occasionally longer.

Only the existing internal tester list is used. Production was not published. Google sample rewarded/banner ads remain enabled, and production privacy review remains disabled. No new physical-device test was performed for this bundle.

## Future builds and signing

Use **Cashflow > Build Internal Test Bundle (Test Ads)**. Increase the Android version code above 2 before the next upload. This build command requires sample ads and creates a signed, non-development bundle; ordinary production builds retain the privacy/configuration checks.

The upload key is `work/signing/cashflow-upload.jks`, alias `cashflow-upload`. Its generated password is in `work/signing/upload-password.txt`. Both files are excluded from Git by the existing `work/*` rule. Back up these two files securely together; keep the password private. They are needed for subsequent signed uploads. Do not delete the signing directory during build cleanup.

API 36 was installed into the existing local Unity Android SDK. The regular phone-APK helper still explicitly targets API 35; the internal-test helper sets API 36 for Play uploads.
