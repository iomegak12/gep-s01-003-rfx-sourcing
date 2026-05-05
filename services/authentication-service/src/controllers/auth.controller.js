import { loginSchema, refreshSchema } from '../validation/auth.schemas.js';
import { login, refresh, me } from '../services/auth.service.js';
import { ValidationError } from '../errors/domain-errors.js';

function parseBody(schema, body) {
  const result = schema.safeParse(body);
  if (!result.success) {
    const messages = result.error.errors.map((e) => e.message).join('; ');
    throw new ValidationError(messages);
  }
  return result.data;
}

export async function handleLogin(req, res, next) {
  try {
    const data = parseBody(loginSchema, req.body);
    const tokens = await login(data);
    res.status(200).json(tokens);
  } catch (err) {
    next(err);
  }
}

export async function handleRefresh(req, res, next) {
  try {
    const data = parseBody(refreshSchema, req.body);
    const result = await refresh(data);
    res.status(200).json(result);
  } catch (err) {
    next(err);
  }
}

export function handleMe(req, res, next) {
  try {
    const profile = me(req.user);
    res.status(200).json(profile);
  } catch (err) {
    next(err);
  }
}
