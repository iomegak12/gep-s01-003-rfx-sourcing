import { describe, it, expect, beforeAll } from 'vitest';
import request from 'supertest';
import { createApp } from '../../src/app.js';
import { runMigrations } from '../../src/infrastructure/migrations.js';
import { seedUsers } from '../../src/seed/users.seed.js';
import { signAccessToken, signRefreshToken } from '../../src/services/token.service.js';

const app = createApp();

const BUYER = {
  id: '8f3b2c1a-1d4e-4a5b-9c7d-2e6f8a0b1c2d',
  username: 'buyer.user',
  roles: ['Buyer'],
};

beforeAll(async () => {
  runMigrations();
  await seedUsers();
});

const LOGIN = '/api/v1/auth/login';
const ENDPOINT = '/api/v1/auth/refresh';

describe('POST /api/v1/auth/refresh — happy path', () => {
  it('issues a new access token using a valid refresh token', async () => {
    const loginRes = await request(app)
      .post(LOGIN)
      .send({ username: 'buyer.user', password: 'Buyer@123' });

    const { refreshToken } = loginRes.body;

    const res = await request(app).post(ENDPOINT).send({ refreshToken });

    expect(res.status).toBe(200);
    expect(res.body.accessToken).toBeDefined();
    expect(res.body.tokenType).toBe('Bearer');
    expect(res.body.accessTokenExpiresInSeconds).toBeTypeOf('number');

    const [, payloadB64] = res.body.accessToken.split('.');
    const payload = JSON.parse(Buffer.from(payloadB64, 'base64url').toString());
    expect(payload.tokenType).toBe('access');
    expect(payload.sub).toBe(BUYER.id);
    expect(payload.roles).toEqual(BUYER.roles);
  });
});

describe('POST /api/v1/auth/refresh — error cases', () => {
  it('returns 400 when refreshToken is missing', async () => {
    const res = await request(app).post(ENDPOINT).send({});

    expect(res.status).toBe(400);
    expect(res.headers['content-type']).toMatch(/application\/problem\+json/);
  });

  it('returns 401 for a malformed token', async () => {
    const res = await request(app).post(ENDPOINT).send({ refreshToken: 'not.a.token' });

    expect(res.status).toBe(401);
    expect(res.headers['content-type']).toMatch(/application\/problem\+json/);
    expect(res.body.type).toMatch(/invalid-refresh-token/);
  });

  it('returns 401 when an access token is used as refresh token', async () => {
    const accessToken = signAccessToken(BUYER);
    const res = await request(app).post(ENDPOINT).send({ refreshToken: accessToken });

    expect(res.status).toBe(401);
    expect(res.body.type).toMatch(/invalid-refresh-token/);
  });
});
