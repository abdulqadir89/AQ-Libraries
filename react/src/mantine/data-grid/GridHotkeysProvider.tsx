'use client';

import { createContext, useContext } from 'react';
import type { ReactNode } from 'react';
import { resolveGridHotkeys } from './gridHotkeys';
import type { ResolvedGridHotkeys } from './gridHotkeys';
import type { GridHotkeyMap } from './DataGrid.types';

const GridHotkeysContext = createContext<GridHotkeyMap | null>(null);

export interface AQGridHotkeysProviderProps {
  /** App-wide key overrides, merged over the built-in defaults. `false` turns a key off. */
  hotkeys?: GridHotkeyMap;
  children: ReactNode;
}

export function AQGridHotkeysProvider({ hotkeys, children }: AQGridHotkeysProviderProps) {
  return <GridHotkeysContext.Provider value={hotkeys ?? null}>{children}</GridHotkeysContext.Provider>;
}

/**
 * The keys a grid answers to: built-in defaults, then the app-wide provider, then the grid's own `hotkeys` prop.
 * Works with no provider mounted.
 */
export function useGridHotkeys(gridHotkeys?: GridHotkeyMap | false): ResolvedGridHotkeys {
  const appHotkeys = useContext(GridHotkeysContext);
  // RULE RCT-20
  return resolveGridHotkeys(appHotkeys, gridHotkeys);
}
