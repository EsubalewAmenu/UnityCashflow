# Firebase multiplayer integration

The networking target is Firebase Authentication, Cloud Firestore and callable Cloud Functions. The previous self-hosted ASP.NET scaffold was removed before any server was implemented or deployed.

These files implement the initial lobby backend, not a functioning online game. `functions/index.js` provides transactional create, join, ready, leave and resume callable operations using authenticated UIDs and retry receipts. `functions/catalog.json` captures the existing professions, dreams, board spaces and deals for trusted backend use. `firestore.rules` permits a signed-in room member to read only that room's sanitized view and denies all client writes and collection listing. Private authoritative state, deck ordering and receipts stay inaccessible to clients. Firebase project `cashflow-kingdom-9ff53` has been created on Spark. Android app `com.herma.Cashflowgame` is registered as Cashflow Kingdom Mobile (App ID `1:332567624526:android:038cd663f55f6fba627c1f`). Anonymous Authentication is enabled. Functions and rules have not been deployed.

Validation: `node --test Firebase/functions/room.test.js` passes 16 pure lobby tests, including expired-room departure, stale revisions, authorization and duplicate receipts. `node --check Firebase/functions/index.js` passes syntax validation. Auth + Firestore emulator security checks passed: unauthenticated reads, outsider reads and member writes were rejected, while member reads succeeded. Firestore emulator transaction integration passed (competing joins, deduplication, stale revision rejection, resume, host transfer, and closing the room). Offline Unity parsing/request checks passed; real-device network testing is still pending. Dependencies are installed and locked in package-lock.json; the callable module loads successfully with the Firebase SDKs. Local checks ran under Node 24; deployed runtime remains Node 22. Match start, authoritative gameplay commands and online UI remain to be implemented. Receipt TTL cleanup and abuse limits must be configured before deployment; maxInstances alone is not a spending cap.

## Integration contract

- Authentication supplies the player UID; never trust a client-supplied player ID.
- Callable functions create/join private two-player rooms, change readiness and start the match only when both players are ready and the caller is the host.
- Firestore room listeners update lobby and board presentation without polling.
- Commands include a request ID, expected match revision and decision ID. A transaction checks membership/turn ownership, deduplicates requests, applies trusted game rules and writes state, receipt and sanitized room view together.
- Dice, deck shuffling, financial amounts, movement settlement and automatic events run in functions. Clients submit intent only.
- Reconnection restores the authenticated seat and current snapshot. Unconfirmed commands retry with the same request ID.
- Single-player and local pass-and-play remain independent. Multiplayer fullscreen/reward ads stay disabled until a server-verified reward path exists.
- Enforce App Check on callable functions before public release. Emulator testing must not require production credentials.

## Configuration still needed

The project is bound in `.firebaserc`. The Android configuration was downloaded through the authorized Firebase CLI into Assets/google-services.json and its project/package were verified. Firebase Unity SDK 13.17.0 (App, Auth, Firestore, Functions, App Check) is installed through local UPM archives. CashflowFirebaseLobby provides guest sign-in, command submission, room listening and resume requests. It is not yet connected to the player-facing opening menu. Unity 2022.3.62f3 compilation and FirebaseLobbyReview passed in the isolated validation project. Register iOS in the same project if shipping there. Firestore Standard `(default)` was created in `europe-west1` (Belgium) on 2026-10-10 after the user selected the region. Production-mode rules deny client access until tested room rules are deployed. Scheduled backups remain disabled. The user upgraded the project to Blaze; the refreshed console confirmed Blaze on 2026-10-10. Do not add a service-account private key to Unity or source control.

Cloud Functions deployment requires the Firebase Blaze billing plan. This is managed Firebase infrastructure, not a server to maintain. The user performed the Blaze upgrade. Firebase CLI authorization succeeded for the project owner on 2026-10-10; no functions or custom room rules have been deployed. Deployment attempts on 2026-10-10 failed on outbound Google API requests (Firebase Rules test endpoint and Cloud Runtime Config); rules compilation succeeded in one attempt but publication did not complete. Retry the CLI deployment after connectivity/service recovery.

## References

- https://firebase.google.com/docs/unity/setup
- https://firebase.google.com/docs/functions/callable
- https://firebase.google.com/docs/firestore/security/rules-conditions
- https://firebase.google.com/docs/functions/get-started

Android Auth includes Analytics transitively; CashflowPrivacy.androidlib disables Analytics collection. An Android build/merged-manifest check and real-device App Check registration are still required before release.
