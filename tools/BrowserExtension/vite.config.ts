import { defineConfig } from 'vite';
import { resolve } from 'node:path';
import react from '@vitejs/plugin-react';
import AutoImport from "unplugin-auto-import/vite";
import AntdResolver from "unplugin-antd-resolver";
import fs from 'node:fs';

const r = (...p: string[]) => resolve(process.cwd(), ...p);

const targetProfile = process.env.TARGET || 'chromium.mv3';

// 可选: 按需扫描目录生成入口 (保持最小侵入，默认不启用)
function scanEntries() {
  // 示例: 将 scripts 目录下的每个 ts/js 当作入口
  const base = r('scripts');
  if (!fs.existsSync(base)) return {};
  return Object.fromEntries(
    fs.readdirSync(base)
      .filter(f => /\.(ts|js|mjs)$/.test(f))
      .map(f => {
        const name = f.replace(/\.(ts|js|mjs)$/, '');
        return [name, resolve(base, f)];
      })
  );
}

export default defineConfig({
  build: {
    outDir: 'dist',
    emptyOutDir: true,
    target: 'es2022',
    rollupOptions: {
      preserveEntrySignatures: 'strict',
    }
  },
  optimizeDeps: {
    include: ["react", "react-dom", "antd"]
  },
  plugins: [
    react(),
    AutoImport({
      include: [
        /[\\/]src[\\/]pages[\\/].*\.[jt]sx?$/,
      ],
      resolvers: [AntdResolver()],
    }),
  ],
  resolve: {
    alias: {
      '@runtime': r('src/runtime'),
      '@config': r('src/config'),
      '@addins': r('src/addins'),
      '@manifest': r('src/manifest'),
      '@utils': r('src/utils'),
      '@assets': r('src/assets'),
      '@background': r('src/background'),
      '@content': r('src/content'),
      '@type': r('src/types'),
      '@pages': r('src/pages'),
      '@scripts': r('scripts'),
      '@': r('src'),
    }
  },
  define: {
    __TARGET_PROFILE__: JSON.stringify(targetProfile),
  },
});
