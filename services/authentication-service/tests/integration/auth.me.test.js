import { describe, it, expect, beforeAll } from 'vitest';
import request from 'supertest';
import { createApp } from '../../src/app.js';
import { runMigrations } from '../../src/infrastructure/migrations.js';
import { seedUsers } from '../../src/seed/users.seed.js';
import { signAccessToken, signRefreshToken } from '../../src/services/token.service.js';

const app = createApp();

const USERS = [
  { id: '8f3b2c1a-1d4e-4a5b-9c7d-2e6f8a0b1c2d', username: 'buyer.user', roles: ['Buyer'] },
  { id: '7e2a1b09-0c3d-3a4b-8b6c-1d5e7a9b0b1e', username: 'supplier.user', roles: ['Supplier'] },
  { id: '6d190a08-fb2c-2a3b-7a5b-0c4d6987a0d0', username: 'admin.user', roles: ['Admin', 'All'] },
];

beforeAll(async () => {
  runMigrations();
  await seedUsers();
});

const LOGIN = '/api/v1/auth/login';
const ENDPOINT = '/api/v1/auth/me';

describe('GET /api/v1/auth/me — happy path', () => {
  for (const user of USERS) {
    it(`returns profile for ${user.username}`, async () => {
      const loginRes = await request(app)
        .post(LOGIN)
        .send({ username: user.username, password: `${user.roles[0]}@123` });

      const { accessToken } = loginRes.body;

      const res = await request(app)
        .get(ENDPOINT)
        .set('Authorization', `Bearer ${accessToken}`);

      expect(res.status).toBe(200);
      expect(res.body.userId).toBe(user.id);
      expect(res.body.username).toBe(user.username);
      expect(res.body.roles).toEqual(user.roles);
      expect(res.body.tokenIssuedAt).toMatch(/^\d{4}-\d{2}-\d{2}T/);
      expect(res.body.tokenExpiresAt).toMatch(/^\d{4}-\d{2}-\d{2}T/);
    });
  }
});

describe('GET /api/v1/auth/me — error cases', () => {
  it('returns 401 with no Authorization header', async () => {
    const res = await request(app).get(ENDPOINT);

    expect(res.status).toBe(401);
    expect(res.headers['content-type']).toMatch(/application\/problem\+json/);
    expect(res.body.type).toMatch(/invalid-access-token/);
  });

  it('returns 401 with a malformed Bearer token', async () => {
    const res = await request(app)
      .get(ENDPOINT)
      .set('Authorization', 'Bearer not.a.token');

    expect(res.status).toBe(401);
    expect(res.body.type).toMatch(/invalid-access-token/);
  });

  it('returns 401 when a refresh token is used as access token', async () => {
    const refreshToken = signRefreshToken(USERS[0]);
    const res = await request(app)
      .get(ENDPOINT)
      .set('Authorization', `Bearer ${refreshToken}`);

    expect(res.status).toBe(401);
    expect(res.body.type).toMatch(/invalid-access-token/);
  });

  it('returns 401 with wrong scheme (Basic instead of Bearer)', async () => {
    const res = await request(app)
      .get(ENDPOINT)
      .set('Authorization', 'Basic dXNlcjpwYXNz');

    expect(res.status).toBe(401);
    expect(res.body.type).toMatch(/invalid-access-token/);
  });
});
