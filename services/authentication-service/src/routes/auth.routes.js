import { Router } from 'express';
import { handleLogin, handleRefresh, handleMe } from '../controllers/auth.controller.js';
import { authRequired } from '../middleware/auth-required.js';

const router = Router();

router.post('/login', handleLogin);
router.post('/refresh', handleRefresh);
router.get('/me', authRequired, handleMe);

export default router;
