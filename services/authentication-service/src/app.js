import express from 'express';
import helmet from 'helmet';
import cors from 'cors';
import { correlationId } from './middleware/correlation-id.js';
import { requestLogger } from './middleware/request-logger.js';
import { errorHandler } from './middleware/error-handler.js';
import { createRateLimiter } from './middleware/rate-limit.js';
import { AppError } from './errors/app-error.js';
import apiRoutes from './routes/index.js';
import docsRoutes from './routes/docs.routes.js';

export function createApp(opts = {}) {
  const app = express();

  app.use(helmet());
  app.use(cors());
  app.use(express.json());

  // Correlation ID must precede request logger so the ID appears in every log line.
  app.use(correlationId);
  app.use(requestLogger);

  // Rate limiting scoped to the abuse-prone auth endpoints only.
  const authLimiter = createRateLimiter(opts.rateLimit ?? {});
  app.use('/api/v1/auth/login', authLimiter);
  app.use('/api/v1/auth/refresh', authLimiter);

  app.use('/api/v1', apiRoutes);
  app.use('/api', docsRoutes);

  // 404 for any route not matched above — forwarded to the global error handler.
  app.use((req, _res, next) => {
    next(new AppError('Not Found', 404, `${req.method} ${req.url} does not exist.`, 'not-found'));
  });

  // Global RFC 7807 error handler — must be last.
  app.use(errorHandler);

  return app;
}
