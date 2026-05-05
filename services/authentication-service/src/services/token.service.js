import jwt from 'jsonwebtoken';
import { config } from '../config/env.js';
import { InvalidAccessTokenError, InvalidRefreshTokenError } from '../errors/domain-errors.js';

const BASE_PAYLOAD = {
  iss: config.JWT_ISSUER,
  aud: config.JWT_AUDIENCE,
};

export function signAccessToken(user) {
  return jwt.sign(
    {
      ...BASE_PAYLOAD,
      sub: user.id,
      tokenType: 'access',
      roles: user.roles,
    },
    config.JWT_SIGNING_KEY,
    { expiresIn: config.JWT_ACCESS_TOKEN_LIFETIME_SECONDS }
  );
}

export function signRefreshToken(user) {
  return jwt.sign(
    {
      ...BASE_PAYLOAD,
      sub: user.id,
      tokenType: 'refresh',
    },
    config.JWT_SIGNING_KEY,
    { expiresIn: config.JWT_REFRESH_TOKEN_LIFETIME_SECONDS }
  );
}

function decode(token, expectedType, ErrorClass) {
  let payload;
  try {
    payload = jwt.verify(token, config.JWT_SIGNING_KEY, {
      issuer: config.JWT_ISSUER,
      audience: config.JWT_AUDIENCE,
    });
  } catch (err) {
    throw new ErrorClass(err.message);
  }

  if (payload.tokenType !== expectedType) {
    throw new ErrorClass(`Expected tokenType '${expectedType}', got '${payload.tokenType}'.`);
  }

  return payload;
}

export function verifyAccessToken(token) {
  return decode(token, 'access', InvalidAccessTokenError);
}

export function verifyRefreshToken(token) {
  return decode(token, 'refresh', InvalidRefreshTokenError);
}
