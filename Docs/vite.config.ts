import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import mdx from '@mdx-js/rollup';
import remarkGfm from 'remark-gfm';
import rehypeSlug from 'rehype-slug';
import { resolve } from 'path';

export default defineConfig(({ command }) => {
  /**
   * Base path rules:
   * - Dev server (`vite`) should run at `/` for ergonomic local URLs.
   * - Production build (GitHub Pages) needs `/<repo>/` (or an override).
   */
  const base =
    command === 'serve'
      ? '/'
      : (process.env.VITE_BASE_PATH ?? '/BloodCraftPlus/');

  return {
    base,
  plugins: [
    {
      enforce: 'pre',
      ...mdx({
        remarkPlugins: [remarkGfm],
        rehypePlugins: [rehypeSlug],
        providerImportSource: '@mdx-js/react',
      }),
    },
    react(),
  ],
  resolve: {
    alias: {
      '@': resolve(__dirname, 'src'),
    },
  },
  build: {
    outDir: 'dist',
    sourcemap: false,
  },
  };
});
