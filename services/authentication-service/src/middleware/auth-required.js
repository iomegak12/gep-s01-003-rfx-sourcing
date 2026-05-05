import { verifyAccessToken } from '../services/token.service.js';
import { InvalidAccessTokenError } from '../errors/domain-errors.js';

export function authRequired(req, _res, next) {
  const authHeader = req.headers['authorization'] ?? '';
  const [scheme, token] = authHeader.split(' ');

  if (scheme !== 'Bearer' || !token) {
    return next(new InvalidAccessTokenError('Authorization header must use Bearer scheme.'));
  }

  try {
    req.user = verifyAccessToken(token);
  } catch (err) {
    return next(err);
  }

  next();
}
