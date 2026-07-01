import { execSync } from 'node:child_process';
import { existsSync, readFileSync } from 'node:fs';
import { homedir } from 'node:os';
import { fileURLToPath } from 'node:url';
import { dirname, join } from 'node:path';

if (process.env.PLAYWRIGHT_SKIP_INSTALL === '1') {
  process.exit(0);
}

const scriptDir = dirname(fileURLToPath(import.meta.url));
const playwrightCli = join(scriptDir, '..', 'node_modules', '@playwright', 'test', 'cli.js');

if (!existsSync(playwrightCli)) {
  process.exit(0);
}

const { browsers } = JSON.parse(
  readFileSync(join(scriptDir, '..', 'node_modules', 'playwright-core', 'browsers.json'), 'utf8'),
);
const headlessShell = browsers.find((browser) => browser.name === 'chromium-headless-shell');

function defaultBrowsersPath() {
  if (process.env.PLAYWRIGHT_BROWSERS_PATH) {
    return process.env.PLAYWRIGHT_BROWSERS_PATH;
  }

  if (process.platform === 'win32') {
    return join(process.env.LOCALAPPDATA ?? join(homedir(), 'AppData', 'Local'), 'ms-playwright');
  }

  if (process.platform === 'darwin') {
    return join(homedir(), 'Library', 'Caches', 'ms-playwright');
  }

  return join(homedir(), '.cache', 'ms-playwright');
}

function headlessShellExecutable() {
  const folder = `chromium_headless_shell-${headlessShell.revision}`;
  const base = join(defaultBrowsersPath(), folder);

  if (process.platform === 'win32') {
    return join(base, 'chrome-headless-shell-win64', 'chrome-headless-shell.exe');
  }

  if (process.platform === 'darwin') {
    const arch = process.arch === 'arm64' ? 'mac-arm64' : 'mac-x64';
    return join(base, `chrome-headless-shell-${arch}`, 'chrome-headless-shell');
  }

  const arch = process.arch === 'arm64' ? 'linux-arm64' : 'linux-x64';
  return join(base, `chrome-headless-shell-${arch}`, 'chrome-headless-shell');
}

const installArgs = process.env.CI
  ? ['install', '--with-deps', 'chromium']
  : ['install', 'chromium'];

if (!existsSync(headlessShellExecutable())) {
  installArgs.push('--force');
}

execSync(`node "${playwrightCli}" ${installArgs.join(' ')}`, { stdio: 'inherit' });
