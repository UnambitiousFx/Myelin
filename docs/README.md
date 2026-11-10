# Website

This website is built using [Docusaurus](https://docusaurus.io/).

```bash
pnpm install   # dependencies
pnpm start     # local dev server with live reload
pnpm build     # static site, plus llms.txt / llms-full.txt, in build/
pnpm typecheck
```

## Deployment

`.github/workflows/docs.yml` builds the site on every pull request that touches `docs/`, and deploys it to GitHub Pages
(`myelin.unambitiousfx.com`, see `static/CNAME`) on pushes to `main`, once the repository is public.
