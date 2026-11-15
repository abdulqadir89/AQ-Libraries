import { useSyncExternalStore } from 'react';
import type { GridHotkeyMap, GridHotkeyName } from './DataGrid.types';

export type ResolvedGridHotkeys = Record<GridHotkeyName, string | false>;

export const defaultGridHotkeys: ResolvedGridHotkeys = {
  prevRow: 'ArrowUp',
  nextRow: 'ArrowDown',
  firstRow: 'Home',
  lastRow: 'End',
  prevPage: 'PageUp',
  nextPage: 'PageDown',
  open: 'Enter',
  select: 'Space',
  selectAll: 'mod+A',
  overview: 'V',
  details: 'D',
  edit: 'E',
  delete: 'Delete',
  filter: 'F',
  create: 'alt+N',
  refresh: 'alt+R',
  search: '/',
};

export function resolveGridHotkeys(...layers: Array<GridHotkeyMap | false | null | undefined>): ResolvedGridHotkeys {
  const resolved = { ...defaultGridHotkeys };
  for (const layer of layers) {
    if (layer === false) {
      (Object.keys(resolved) as GridHotkeyName[]).forEach(name => { resolved[name] = false; });
      continue;
    }
    if (!layer) continue;
    for (const name of Object.keys(layer) as GridHotkeyName[]) {
      const value = layer[name];
      if (value !== undefined) resolved[name] = value;
    }
  }
  return resolved;
}

export interface HotkeyEventLike {
  key: string;
  code: string;
  ctrlKey: boolean;
  metaKey: boolean;
  altKey: boolean;
  shiftKey: boolean;
}

interface ParsedHotkey {
  key: string;
  mod: boolean;
  ctrl: boolean;
  meta: boolean;
  alt: boolean;
  shift: boolean;
}

const KEY_ALIASES: Record<string, string> = { space: ' ', esc: 'escape', del: 'delete', up: 'arrowup', down: 'arrowdown', left: 'arrowleft', right: 'arrowright' };

const SHIFTED_CHARACTERS = '?!@#$%^&*()_+{}|:"<>~';

const parsedCache = new Map<string, ParsedHotkey>();

function parseHotkey(combo: string): ParsedHotkey {
  const cached = parsedCache.get(combo);
  if (cached) return cached;

  const parts = combo.trim().toLowerCase().split('+').map(part => part.trim());
  const rawKey = parts.pop() ?? '';
  const parsed: ParsedHotkey = {
    key: KEY_ALIASES[rawKey] ?? rawKey,
    mod: parts.includes('mod'),
    ctrl: parts.includes('ctrl') || parts.includes('control'),
    meta: parts.includes('meta') || parts.includes('cmd'),
    alt: parts.includes('alt') || parts.includes('option'),
    shift: parts.includes('shift'),
  };
  parsedCache.set(combo, parsed);
  return parsed;
}

export function normalizeHotkey(combo: string): string {
  const p = parseHotkey(combo);
  return [p.mod && 'mod', p.ctrl && 'ctrl', p.meta && 'meta', p.alt && 'alt', p.shift && 'shift', p.key].filter(Boolean).join('+');
}

export function matchesHotkey(event: HotkeyEventLike, combo: string | false | undefined): boolean {
  if (!combo) return false;
  const p = parseHotkey(combo);
  if (!p.key) return false;

  // mod is Cmd on a Mac and Ctrl elsewhere, so Ctrl+A stays the line-start key on a Mac and Win+A stays the OS's.
  const modDown = detectMac() ? event.metaKey && !event.ctrlKey : event.ctrlKey && !event.metaKey;
  const modifiersMatch = p.mod
    ? modDown && event.altKey === p.alt
    : event.ctrlKey === p.ctrl && event.metaKey === p.meta && event.altKey === p.alt;
  if (!modifiersMatch) return false;

  // A character that is itself typed with Shift (?, :) cannot be told apart by the Shift state; every other key must match it exactly, so '/' is not Shift+7 on a German layout.
  const typedWithShift = p.key.length === 1 && SHIFTED_CHARACTERS.includes(p.key);
  if (!typedWithShift && event.shiftKey !== p.shift) return false;

  // A letter is matched by the character the layout types (so Alt+N is the key labelled N on Dvorak and AZERTY too). When Alt rewrites that character
  // (Option+N types a tilde on a Mac) or the layout has no Latin letters, the physical key stands in.
  if (/^[a-z]$/.test(p.key)) {
    const typed = event.key.toLowerCase();
    if (typed === p.key) return true;
    if (/^[a-z]$/.test(typed)) return false;
    return event.code === `Key${p.key.toUpperCase()}`;
  }
  return event.key.toLowerCase() === p.key;
}

