# Cashflow Kingdom Mobile 1.0 (6)

Version code 6 includes the landing-only Payday popup fix: passing Payday still settles its payment, then resolves the destination directly without a salary popup or interstitial. Landing on Payday retains the popup and optional interstitial. The same rule applies to Fast Track Cashflow Day. Old saved passing-Payday breaks resolve without another payment.

The new finance logo and existing production ad IDs are retained. Unity 2022.3.62f3, ARM64 IL2CPP, Android API 36, and the existing upload signing key are used.

Files: `work/production-v6/CashflowKingdom.aab` and `work/production-v6/CashflowKingdom-1.0-v6-IL2CPP.symbols.zip`.

Save/resume and advertising regression suites passed after the fix; evidence is in `work/payday-landing-review/`. Build results, signature verification, native symbol build-ID checks and SHA-256 hashes are recorded in the release folder. No new physical-device test or Google Play upload was performed. Publisher privacy/consent settings are unchanged.
