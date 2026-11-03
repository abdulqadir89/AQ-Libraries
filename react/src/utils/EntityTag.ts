function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null;
}

/**
 * The If-Match value for a record loaded at `revision`: the revision in double quotes, as the API sends it in its
 * `ETag` header. A record that carries no revision gets `undefined`, so the request is refused rather than sent blind.
 */
// RULE RCT-17
export function formatETag(revision: number | undefined): string | undefined {
  return revision === undefined ? undefined : `"${revision}"`;
}

/**
 * True when an API error is a refused stale save: HTTP 412 Precondition Failed.
 * Reads the shape structurally (`status`), so it works with any generated client's error class.
 */
// RULE RCT-18
export function isPreconditionFailed(error: unknown): boolean {
  return isRecord(error) && error.status === 412;
}
