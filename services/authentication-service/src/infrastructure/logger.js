import pino from 'pino';
import { config } from '../config/env.js';
import { APP_NAME } from '../config/constants.js';

export const logger = pino({
  level: config.LOG_LEVEL,
  base: { service: APP_NAME },
  timestamp: pino.stdTimeFunctions.isoTime,
});
