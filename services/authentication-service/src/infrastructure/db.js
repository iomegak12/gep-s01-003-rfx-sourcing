import Database from 'better-sqlite3';
import { mkdirSync } from 'fs';
import { dirname } from 'path';
import { config } from '../config/env.js';
import { logger } from './logger.js';

function open() {
  const path = config.DATABASE_PATH;
  // mkdirSync with recursive:true is a no-op if the directory already exists.
  mkdirSync(dirname(path), { recursive: true });
  const db = new Database(path);
  db.pragma('journal_mode = WAL');
  db.pragma('foreign_keys = ON');
  logger.info({ path }, 'Database opened');
  return db;
}

export const db = open();

export function closeDb() {
  db.close();
  logger.info('Database closed');
}
