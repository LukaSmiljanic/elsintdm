function digitsOnly(raw: string) {
  return raw.replace(/[\s\-()./]/g, '');
}

/** Serbian mobile only: 06x… or +3816x…. */
export function isValidRsMobile(raw: string) {
  const p = digitsOnly(raw);
  return /^(\+381|381)6\d{7,8}$/.test(p) || /^06\d{7,8}$/.test(p);
}

/** Mobile or landline (011, 021, 024…) — clients who call sometimes leave a fixed line. */
export function isValidRsPhone(raw: string) {
  if (isValidRsMobile(raw)) return true;
  const p = digitsOnly(raw);
  return /^(\+381|381)[1-3]\d{6,9}$/.test(p) || /^0[1-3]\d{6,9}$/.test(p);
}

export function isValidEmail(raw: string) {
  return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(raw);
}
