import pinoHttp from 'pino-http';
import { logger } from '../infrastructure/logger.js';

export const requestLogger = pinoHttp({
  logger,
  customProps: (req) => ({ correlationId: req.correlationId }),
  customLogLevel: (_req, res, err) => {
    if (err || res.statusCode >= 500) return 'error';
    if (res.statusCode >= 400) return 'warn';
    return 'info';
  },
  serializers: {
    req: (req) => ({ method: req.method, url: req.url }),
    res: (res) => ({ statusCode: res.statusCode }),
  },
});
