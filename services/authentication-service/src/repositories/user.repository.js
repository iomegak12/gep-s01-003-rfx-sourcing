import { db } from '../infrastructure/db.js';

function parseUser(row) {
  if (!row) return null;
  return { ...row, roles: JSON.parse(row.roles) };
}

// Statements are prepared inside each function (not at module-load time) so they
// are only compiled after runMigrations() has created the users table.

export function findByUsername(username) {
  return parseUser(db.prepare('SELECT * FROM users WHERE username = ?').get(username));
}

export function findById(id) {
  return parseUser(db.prepare('SELECT * FROM users WHERE id = ?').get(id));
}

export function updatePasswordHash(id, newHash) {
  db.prepare('UPDATE users SET password_hash = ? WHERE id = ?').run(newHash, id);
}
