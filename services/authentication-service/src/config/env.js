import 'dotenv/config';
import { z } from 'zod';

const boolStr = (defaultVal) =>
  z
    .string()
    .default(defaultVal)
    .transform((v) => v === 'true');

const schema = z.object({
  APP_NAME: z.string().default('auth-service'),
  APP_DESCRIPTION: z
    .string()
    .default('RFx Sourcing — Authentication Service (issues HS256 JWTs)'),
  NODE_ENV: z.enum(['development', 'production', 'test']).default('development'),
  HOST: z.string().default('0.0.0.0'),
  PORT: z.coerce.number().int().positive().default(5003),
  LOG_LEVEL: z
    .enum(['fatal', 'error', 'warn', 'info', 'debug', 'trace'])
    .default('info'),

  JWT_SIGNING_KEY: z.string().min(32, 'JWT_SIGNING_KEY must be at least 32 characters'),
  JWT_ISSUER: z.string().default('rfx-auth-service'),
  JWT_AUDIENCE: z.string().default('rfx-sourcing-system'),
  JWT_ACCESS_TOKEN_LIFETIME_SECONDS: z.coerce.number().int().positive().default(900),
  JWT_REFRESH_TOKEN_LIFETIME_SECONDS: z.coerce.number().int().positive().default(86400),

  DATABASE_PATH: z.string().default('./data/auth.db'),
  SEED_DATA_ENABLED: z
    .string()
    .default('true')
    .transform((v) => v !== 'false'),

  ARGON2_MEMORY_COST_KIB: z.coerce.number().int().min(19456).default(65536),
  ARGON2_TIME_COST: z.coerce.number().int().min(1).default(2),
  ARGON2_PARALLELISM: z.coerce.number().int().min(1).default(1),
  ARGON2_HASH_LENGTH: z.coerce.number().int().min(16).default(32),

  RATE_LIMIT_ENABLED: boolStr('false'),
  RATE_LIMIT_WINDOW_MS: z.coerce.number().int().positive().default(60000),
  RATE_LIMIT_MAX: z.coerce.number().int().positive().default(100),
});

const result = schema.safeParse(process.env);

if (!result.success) {
  console.error('Invalid environment configuration:');
  const errors = result.error.flatten().fieldErrors;
  for (const [field, messages] of Object.entries(errors)) {
    console.error(`  ${field}: ${messages.join(', ')}`);
  }
  process.exit(1);
}

export const config = result.data;
