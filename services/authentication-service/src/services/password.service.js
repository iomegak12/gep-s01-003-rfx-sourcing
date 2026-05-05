import argon2 from 'argon2';
import { config } from '../config/env.js';

function argon2Options() {
  return {
    type: argon2.argon2id,
    memoryCost: config.ARGON2_MEMORY_COST_KIB,
    timeCost: config.ARGON2_TIME_COST,
    parallelism: config.ARGON2_PARALLELISM,
    hashLength: config.ARGON2_HASH_LENGTH,
  };
}

export async function hash(plain) {
  return argon2.hash(plain, argon2Options());
}

export async function verify(phc, plain) {
  return argon2.verify(phc, plain);
}

export function needsRehash(phc) {
  return argon2.needsRehash(phc, argon2Options());
}
