import { Router } from 'express';
import healthRoutes from './health.routes.js';
import authRoutes from './auth.routes.js';
import { serveOpenApiJson } from '../controllers/docs.controller.js';

const router = Router();

router.get('/openapi.json', serveOpenApiJson);
router.use('/health', healthRoutes);
router.use('/auth', authRoutes);

export default router;
