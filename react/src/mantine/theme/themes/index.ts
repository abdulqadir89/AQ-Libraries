import type { ThemeModule } from '../ThemeProvider';
import { shadcnTheme as zincTheme } from './zinc/theme';
import { shadcnCssVariableResolver as zincCssVariableResolver } from './zinc/cssVariableResolver';
import { shadcnTheme as blueTheme } from './blue/theme';
import { shadcnCssVariableResolver as blueCssVariableResolver } from './blue/cssVariableResolver';
import { shadcnTheme as clayTheme } from './clay/theme';
import { shadcnCssVariableResolver as clayCssVariableResolver } from './clay/cssVariableResolver';

/**
 * Default theme modules available in the shared library.
 *
 * Pre-loaded synchronously — no dynamic imports, SSR-safe.
 * Consuming apps pass this (or a merged record with custom themes) to ThemeProvider.
 */
export const defaultThemeModules: Record<string, ThemeModule> = {
  zinc: { theme: zincTheme, cssVariableResolver: zincCssVariableResolver },
  blue: { theme: blueTheme, cssVariableResolver: blueCssVariableResolver },
  clay: { theme: clayTheme, cssVariableResolver: clayCssVariableResolver },
};

/** List of default theme names */
export const defaultThemes = Object.keys(defaultThemeModules);

export type DefaultThemeName = 'zinc' | 'blue' | 'clay';

/**
 * Get theme display name for UI (capitalizes first letter).
 */
export function getThemeDisplayName(themeName: string): string {
  return themeName.charAt(0).toUpperCase() + themeName.slice(1);
}

// Re-export individual themes for selective imports
export { zincTheme, zincCssVariableResolver, blueTheme, blueCssVariableResolver, clayTheme, clayCssVariableResolver };
