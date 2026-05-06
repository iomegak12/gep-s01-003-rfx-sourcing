import { config } from './config/env.js';
import { printBanner } from './config/banner.js';
import { createApp } from './app.js';
import { runMigrations } from './infrastructure/migrations.js';
import { seedUsers } from './seed/users.seed.js';
import { closeDb } from './infrastructure/db.js';
import { logger } from './infrastructure/logger.js';

const app = createApp();

// Migrations and seed run synchronously/awaited before the server accepts requests.
runMigrations();
const userCount = await seedUsers();

const server = app.listen(config.PORT, config.HOST, () => {
  printBanner({ dbUserCount: userCount });
});

function shutdown(signal) {
  logger.info({ signal }, 'Shutdown signal received — draining connections…');

  server.close((err) => {
    if (err) {
      logger.error({ err }, 'Error closing HTTP server');
      process.exitCode = 1;
    } else {
      logger.info('HTTP server closed');
    }
    closeDb();
    process.exit(process.exitCode ?? 0);
  });

  // Force-exit if graceful drain takes too long (10 s).
  setTimeout(() => {
    logger.warn('Graceful shutdown timed out — forcing exit');
    process.exit(1);
  }, 10_000).unref();
}

process.on('SIGINT', () => shutdown('SIGINT'));
process.on('SIGTERM', () => shutdown('SIGTERM'));
