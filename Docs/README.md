# BloodUI Documentation

This is the documentation site for **BloodUI**, a client-side UI companion for the [BloodCraft](https://thunderstore.io/c/v-rising/p/zfolmt/Bloodcraft/) V Rising mod.

## Development

```bash
# Install dependencies
npm install

# Start development server
npm run dev

# Build for production
npm run build
```

## Structure

```
src/
├── content/           # MDX documentation pages
│   ├── index.mdx     # Homepage
│   ├── getting-started/
│   ├── features/     # UI feature docs
│   ├── reference/    # Config and changelog
│   └── contributing/
├── components/        # React components
├── docs.config.ts     # Navigation config
└── App.tsx           # Main app
```

## Deployment

The site deploys automatically to GitHub Pages when pushing to main.

## License

MIT
