import { useSyncExternalStore } from 'react';
import { defaultMessages } from '../locale/defaultMessages';
import type { HotkeyKeyMessages } from '../locale/types';
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
  // TODO-011: '/' is typed with Shift on some layouts (German), so it does not match there.
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

const MODIFIER_NAMES = ['mod', 'ctrl', 'control', 'meta', 'cmd', 'alt', 'option', 'shift'];

// "alt+N", "alt + N", "alt++" (the plus key), "+" and " " (the space key) all parse; a modifier name that is not one of ours is reported once.
function parseHotkey(combo: string): ParsedHotkey {
  const cached = parsedCache.get(combo);
  if (cached) return cached;

  const text = combo.trim().toLowerCase();
  let rawKey: string;
  let modifierText: string;
  if (text === '') {
    rawKey = ' ';
    modifierText = '';
  } else if (text.endsWith('+')) {
    rawKey = '+';
    modifierText = text.slice(0, -1).trim().replace(/\+$/, '');
  } else {
    const parts = text.split('+').map(part => part.trim());
    rawKey = parts.pop() ?? '';
    modifierText = parts.join('+');
  }
  const modifiers = modifierText.split('+').map(part => part.trim()).filter(Boolean);
  const unknown = modifiers.filter(name => !MODIFIER_NAMES.includes(name));
  if (unknown.length > 0) console.warn(`Hotkey "${combo}" has unknown modifier ${unknown.map(name => `"${name}"`).join(', ')} and will not match as written.`);

  const parsed: ParsedHotkey = {
    key: KEY_ALIASES[rawKey] ?? rawKey,
    mod: modifiers.includes('mod'),
    ctrl: modifiers.includes('ctrl') || modifiers.includes('control'),
    meta: modifiers.includes('meta') || modifiers.includes('cmd'),
    alt: modifiers.includes('alt') || modifiers.includes('option'),
    shift: modifiers.includes('shift'),
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

const KEY_NAME_FIELDS: Record<string, keyof HotkeyKeyMessages> = {
  ' ': 'space', enter: 'enter', delete: 'delete', escape: 'escape', tab: 'tab', backspace: 'backspace',
  home: 'home', end: 'end', pageup: 'pageUp', pagedown: 'pageDown',
  arrowup: 'up', arrowdown: 'down', arrowleft: 'left', arrowright: 'right',
};

export function formatHotkey(combo: string, isMac = false, names: HotkeyKeyMessages = defaultMessages.dataGrid.hotkeyKeys): string {
  const p = parseHotkey(combo);
  const field = KEY_NAME_FIELDS[p.key];
  const keyLabel = field ? names[field] : p.key.length === 1 ? p.key.toUpperCase() : p.key.replace(/^./, c => c.toUpperCase());
  const parts: string[] = [];
  if (p.mod) parts.push(isMac ? names.cmd : names.ctrl);
  if (p.ctrl) parts.push(names.ctrl);
  if (p.meta) parts.push(isMac ? names.cmd : names.win);
  if (p.alt) parts.push(isMac ? names.option : names.alt);
  if (p.shift) parts.push(names.shift);
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

// The grid's own controls inside a row (its action buttons and checkboxes), whose arrow keys carry no meaning of their own.
// A button a cell renders itself (a menu trigger) is not one, and keeps its arrow keys.
export function isRowControl(element: EventTarget | null): boolean {
  if (element instanceof HTMLInputElement) return element.type === 'checkbox';
  return element instanceof HTMLElement && element.dataset.actionIndex !== undefined;
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

function openModal(): Element | null {
  const modals = Array.from(document.querySelectorAll('[role="dialog"][aria-modal="true"]')).filter(modal => modal.getClientRects().length > 0);
  return modals[modals.length - 1] ?? null;
}

// The combinations a grid lists in data-aq-page-keys, written as a JSON list so no combination can break the list apart.
function pageKeyCombos(grid: HTMLElement): string[] {
  try {
    const listed: unknown = JSON.parse(grid.dataset.aqPageKeys ?? '[]');
    return Array.isArray(listed) ? listed.filter((combo): combo is string => typeof combo === 'string') : [];
  } catch {
    return [];
  }
}

// Which mounted grid answers a page-wide key: the grid holding focus, else the one last used, else the first visible one.
// Inside a dialog only the grids in that dialog count, so a grid behind it stays quiet and a grid in it works.
export function ownsPageKeys(root: HTMLElement | null, event: KeyboardEvent): boolean {
  if (!root) return false;
  const element = event.target instanceof Element ? event.target : null;
  // With focus on the body or a backdrop an open modal still decides: only grids inside it answer.
  const overlay = element?.closest(PAGE_KEY_OVERLAYS) ?? openModal();
  // Only grids that answer this key (their data-aq-page-keys lists it) are candidates.
  const grids = Array.from(document.querySelectorAll<HTMLElement>('[data-aq-grid]')).filter(grid =>
    grid.getClientRects().length > 0
    && (!overlay || overlay.contains(grid))
    && pageKeyCombos(grid).some(combo => matchesHotkey(event, combo)));
  if (!grids.includes(root)) return false;
  const focusedGrid = element?.closest<HTMLElement>('[data-aq-grid]');
  if (focusedGrid && grids.includes(focusedGrid)) return focusedGrid === root;
  return (lastActiveGrid && grids.includes(lastActiveGrid) ? lastActiveGrid : grids[0]) === root;
}
