import { Router } from 'express';
import {
  swaggerUiMiddleware,
  swaggerUiSetup,
  serveOpenApiJson,
} from '../controllers/docs.controller.js';

const router = Router();

router.get('/openapi.json', serveOpenApiJson);
router.use('/docs', swaggerUiMiddleware, swaggerUiSetup);

export default router;
