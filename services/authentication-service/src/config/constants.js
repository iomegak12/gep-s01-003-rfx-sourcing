import { createRequire } from 'module';
import { fileURLToPath } from 'url';
import { dirname, join } from 'path';
import { config } from './env.js';

const _require = createRequire(import.meta.url);
const _dir = dirname(fileURLToPath(import.meta.url));
const pkg = _require(join(_dir, '../../package.json'));

export const APP_NAME = config.APP_NAME;
export const APP_DESCRIPTION = config.APP_DESCRIPTION;
export const APP_VERSION = pkg.version;
