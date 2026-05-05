import { v4 as uuidv4 } from 'uuid';

const HEADER = 'x-correlation-id';

export function correlationId(req, res, next) {
  const id = req.headers[HEADER] ?? uuidv4();
  req.correlationId = id;
  res.setHeader('X-Correlation-Id', id);
  next();
}
