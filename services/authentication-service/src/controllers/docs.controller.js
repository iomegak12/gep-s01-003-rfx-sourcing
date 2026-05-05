import { readFileSync } from 'fs';
import { join, dirname } from 'path';
import { fileURLToPath } from 'url';
import yaml from 'js-yaml';
import swaggerUi from 'swagger-ui-express';

const __dirname = dirname(fileURLToPath(import.meta.url));
const specPath = join(__dirname, '../openapi/openapi.yaml');

export const openApiSpec = yaml.load(readFileSync(specPath, 'utf8'));

export const swaggerUiMiddleware = swaggerUi.serve;
export const swaggerUiSetup = swaggerUi.setup(openApiSpec, {
  customSiteTitle: 'RFx Auth Service — API Docs',
});

export function serveOpenApiJson(_req, res) {
  res.json(openApiSpec);
}
