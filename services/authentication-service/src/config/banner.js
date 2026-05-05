import chalk from 'chalk';
import boxen from 'boxen';
import { config } from './env.js';
import { APP_NAME, APP_DESCRIPTION, APP_VERSION } from './constants.js';

const GET = chalk.bold.green('GET ');
const POST = chalk.bold.yellow('POST');

const endpoints = [
  { method: POST, path: '/api/v1/auth/login',   desc: 'Authenticate; receive tokens' },
  { method: POST, path: '/api/v1/auth/refresh', desc: 'Exchange refresh token' },
  { method: GET,  path: '/api/v1/auth/me',      desc: 'Current user profile' },
  { method: GET,  path: '/api/v1/health',        desc: 'Liveness probe' },
];

export function printBanner(extras = {}) {
  const displayHost = config.HOST === '0.0.0.0' ? 'localhost' : config.HOST;
  const baseUrl = `http://${displayHost}:${config.PORT}`;

  const rateLimitStatus = config.RATE_LIMIT_ENABLED
    ? chalk.yellow(
        `enabled  (window: ${config.RATE_LIMIT_WINDOW_MS}ms, max: ${config.RATE_LIMIT_MAX})`
      )
    : chalk.dim('disabled');

  const dbStatus = extras.dbUserCount !== undefined
    ? chalk.green(`connected  (${extras.dbUserCount} users)`)
    : chalk.dim('not yet connected');

  const endpointLines = endpoints.map(
    ({ method, path, desc }) =>
      `    ${method}  ${chalk.white(path.padEnd(28))}${chalk.dim(desc)}`
  );

  const lines = [
    `${chalk.bold.cyan(APP_NAME)}  ${chalk.dim('v' + APP_VERSION)}`,
    chalk.dim(APP_DESCRIPTION),
    '',
    `  ${chalk.bold('Network        ')}  ${chalk.green(baseUrl)}`,
    `  ${chalk.bold('Environment    ')}  ${chalk.yellow(config.NODE_ENV)}`,
    `  ${chalk.bold('Log level      ')}  ${config.LOG_LEVEL}`,
    `  ${chalk.bold('Database       ')}  ${dbStatus}`,
    `  ${chalk.bold('Rate limiting  ')}  ${rateLimitStatus}`,
    '',
    `  ${chalk.bold('Endpoints')}`,
    ...endpointLines,
    '',
    `  ${chalk.bold('API docs       ')}  ${chalk.cyan(baseUrl + '/api/docs')}`,
  ];

  const banner = boxen(lines.join('\n'), {
    padding: { top: 1, bottom: 1, left: 2, right: 4 },
    margin: { top: 1, bottom: 0 },
    borderStyle: 'round',
    borderColor: 'cyan',
    title: ' RFx Auth Service ',
    titleAlignment: 'center',
  });

  console.log(banner);
}
