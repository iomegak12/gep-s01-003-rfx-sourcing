import { AppError } from '../errors/app-error.js';
import { InternalError } from '../errors/domain-errors.js';
import { toProblemDetails } from '../errors/problem-details.js';
import { logger } from '../infrastructure/logger.js';

// Four-parameter signature required by Express to recognise this as an error handler.
// eslint-disable-next-line no-unused-vars
export function errorHandler(err, req, res, next) {
  const appErr = err instanceof AppError ? err : new InternalError();

  if (!(err instanceof AppError)) {
    logger.error({ err, correlationId: req.correlationId }, 'Unhandled error');
  }

  res
    .status(appErr.status)
    .type('application/problem+json')
    .json(toProblemDetails(appErr, req));
}
