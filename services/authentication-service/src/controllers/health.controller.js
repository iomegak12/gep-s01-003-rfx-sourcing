import { APP_NAME } from '../config/constants.js';

export function healthCheck(_req, res) {
  res.json({
    status: 'UP',
    service: APP_NAME,
    timestamp: new Date().toISOString(),
  });
}