export function formatHotkey(combo: string, isMac = false): string {
  const p = parseHotkey(combo);
  const keyLabel = p.key === ' ' ? 'Space' : p.key.length === 1 ? p.key.toUpperCase() : p.key.replace(/^arrow/, '').replace(/^./, c => c.toUpperCase());
  const parts: string[] = [];
  if (p.mod) parts.push(isMac ? 'Cmd' : 'Ctrl');
  if (p.ctrl) parts.push('Ctrl');
  if (p.meta) parts.push(isMac ? 'Cmd' : 'Win');
  if (p.alt) parts.push(isMac ? 'Option' : 'Alt');
  if (p.shift) parts.push('Shift');
  parts.push(keyLabel);
  return parts.join('+');
}

export function findDuplicateHotkeys(entries: Array<[label: string, combo: string | false | undefined]>): string[] {
  const seen = new Map<string, string>();
  const duplicates: string[] = [];
  for (const [label, combo] of entries) {
    if (!combo) continue;
    const normalized = normalizeHotkey(combo);
    const first = seen.get(normalized);
    if (first) duplicates.push(`${first} and ${label} share "${combo}"`);
    else seen.set(normalized, label);
  }
  return duplicates;
}

export function isTextEntry(element: EventTarget | null): boolean {
  if (!(element instanceof HTMLElement)) return false;
  if (element.isContentEditable) return true;
  if (element instanceof HTMLTextAreaElement || element instanceof HTMLSelectElement) return true;
  if (element instanceof HTMLInputElement) return !['checkbox', 'radio', 'button', 'submit'].includes(element.type);
  return false;
}

const noopSubscribe = () => () => {};
const detectMac = (): boolean => {
  if (typeof navigator === 'undefined') return false;
  const platform = (navigator as Navigator & { userAgentData?: { platform?: string } }).userAgentData?.platform ?? navigator.platform ?? '';
  return /^(Mac|iPhone|iPad)/.test(platform);
};

// False while hydrating on the server's HTML, then the real platform, so the first client render needs no extra pass.
export function useIsMac(): boolean {
  return useSyncExternalStore(noopSubscribe, detectMac, () => false);
}

const PAGE_KEY_OVERLAYS = '[role="dialog"], [role="menu"], [role="listbox"]';
let lastActiveGrid: HTMLElement | null = null;

export function markGridActive(root: HTMLElement | null) {
  lastActiveGrid = root;
}

// Which mounted grid answers a page-wide key: the grid holding focus, else the one last used, else the first visible one.
// Inside a dialog only the grids in that dialog count, so a grid behind it stays quiet and a grid in it works.
export function ownsPageKeys(root: HTMLElement | null, event: KeyboardEvent): boolean {
  if (!root) return false;
  const element = event.target instanceof Element ? event.target : null;
  const overlay = element?.closest(PAGE_KEY_OVERLAYS) ?? null;
  // Only grids that answer this key (their data-aq-page-keys lists it) are candidates.
  const grids = Array.from(document.querySelectorAll<HTMLElement>('[data-aq-grid]')).filter(grid =>
    grid.getClientRects().length > 0
    && (!overlay || overlay.contains(grid))
    && (grid.dataset.aqPageKeys ?? '').split(' ').some(combo => matchesHotkey(event, combo)));
  if (!grids.includes(root)) return false;
  const focusedGrid = element?.closest<HTMLElement>('[data-aq-grid]');
  if (focusedGrid && grids.includes(focusedGrid)) return focusedGrid === root;
  return (lastActiveGrid && grids.includes(lastActiveGrid) ? lastActiveGrid : grids[0]) === root;
}
