import { db } from '../infrastructure/db.js';
import { hash } from '../services/password.service.js';
import { config } from '../config/env.js';
import { logger } from '../infrastructure/logger.js';

// Stable UUIDs so that JWT `sub` claims are predictable across restarts.
const SEED_USERS = [
  {
    id: '8f3b2c1a-1d4e-4a5b-9c7d-2e6f8a0b1c2d',
    username: 'buyer.user',
    password: 'Buyer@123',
    roles: ['Buyer'],
  },
  {
    id: '7e2a1b09-0c3d-3a4b-8b6c-1d5e7a9b0b1e',
    username: 'supplier.user',
    password: 'Supplier@123',
    roles: ['Supplier'],
  },
  {
    id: '6d190a08-fb2c-2a3b-7a5b-0c4d6987a0d0',
    username: 'admin.user',
    password: 'Admin@123',
    roles: ['Admin', 'All'],
  },
];

export async function seedUsers() {
  if (!config.SEED_DATA_ENABLED) {
    logger.info('Seed skipped — SEED_DATA_ENABLED=false');
    return db.prepare('SELECT COUNT(*) as count FROM users').get().count;
  }

  const { count } = db.prepare('SELECT COUNT(*) as count FROM users').get();

  if (count > 0) {
    logger.info({ count }, 'Seed skipped — table not empty');
    return count;
  }

  logger.info('Seeding users…');

  const insert = db.prepare(
    'INSERT INTO users (id, username, password_hash, roles, created_at) VALUES (@id, @username, @password_hash, @roles, @created_at)'
  );

  for (const user of SEED_USERS) {
    const passwordHash = await hash(user.password);
    insert.run({
      id: user.id,
      username: user.username,
      password_hash: passwordHash,
      roles: JSON.stringify(user.roles),
      created_at: new Date().toISOString(),
    });
    logger.info({ username: user.username, roles: user.roles }, 'User seeded');
  }

  logger.info({ count: SEED_USERS.length }, 'Seed complete');
  return SEED_USERS.length;
}
