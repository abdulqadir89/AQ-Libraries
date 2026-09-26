/**
 * Plain recursive object merge. The right side wins; arrays and functions are
 * replaced wholesale, not merged. Used to overlay partial `AQMessages` overrides
 * on top of `defaultMessages`.
 */
export function deepMerge<T>(base: T, override: unknown): T {
  if (override === undefined || override === null) {
    return base;
  }

  if (
    typeof base !== 'object' ||
    base === null ||
    Array.isArray(base) ||
    typeof base === 'function' ||
    typeof override !== 'object' ||
    Array.isArray(override)
  ) {
    return override as T;
  }

  const result: Record<string, unknown> = { ...(base as Record<string, unknown>) };
  for (const key of Object.keys(override as Record<string, unknown>)) {
    const overrideValue = (override as Record<string, unknown>)[key];
    const baseValue = (base as Record<string, unknown>)[key];
    result[key] = deepMerge(baseValue, overrideValue);
  }
  return result as T;
}
