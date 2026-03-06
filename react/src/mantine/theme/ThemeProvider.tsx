/**
 * Theme Provider — Shared theme infrastructure for Mantine apps
 * 
 * Accepts pre-loaded theme modules synchronously to avoid SSR streaming issues.
 * Consumers import themes statically and pass them as a record.
 */

import { createContext, useContext, useState, useEffect, useMemo } from 'react';
import type { ReactNode } from 'react';
import { MantineProvider } from '@mantine/core';
import type { MantineColorScheme, MantineThemeOverride, CSSVariablesResolver } from '@mantine/core';
import { ModalsProvider } from '@mantine/modals';

export interface ThemeModule {
  theme: MantineThemeOverride;
  cssVariableResolver: CSSVariablesResolver;
}

interface ThemeContextValue {
  themeName: string;
  setThemeName: (name: string) => void;
  availableThemes: string[];
  colorScheme: MantineColorScheme;
  setColorScheme: (scheme: MantineColorScheme) => void;
  toggleColorScheme: () => void;
}

const ThemeContext = createContext<ThemeContextValue | undefined>(undefined);

export function useTheme() {
  const context = useContext(ThemeContext);
  if (!context) {
    throw new Error('useTheme must be used within ThemeProvider');
  }
  return context;
}

export interface ThemeProviderProps {
  children: ReactNode;
  /** Pre-loaded theme modules keyed by name */
  themes: Record<string, ThemeModule>;
  /** Default theme name (defaults to first key in themes) */
  defaultTheme?: string;
  /** Default color scheme */
  defaultColorScheme?: MantineColorScheme;
  /** localStorage key prefix for persisting preferences */
  storageKeyPrefix?: string;
}

export function ThemeProvider({
  children,
  themes,
  defaultTheme,
  defaultColorScheme = 'light',
  storageKeyPrefix = 'aq-theme',
}: ThemeProviderProps) {
  const availableThemes = useMemo(() => Object.keys(themes), [themes]);
  const resolvedDefaultTheme = defaultTheme || availableThemes[0] || '';

  const [themeName, setThemeNameState] = useState<string>(resolvedDefaultTheme);
  const [colorScheme, setColorSchemeState] = useState<MantineColorScheme>(defaultColorScheme);
  const [mounted, setMounted] = useState(false);

  // Load preferences from localStorage on mount (client only)
  useEffect(() => {
    try {
      const savedTheme = localStorage.getItem(`${storageKeyPrefix}-name`);
      const savedScheme = localStorage.getItem(`${storageKeyPrefix}-color-scheme`) as MantineColorScheme;

      if (savedTheme && availableThemes.includes(savedTheme)) {
        setThemeNameState(savedTheme);
      }

      if (savedScheme) {
        setColorSchemeState(savedScheme);
      }
    } catch {
      // localStorage not available (SSR or restricted)
    }

    setMounted(true);
  }, [availableThemes, storageKeyPrefix]);

  const setThemeName = (name: string) => {
    if (!availableThemes.includes(name)) return;
    setThemeNameState(name);
    try {
      localStorage.setItem(`${storageKeyPrefix}-name`, name);
    } catch {
      // localStorage not available
    }
  };

  const setColorScheme = (scheme: MantineColorScheme) => {
    setColorSchemeState(scheme);
    try {
      localStorage.setItem(`${storageKeyPrefix}-color-scheme`, scheme);
    } catch {
      // localStorage not available
    }
  };

  const toggleColorScheme = () => {
    const newScheme = colorScheme === 'light' ? 'dark' : 'light';
    setColorScheme(newScheme);
  };

  // Get active theme module — always available synchronously (no async loading)
  const activeTheme = themes[themeName] || themes[resolvedDefaultTheme] || Object.values(themes)[0];

  return (
    <ThemeContext.Provider
      value={{
        themeName,
        setThemeName,
        availableThemes,
        colorScheme,
        setColorScheme,
        toggleColorScheme,
      }}
    >
      <MantineProvider
        theme={activeTheme.theme}
        cssVariablesResolver={activeTheme.cssVariableResolver}
        defaultColorScheme={defaultColorScheme}
        forceColorScheme={mounted ? (colorScheme === 'auto' ? undefined : colorScheme) : undefined}
      >
        <ModalsProvider>
          {children}
        </ModalsProvider>
      </MantineProvider>
    </ThemeContext.Provider>
  );
}
