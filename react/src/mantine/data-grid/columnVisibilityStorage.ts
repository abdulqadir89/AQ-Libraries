// Persists per-gridId column visibility overrides the user has made via the
// column show/hide menu. Only overrides are stored (not the full resolved
// state), so a later change to a column's `defaultHidden` in code still
// applies for columns the user never touched.
export class ColumnVisibilityStorage {
  private static readonly STORAGE_KEY = 'dqm-grid-columns';

  static get(gridId: string): Record<string, boolean> | null {
    try {
      const stored = localStorage.getItem(this.STORAGE_KEY);
      if (!stored) return null;
      const settings = JSON.parse(stored);
      return settings[gridId] ?? null;
    } catch (error) {
      console.warn('Failed to read column visibility from localStorage:', error);
      return null;
    }
  }

  static set(gridId: string, overrides: Record<string, boolean>): void {
    try {
      const stored = localStorage.getItem(this.STORAGE_KEY);
      const settings = stored ? JSON.parse(stored) : {};
      settings[gridId] = overrides;
      localStorage.setItem(this.STORAGE_KEY, JSON.stringify(settings));
    } catch (error) {
      console.warn('Failed to save column visibility to localStorage:', error);
    }
  }
}
