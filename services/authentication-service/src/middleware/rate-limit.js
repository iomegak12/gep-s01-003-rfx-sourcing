import rateLimit from 'express-rate-limit';
import { config } from '../config/env.js';

function problemDetails429(req, res) {
  res.status(429).set('Content-Type', 'application/problem+json').json({
    type: 'https://rfx.gep.local/probs/rate-limit-exceeded',
    title: 'Too Many Requests',
    status: 429,
    detail: 'You have exceeded the allowed request rate. Please wait before retrying.',
    instance: req.originalUrl || req.url,
    correlationId: req.correlationId,
  });
}

/**
 * Returns an express-rate-limit middleware when enabled, or a no-op passthrough.
 * Accepts optional overrides so tests can inject a low threshold without touching env.
 */
export function createRateLimiter(opts = {}) {
  const enabled = opts.enabled ?? config.RATE_LIMIT_ENABLED;

  if (!enabled) {
    return (_req, _res, next) => next();
  }

  return rateLimit({
    windowMs: opts.windowMs ?? config.RATE_LIMIT_WINDOW_MS,
    max: opts.max ?? config.RATE_LIMIT_MAX,
    standardHeaders: true,
    legacyHeaders: false,
    handler: problemDetails429,
  });
}
