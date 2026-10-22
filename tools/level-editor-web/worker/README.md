# Level submission Worker

This Worker accepts account-free submissions from the editor, verifies the Turnstile response, validates the level JSON, then opens a pull request that adds the level under `levels/pending/` in `UMCreations/grid-golf-levels`. It never writes to the repository's default branch.

## Configure the Worker

1. Create a GitHub App and install it on `UMCreations/grid-golf-levels` only. Grant repository contents read/write and pull request read/write permissions.
2. Add `GITHUB_APP_ID` and `GITHUB_INSTALLATION_ID` as Worker variables. GitHub's downloaded RSA key can be converted to PKCS#8 with `openssl pkcs8 -topk8 -nocrypt -in app-key.pem -out app-key-pkcs8.pem`; store the converted key as the `GITHUB_APP_PRIVATE_KEY` secret.
3. Create a Turnstile widget for `umcreations.github.io`, `localhost`, and `127.0.0.1`. Store its secret as `TURNSTILE_SECRET`; put the public site key in the editor's `VITE_TURNSTILE_SITE_KEY` setting.
4. Production editor URL is `https://umcreations.github.io/grid-golf/editor/`. Keep `ALLOWED_ORIGINS` set to `https://umcreations.github.io` (origins do not include URL paths) and `ALLOWED_HOSTNAMES` set to `umcreations.github.io`, plus local development entries.
5. Set `VITE_LEVEL_SUBMISSION_URL` to the deployed Worker URL followed by `/submit`, and set `VITE_TURNSTILE_SITE_KEY` to the widget's site key in the `UMCreations/grid-golf` repository's Actions variables. Those are public client settings; keep the app private key and Turnstile secret in Worker secrets.

For local development, copy `.dev.vars.example` to `.dev.vars`, fill in the values, then run `npm run worker:dev`. Keep `.dev.vars` private. Deploy with `npm run worker:deploy` after configuring production variables and secrets.

The level repository must be initialized and its `main` branch must contain the catalog scaffold before submissions are enabled. Merging a submission PR only adds a pending level; maintainers must review it, move it to `levels/published/<mode>/`, and update `catalog.json` before the game can load it.
