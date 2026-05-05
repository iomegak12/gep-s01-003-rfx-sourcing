import { findByUsername, findById, updatePasswordHash } from '../repositories/user.repository.js';
import { verify, hash, needsRehash } from './password.service.js';
import { signAccessToken, signRefreshToken, verifyRefreshToken } from './token.service.js';
import {
  InvalidCredentialsError,
  InvalidAccessTokenError,
  InvalidRefreshTokenError,
} from '../errors/domain-errors.js';
import { config } from '../config/env.js';

export async function login({ username, password }) {
  const user = findByUsername(username);

  if (!user) {
    throw new InvalidCredentialsError();
  }

  const valid = await verify(user.password_hash, password);
  if (!valid) {
    throw new InvalidCredentialsError();
  }

  // Opportunistic rehash when Argon2 params have been upgraded.
  if (needsRehash(user.password_hash)) {
    const newHash = await hash(password);
    updatePasswordHash(user.id, newHash);
  }

  const accessToken = signAccessToken(user);
  const refreshToken = signRefreshToken(user);

  return {
    accessToken,
    refreshToken,
    tokenType: 'Bearer',
    accessTokenExpiresInSeconds: config.JWT_ACCESS_TOKEN_LIFETIME_SECONDS,
    refreshTokenExpiresInSeconds: config.JWT_REFRESH_TOKEN_LIFETIME_SECONDS,
  };
}

export async function refresh({ refreshToken }) {
  let payload;
  try {
    payload = verifyRefreshToken(refreshToken);
  } catch {
    throw new InvalidRefreshTokenError();
  }

  const user = findById(payload.sub);
  if (!user) {
    throw new InvalidRefreshTokenError();
  }

  // Re-read roles from DB so any role changes take effect immediately.
  const accessToken = signAccessToken(user);

  return {
    accessToken,
    tokenType: 'Bearer',
    accessTokenExpiresInSeconds: config.JWT_ACCESS_TOKEN_LIFETIME_SECONDS,
  };
}

export function me({ sub, iat, exp }) {
  const user = findById(sub);
  if (!user) {
    throw new InvalidAccessTokenError('User not found.');
  }

  return {
    userId: user.id,
    username: user.username,
    roles: user.roles,
    tokenIssuedAt: new Date(iat * 1000).toISOString(),
    tokenExpiresAt: new Date(exp * 1000).toISOString(),
  };
}
