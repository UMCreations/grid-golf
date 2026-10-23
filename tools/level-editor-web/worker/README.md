# Level submission Worker

This Worker accepts account-free submissions from the editor, verifies the Turnstile response, validates the level JSON, then opens a pull request that adds the level under `levels/pending/` in `UMCreations/grid-golf-levels`. It never writes to the repository's default branch.

## Configure the Worker

1. Create a GitHub App and install it on `UMCreations/grid-golf-levels` only. Grant repository contents read/write and pull request read/write permissions.
2. Keep `GITHUB_APP_ID` and `GITHUB_INSTALLATION_ID` as Worker variables. Convert the downloaded RSA key to PKCS#8 with `openssl pkcs8 -topk8 -nocrypt -in app-key.pem -out app-key-pkcs8.pem`; store it as the `GITHUB_APP_PRIVATE_KEY` secret. `keep_vars = true` in the Wrangler config preserves dashboard-managed variables on deploy.
3. In the GitHub App's **General** settings, set the OAuth callback URL to `https://grid-golf-level-submissions.puzzlegolf-level-editor-web.workers.dev/admin/auth/callback`. Under **Credentials → Client secrets**, create a client secret and store it in the Worker as `GITHUB_APP_CLIENT_SECRET`. The public client ID and callback URL are already in `wrangler.toml`.
4. Set these Worker secrets (never GitHub Actions variables):

   ```sh
   npx wrangler secret put GITHUB_APP_CLIENT_SECRET --config wrangler.toml
   npx wrangler secret put ADMIN_SESSION_SECRET --config wrangler.toml
   ```

   For `ADMIN_SESSION_SECRET`, generate a fresh value with `openssl rand -hex 32` and paste it only into the Wrangler prompt. Keep the existing `GITHUB_APP_PRIVATE_KEY` and `TURNSTILE_SECRET` as Worker secrets too.
5. Create a Turnstile widget for `umcreations.github.io`, `localhost`, and `127.0.0.1`. Store its secret as `TURNSTILE_SECRET`; put the public site key in the editor's `VITE_TURNSTILE_SITE_KEY` setting.
6. Production editor URL is `https://umcreations.github.io/grid-golf/editor/`. Keep `ALLOWED_ORIGINS` set to `https://umcreations.github.io` (origins do not include URL paths) and `ALLOWED_HOSTNAMES` set to `umcreations.github.io`, plus local development entries.
7. Set `VITE_LEVEL_SUBMISSION_URL` to the deployed Worker URL followed by `/submit`, and `VITE_TURNSTILE_SITE_KEY` to the widget's site key in the `UMCreations/grid-golf` repository's Actions variables. These are public client settings; never put Worker secrets there.

## Reviewing and publishing levels

After deployment, open the editor and choose **Review Pending**. Sign in with the GitHub account configured by `ADMIN_GITHUB_LOGIN` (`UMCreations` in the production config). The Worker lists JSON files already merged into `levels/pending/` on `main` and returns a board preview with structural and solvability checks.

Choose **Move to Published** to create a pull request that moves the level JSON to `levels/published/<mode>/` and adds its entry to `catalog.json`. Merge that PR to publish the level. The browser receives only a short-lived admin session; the GitHub App client secret, private key, and repository write token stay in the Worker.

For local development, copy `.dev.vars.example` to `.dev.vars`, fill in the values, then run `npm run worker:dev`. Keep `.dev.vars` private. Admin sign-in is configured for production; to test OAuth locally, use a separate GitHub App with a callback URL matching the local Worker. Deploy with `npm run worker:deploy` after configuring production variables and secrets.

The level repository must be initialized and its `main` branch must contain the catalog scaffold before submissions are enabled. Merging a submission PR only adds a pending level; the review tool creates a separate publish PR for the maintainer to merge.
