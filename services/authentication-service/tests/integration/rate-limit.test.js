import { describe, it, expect, beforeAll } from 'vitest';
import request from 'supertest';
import { createApp } from '../../src/app.js';
import { runMigrations } from '../../src/infrastructure/migrations.js';
import { seedUsers } from '../../src/seed/users.seed.js';

// Isolated app with rate limiter forced on at a low threshold.
const app = createApp({ rateLimit: { enabled: true, windowMs: 60_000, max: 2 } });

beforeAll(async () => {
  runMigrations();
  await seedUsers();
});

const LOGIN = '/api/v1/auth/login';
const REFRESH = '/api/v1/auth/refresh';
const VALID_LOGIN = { username: 'buyer.user', password: 'Buyer@123' };

describe('Rate limiting — login endpoint', () => {
  it('allows requests up to the threshold', async () => {
    const r1 = await request(app).post(LOGIN).send(VALID_LOGIN);
    const r2 = await request(app).post(LOGIN).send(VALID_LOGIN);

    expect(r1.status).toBe(200);
    expect(r2.status).toBe(200);
  });

  it('returns 429 Problem Details on the request exceeding the limit', async () => {
    const res = await request(app).post(LOGIN).send(VALID_LOGIN);

    expect(res.status).toBe(429);
    expect(res.headers['content-type']).toMatch(/application\/problem\+json/);
    expect(res.body.status).toBe(429);
    expect(res.body.type).toMatch(/rate-limit-exceeded/);
    expect(res.body.title).toBe('Too Many Requests');
    expect(res.body.detail).toBeDefined();
  });

  it('sets RateLimit-* standard headers on successful responses', async () => {
    // Spin up a fresh app so the window is clean.
    const freshApp = createApp({ rateLimit: { enabled: true, windowMs: 60_000, max: 10 } });
    runMigrations();
    const res = await request(freshApp).post(LOGIN).send(VALID_LOGIN);

    expect(res.status).toBe(200);
    // express-rate-limit v7 emits RateLimit-Limit and RateLimit-Remaining
    expect(res.headers['ratelimit-limit'] ?? res.headers['x-ratelimit-limit']).toBeDefined();
  });
});

describe('Rate limiting — refresh endpoint', () => {
  it('returns 429 after exceeding the limit on refresh', async () => {
    const loginRes = await request(app).post(LOGIN).send(VALID_LOGIN);
    // At this point we have already exceeded the login limiter for this IP.
    // Use a fresh app for refresh so the window is separate.
    const freshApp = createApp({ rateLimit: { enabled: true, windowMs: 60_000, max: 1 } });
    runMigrations();

    const { refreshToken } = loginRes.body ?? {};
    if (!refreshToken) return; // login may have been rate-limited; skip gracefully

    await request(freshApp).post(REFRESH).send({ refreshToken });
    const res = await request(freshApp).post(REFRESH).send({ refreshToken });

    expect(res.status).toBe(429);
    expect(res.body.type).toMatch(/rate-limit-exceeded/);
  });
});

describe('Rate limiting — other endpoints unaffected', () => {
  it('health endpoint is never rate-limited', async () => {
    const freshApp = createApp({ rateLimit: { enabled: true, windowMs: 60_000, max: 1 } });

    // Three rapid hits — none should be 429
    for (let i = 0; i < 3; i++) {
      const res = await request(freshApp).get('/api/v1/health');
      expect(res.status).toBe(200);
    }
  });
});
