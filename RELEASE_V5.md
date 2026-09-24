# Cashflow Kingdom Mobile 1.0 (5)

The crown artwork has been replaced with gold coin stacks and a flowing green upward arrow on a purple background. The opening screen and Android launcher use the new artwork. The old crown source is archived in `work/branding-backup`, outside packaged Resources.

Release files are in `work/production-v5/`:

- `CashflowKingdom.aab`: signed Android App Bundle, version code 5.
- `CashflowKingdom-1.0-v5-IL2CPP.symbols.zip`: matching public native symbols.
- `cashflow-google-play-icon.png`: 512 x 512 RGBA Google Play icon.
- `cashflow-logo-master.png`: original generated artwork.

This release includes the latest gameplay banner and Payday/Downsized interstitial changes described in `GAMEPLAY_ADS.md`. Production ad IDs remain enabled, including gameplay banner ending 6021118406 and interstitial ending 5841069288. Privacy/consent account settings were not changed by this branding task; existing pending review remains pending. No new physical-device ad test or Play upload was performed.

Build target: Unity 2022.3.62f3, Android ARM64 IL2CPP, API 36, existing upload signing key. Build succeeded with zero errors. The AAB signature verified, and build IDs match for all three native libraries (libmain, libunity, libil2cpp) in the symbols ZIP. Build and artifact verification results and SHA-256 hashes are recorded alongside the files.

## Artwork generation

Generated using the built-in image generation tool, then resized by Unity for platform icon sizes. Prompt:

> Create a polished professional mobile board-game app icon for Cashflow Kingdom, a financial education investing game. Square 1024x1024 composition, full bleed opaque rich deep plum purple background with subtle radial lighting. Central very clear bold emblem: three ascending stacks of warm gold coins and a graceful emerald teal curved arrow flowing upward around the stacks, expressing cash flow and progress. One front coin bears a simple embossed dollar mark. Premium playful 3D illustrated game art, clean bevels, crisp silhouettes, tasteful highlights, strong contrast, simple recognisable at 48px. Keep all important emblem inside central 65% for Android mask safety. No crown, no text or lettering except single dollar mark, no frame, no rounded outer corners, no external drop shadow, no watermark. Finish as production-ready beautiful app icon, not a mockup.
