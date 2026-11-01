function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null;
}

function namesRevision(name: unknown): boolean {
  return typeof name === 'string' && name.toLowerCase() === 'revision';
}

/**
 * True when an API error is a stale-save refusal: HTTP 409 whose validation errors name the `revision` field.
 * Reads the shape structurally (`status`, `body.errors`), so it works with any generated client's error class.
 * `errors` may be a list of `{ name, reason }` (FastEndpoints problem details) or an object keyed by field name.
 */
// RULE RCT-17
export function isRevisionConflict(error: unknown): boolean {
  if (!isRecord(error) || error.status !== 409 || !isRecord(error.body)) return false;
  const errors = error.body.errors;
  if (Array.isArray(errors)) {
    return errors.some((entry) => isRecord(entry) && namesRevision(entry.name));
  }
  return isRecord(errors) && Object.keys(errors).some(namesRevision);
}
