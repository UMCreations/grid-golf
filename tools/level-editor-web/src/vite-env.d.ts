interface ImportMetaEnv {
  readonly VITE_LEVEL_SUBMISSION_URL?: string;
  readonly VITE_TURNSTILE_SITE_KEY?: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}
