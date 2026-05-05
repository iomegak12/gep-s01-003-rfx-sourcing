import { db } from './db.js';
import { logger } from './logger.js';

export function runMigrations() {
  db.exec(`
    CREATE TABLE IF NOT EXISTS users (
      id            TEXT PRIMARY KEY,
      username      TEXT UNIQUE NOT NULL,
      password_hash TEXT NOT NULL,
      roles         TEXT NOT NULL,
      created_at    TEXT NOT NULL
    )
  `);
  logger.info('Migrations applied');
}
