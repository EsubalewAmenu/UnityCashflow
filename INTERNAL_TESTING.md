# Internal testing release

Published 10 September 2026. Google Play Console confirms **Active** and **Available to internal testers**.

- Release: 1.0 (3) - Unity security update
- Version: 1.0 (version code 3)
- Package: com.herma.Cashflowgame
- Target API: 36; minimum API: 24; ARM64
- Tester link: https://play.google.com/apps/internaltest/4701390841343525746
- Bundle: `work/internal-testing/CashflowKingdom.aab`
- SHA-256: `A1171551F9FC9070B0222142314F65EBB1F4B70F809438430E40016FFD7C672F`

Unity build succeeded with zero errors. Bundle signature verified. Google Play validation has no blocking errors; native debug symbols have now been attached, and the optional Java deobfuscation warning remains because minification is disabled. The app is not reviewed and may appear under the temporary name `com.herma.Cashflowgame (unreviewed)`. Google says publication changes normally appear within one hour, occasionally longer.

Only the existing internal tester list is used. Production was not published. Google sample rewarded/banner ads remain enabled, and production privacy review remains disabled. No new physical-device test was performed for this bundle.

## Future builds and signing

Use **Cashflow > Build Internal Test Bundle (Test Ads)**. Increase the Android version code above 3 before the next upload. This build command requires sample ads and creates a signed, non-development bundle; ordinary production builds retain the privacy/configuration checks.

The upload key is `work/signing/cashflow-upload.jks`, alias `cashflow-upload`. Its generated password is in `work/signing/upload-password.txt`. Both files are excluded from Git by the existing `work/*` rule. Back up these two files securely together; keep the password private. They are needed for subsequent signed uploads. Do not delete the signing directory during build cleanup.

API 36 was installed into the existing local Unity Android SDK. Both phone and internal-test helpers target API 36. Use the patched Unity 2022.3.62f3 editor. See PRODUCTION_READINESS.md for current production status.
