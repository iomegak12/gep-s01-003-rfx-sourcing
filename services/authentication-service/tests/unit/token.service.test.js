import { describe, it, expect } from 'vitest';
import jwt from 'jsonwebtoken';
import {
  signAccessToken,
  signRefreshToken,
  verifyAccessToken,
  verifyRefreshToken,
} from '../../src/services/token.service.js';
import { InvalidAccessTokenError, InvalidRefreshTokenError } from '../../src/errors/domain-errors.js';
import { config } from '../../src/config/env.js';

const SAMPLE_USER = {
  id: '8f3b2c1a-1d4e-4a5b-9c7d-2e6f8a0b1c2d',
  username: 'buyer.user',
  roles: ['Buyer'],
};

describe('token.service — access tokens', () => {
  it('signs and verifies an access token', () => {
    const token = signAccessToken(SAMPLE_USER);
    const payload = verifyAccessToken(token);

    expect(payload.sub).toBe(SAMPLE_USER.id);
    expect(payload.tokenType).toBe('access');
    expect(payload.roles).toEqual(SAMPLE_USER.roles);
    expect(payload.iss).toBe(config.JWT_ISSUER);
    expect(payload.aud).toBe(config.JWT_AUDIENCE);
  });

  it('throws InvalidAccessTokenError for a tampered signature', () => {
    const token = signAccessToken(SAMPLE_USER);
    const tampered = token.slice(0, -4) + 'XXXX';
    expect(() => verifyAccessToken(tampered)).toThrow(InvalidAccessTokenError);
  });

  it('throws InvalidAccessTokenError for an expired token', () => {
    const token = jwt.sign(
      { sub: SAMPLE_USER.id, tokenType: 'access', roles: SAMPLE_USER.roles },
      config.JWT_SIGNING_KEY,
      { issuer: config.JWT_ISSUER, audience: config.JWT_AUDIENCE, expiresIn: -1 }
    );
    expect(() => verifyAccessToken(token)).toThrow(InvalidAccessTokenError);
  });

  it('throws InvalidAccessTokenError for wrong issuer', () => {
    const token = jwt.sign(
      { sub: SAMPLE_USER.id, tokenType: 'access', roles: SAMPLE_USER.roles },
      config.JWT_SIGNING_KEY,
      { issuer: 'other-service', audience: config.JWT_AUDIENCE, expiresIn: 900 }
    );
    expect(() => verifyAccessToken(token)).toThrow(InvalidAccessTokenError);
  });

  it('throws InvalidAccessTokenError for wrong audience', () => {
    const token = jwt.sign(
      { sub: SAMPLE_USER.id, tokenType: 'access', roles: SAMPLE_USER.roles },
      config.JWT_SIGNING_KEY,
      { issuer: config.JWT_ISSUER, audience: 'other-audience', expiresIn: 900 }
    );
    expect(() => verifyAccessToken(token)).toThrow(InvalidAccessTokenError);
  });

  it('throws InvalidAccessTokenError when a refresh token is used as access', () => {
    const token = signRefreshToken(SAMPLE_USER);
    expect(() => verifyAccessToken(token)).toThrow(InvalidAccessTokenError);
  });
});

describe('token.service — refresh tokens', () => {
  it('signs and verifies a refresh token', () => {
    const token = signRefreshToken(SAMPLE_USER);
    const payload = verifyRefreshToken(token);

    expect(payload.sub).toBe(SAMPLE_USER.id);
    expect(payload.tokenType).toBe('refresh');
    expect(payload.roles).toBeUndefined();
    expect(payload.iss).toBe(config.JWT_ISSUER);
    expect(payload.aud).toBe(config.JWT_AUDIENCE);
  });

  it('throws InvalidRefreshTokenError for a tampered signature', () => {
    const token = signRefreshToken(SAMPLE_USER);
    const tampered = token.slice(0, -4) + 'YYYY';
    expect(() => verifyRefreshToken(tampered)).toThrow(InvalidRefreshTokenError);
  });

  it('throws InvalidRefreshTokenError for an expired token', () => {
    const token = jwt.sign(
      { sub: SAMPLE_USER.id, tokenType: 'refresh' },
      config.JWT_SIGNING_KEY,
      { issuer: config.JWT_ISSUER, audience: config.JWT_AUDIENCE, expiresIn: -1 }
    );
    expect(() => verifyRefreshToken(token)).toThrow(InvalidRefreshTokenError);
  });

  it('throws InvalidRefreshTokenError when an access token is used as refresh', () => {
    const token = signAccessToken(SAMPLE_USER);
    expect(() => verifyRefreshToken(token)).toThrow(InvalidRefreshTokenError);
  });
});
