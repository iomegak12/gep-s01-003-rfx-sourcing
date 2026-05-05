import { config } from './config/env.js';
import { printBanner } from './config/banner.js';
import { createApp } from './app.js';
import { runMigrations } from './infrastructure/migrations.js';
import { seedUsers } from './seed/users.seed.js';

const app = createApp();

// Migrations and seed run synchronously/awaited before the server accepts requests.
runMigrations();
const userCount = await seedUsers();

app.listen(config.PORT, config.HOST, () => {
  printBanner({ dbUserCount: userCount });
});
