import { AppError } from './app-error.js';

export class ValidationError extends AppError {
  constructor(detail) {
    super('Validation error', 400, detail, 'validation-error');
  }
}

export class InvalidCredentialsError extends AppError {
  constructor() {
    super(
      'Invalid credentials',
      401,
      'Username or password is incorrect.',
      'invalid-credentials'
    );
  }
}

export class InvalidAccessTokenError extends AppError {
  constructor(detail = 'The supplied access token is invalid, malformed, or has expired.') {
    super('Invalid access token', 401, detail, 'invalid-access-token');
  }
}

export class InvalidRefreshTokenError extends AppError {
  constructor(detail = 'The supplied refresh token is invalid, malformed, or has expired.') {
    super('Invalid or expired refresh token', 401, detail, 'invalid-refresh-token');
  }
}

export class InternalError extends AppError {
  constructor(detail = 'An unexpected error occurred.') {
    super('Internal server error', 500, detail, 'internal-error');
  }
}
