import { describe, it, expect } from 'vitest';
import request from 'supertest';
import { createApp } from '../../src/app.js';
import { runMigrations } from '../../src/infrastructure/migrations.js';

const app = createApp();
runMigrations();

describe('GET /api/v1/health', () => {
  it('returns 200 with UP status', async () => {
    const res = await request(app).get('/api/v1/health');

    expect(res.status).toBe(200);
    expect(res.body.status).toBe('UP');
    expect(res.body.service).toBeDefined();
    expect(res.body.timestamp).toMatch(/^\d{4}-\d{2}-\d{2}T/);
  });
});

describe('GET /api/v1/missing', () => {
  it('returns 404 as Problem Details', async () => {
    const res = await request(app).get('/api/v1/missing');

    expect(res.status).toBe(404);
    expect(res.headers['content-type']).toMatch(/application\/problem\+json/);
    expect(res.body.status).toBe(404);
    expect(res.body.type).toMatch(/not-found/);
  });
});
