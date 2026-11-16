'use client';

import { createContext, useContext, useMemo } from 'react';
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
  // An inline object changes identity every render; its content is what matters, so the grids below only update when it changes.
  const stable = useMemo(() => hotkeys ?? null, [JSON.stringify(hotkeys ?? null)]); // eslint-disable-line react-hooks/exhaustive-deps
  return <GridHotkeysContext.Provider value={stable}>{children}</GridHotkeysContext.Provider>;
}

/**
 * The keys a grid answers to: built-in defaults, then the app-wide provider, then the grid's own `hotkeys` prop.
 * Works with no provider mounted.
 */
export function useGridHotkeys(gridHotkeys?: GridHotkeyMap | false): ResolvedGridHotkeys {
  const appHotkeys = useContext(GridHotkeysContext);
  const gridKey = JSON.stringify(gridHotkeys ?? null);
  // RULE RCT-20
  return useMemo(() => resolveGridHotkeys(appHotkeys, gridHotkeys), [appHotkeys, gridKey]); // eslint-disable-line react-hooks/exhaustive-deps
}
