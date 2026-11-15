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

  const modifiersMatch = p.mod
    ? (event.ctrlKey || event.metaKey) && event.altKey === p.alt
    : event.ctrlKey === p.ctrl && event.metaKey === p.meta && event.altKey === p.alt;
  if (!modifiersMatch) return false;

  const isPunctuation = p.key.length === 1 && !/[a-z0-9]/.test(p.key);
  if (!isPunctuation && event.shiftKey !== p.shift) return false;

  // Alt rewrites the typed character on some layouts (Option+N gives a tilde on a Mac), so a letter is matched by its physical key.
  if (event.altKey && /^[a-z]$/.test(p.key)) return event.code === `Key${p.key.toUpperCase()}`;
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
