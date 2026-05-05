import { describe, it, expect, beforeAll } from 'vitest';
import request from 'supertest';
import { createApp } from '../../src/app.js';
import { runMigrations } from '../../src/infrastructure/migrations.js';
import { seedUsers } from '../../src/seed/users.seed.js';

const app = createApp();

beforeAll(async () => {
  runMigrations();
  await seedUsers();
});

const ENDPOINT = '/api/v1/auth/login';

const CREDENTIALS = [
  { username: 'buyer.user', password: 'Buyer@123', roles: ['Buyer'] },
  { username: 'supplier.user', password: 'Supplier@123', roles: ['Supplier'] },
  { username: 'admin.user', password: 'Admin@123', roles: ['Admin', 'All'] },
];

describe('POST /api/v1/auth/login — happy path', () => {
  for (const { username, password, roles } of CREDENTIALS) {
    it(`issues tokens for ${username}`, async () => {
      const res = await request(app).post(ENDPOINT).send({ username, password });

      expect(res.status).toBe(200);
      expect(res.body.accessToken).toBeDefined();
      expect(res.body.refreshToken).toBeDefined();
      expect(res.body.tokenType).toBe('Bearer');
      expect(res.body.accessTokenExpiresInSeconds).toBeTypeOf('number');
      expect(res.body.refreshTokenExpiresInSeconds).toBeTypeOf('number');

      // Verify JWT claims
      const [, payloadB64] = res.body.accessToken.split('.');
      const payload = JSON.parse(Buffer.from(payloadB64, 'base64url').toString());
      expect(payload.tokenType).toBe('access');
      expect(payload.roles).toEqual(roles);
      expect(payload.iss).toBe('rfx-auth-service');
      expect(payload.aud).toBe('rfx-sourcing-system');
    });
  }
});

describe('POST /api/v1/auth/login — error cases', () => {
  it('returns 400 when username is missing', async () => {
    const res = await request(app).post(ENDPOINT).send({ password: 'Buyer@123' });

    expect(res.status).toBe(400);
    expect(res.headers['content-type']).toMatch(/application\/problem\+json/);
    expect(res.body.status).toBe(400);
  });

  it('returns 400 when password is missing', async () => {
    const res = await request(app).post(ENDPOINT).send({ username: 'buyer.user' });

    expect(res.status).toBe(400);
    expect(res.headers['content-type']).toMatch(/application\/problem\+json/);
  });

  it('returns 400 when body is empty', async () => {
    const res = await request(app).post(ENDPOINT).send({});

    expect(res.status).toBe(400);
    expect(res.headers['content-type']).toMatch(/application\/problem\+json/);
  });

  it('returns 401 for a wrong password', async () => {
    const res = await request(app)
      .post(ENDPOINT)
      .send({ username: 'buyer.user', password: 'WrongPassword!' });

    expect(res.status).toBe(401);
    expect(res.headers['content-type']).toMatch(/application\/problem\+json/);
    expect(res.body.type).toMatch(/invalid-credentials/);
  });

  it('returns 401 for an unknown username', async () => {
    const res = await request(app)
      .post(ENDPOINT)
      .send({ username: 'nobody', password: 'anything' });

    expect(res.status).toBe(401);
    expect(res.body.type).toMatch(/invalid-credentials/);
  });
});
