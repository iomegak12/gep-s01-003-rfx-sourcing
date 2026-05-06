import { describe, it, expect } from 'vitest';
import request from 'supertest';
import { createApp } from '../../src/app.js';
import { createRequire } from 'module';

const require = createRequire(import.meta.url);
const { version } = require('../../package.json');

const app = createApp();

describe('GET /api/openapi.json', () => {
  it('returns 200 with JSON content', async () => {
    const res = await request(app).get('/api/openapi.json');

    expect(res.status).toBe(200);
    expect(res.headers['content-type']).toMatch(/application\/json/);
  });

  it('version matches package.json', async () => {
    const res = await request(app).get('/api/openapi.json');

    expect(res.body.info.version).toBe(version);
  });

  it('spec covers all five endpoints including versioned spec route', async () => {
    const res = await request(app).get('/api/openapi.json');
    const paths = Object.keys(res.body.paths);

    expect(paths).toContain('/auth/login');
    expect(paths).toContain('/auth/refresh');
    expect(paths).toContain('/auth/me');
    expect(paths).toContain('/health');
    expect(paths).toContain('/openapi.json');
  });

  it('ProblemDetails schema is defined as a reusable component', async () => {
    const res = await request(app).get('/api/openapi.json');

    expect(res.body.components.schemas.ProblemDetails).toBeDefined();
  });

  it('bearerAuth security scheme is defined', async () => {
    const res = await request(app).get('/api/openapi.json');

    expect(res.body.components.securitySchemes.bearerAuth).toMatchObject({
      type: 'http',
      scheme: 'bearer',
      bearerFormat: 'JWT',
    });
  });
});

describe('GET /api/v1/openapi.json', () => {
  it('returns same spec as /api/openapi.json', async () => {
    const r1 = await request(app).get('/api/openapi.json');
    const r2 = await request(app).get('/api/v1/openapi.json');

    expect(r2.status).toBe(200);
    expect(r2.headers['content-type']).toMatch(/application\/json/);
    expect(r2.body.info.version).toBe(r1.body.info.version);
    expect(Object.keys(r2.body.paths)).toEqual(Object.keys(r1.body.paths));
  });
});

describe('GET /api/docs', () => {
  it('returns Swagger UI HTML (follows trailing-slash redirect)', async () => {
    // swagger-ui-express issues a 301 redirect from /docs to /docs/
    const res = await request(app).get('/api/docs/');

    expect(res.status).toBe(200);
    expect(res.headers['content-type']).toMatch(/text\/html/);
    expect(res.text).toMatch(/swagger/i);
  });
});
