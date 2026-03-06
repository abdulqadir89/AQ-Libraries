/**
 * Build script for @AQ/react-components
 *
 * 1. Compiles TypeScript → JavaScript + declarations using tsc
 * 2. Copies non-TS assets (CSS files) to dist/ preserving directory structure
 */

import { execSync } from 'child_process';
import { cpSync, mkdirSync, rmSync, readdirSync, statSync } from 'fs';
import { join, relative, dirname } from 'path';

const SRC_DIR = 'src';
const DIST_DIR = 'dist';

// Step 0: Clean dist
console.log('🧹 Cleaning dist/...');
rmSync(DIST_DIR, { recursive: true, force: true });

// Step 1: Compile TypeScript
console.log('🔨 Compiling TypeScript...');
execSync('npx tsc --project tsconfig.build.json', { stdio: 'inherit' });

// Step 2: Copy CSS files
console.log('📋 Copying CSS files...');
function copyCssFiles(dir) {
  for (const entry of readdirSync(dir, { withFileTypes: true })) {
    const fullPath = join(dir, entry.name);
    if (entry.isDirectory()) {
      copyCssFiles(fullPath);
    } else if (entry.name.endsWith('.css')) {
      const rel = relative(SRC_DIR, fullPath);
      const dest = join(DIST_DIR, rel);
      mkdirSync(dirname(dest), { recursive: true });
      cpSync(fullPath, dest);
      console.log(`  ${rel}`);
    }
  }
}
copyCssFiles(SRC_DIR);

console.log('✅ Build complete!');
