import { describe, it, expect } from 'vitest';
import { hash, verify, needsRehash } from '../../src/services/password.service.js';

describe('password.service', () => {
  const PLAIN = 'Buyer@123';

  describe('hash', () => {
    it('returns a PHC string starting with $argon2id$', async () => {
      const phc = await hash(PLAIN);
      expect(phc).toMatch(/^\$argon2id\$/);
    });

    it('produces a different hash on each call (random salt)', async () => {
      const a = await hash(PLAIN);
      const b = await hash(PLAIN);
      expect(a).not.toBe(b);
    });
  });

  describe('verify', () => {
    it('returns true for the correct password', async () => {
      const phc = await hash(PLAIN);
      await expect(verify(phc, PLAIN)).resolves.toBe(true);
    });

    it('returns false for a wrong password', async () => {
      const phc = await hash(PLAIN);
      await expect(verify(phc, 'WrongPass!')).resolves.toBe(false);
    });
  });

  describe('needsRehash', () => {
    it('returns false for a freshly-generated hash (same params)', async () => {
      const phc = await hash(PLAIN);
      expect(needsRehash(phc)).toBe(false);
    });
  });
});
