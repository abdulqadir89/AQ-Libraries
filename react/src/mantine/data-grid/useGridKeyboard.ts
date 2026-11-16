import { useEffect, useRef } from 'react';
import type { RefObject } from 'react';
import type { PaginationConfig } from './DataGrid.types';
import { isTextEntry, matchesHotkey, ownsPageKeys, registerPageKeyHandler } from './gridHotkeys';
import type { ResolvedGridHotkeys } from './gridHotkeys';

interface PageKeysOptions {
  enabled: boolean;
  rootRef: RefObject<HTMLElement | null>;
  searchInputRef: RefObject<HTMLInputElement | null>;
  keys: ResolvedGridHotkeys;
  canCreate: boolean;
  canRefresh: boolean;
  canSearch: boolean;
  canHelp: boolean;
  onCreate?: () => void;
  onRefresh: () => void;
  onHelp: () => void;
}

/**
 * Create, refresh and search answer anywhere on the page, for the table and the card grid alike.
 * Returns the value for the grid root's `data-aq-page-keys`, the keys this grid answers, so a page with
 * several grids hands each key to a grid that has a use for it.
 */
export function usePageKeys({ enabled, rootRef, searchInputRef, keys, canCreate, canRefresh, canSearch, canHelp, onCreate, onRefresh, onHelp }: PageKeysOptions): string {
  const combos = JSON.stringify([canCreate && keys.create, canRefresh && keys.refresh, canSearch && keys.search, canHelp && keys.help].filter(Boolean));

  const handler = useRef<(event: KeyboardEvent) => void>(() => {});
  useEffect(() => {
    handler.current = (event: KeyboardEvent) => {
      if (!enabled || event.defaultPrevented || event.repeat || event.isComposing) return;
      // Whatever keys are held, a shortcut never fires while the user types, so it cannot drop a half-written form.
      if (isTextEntry(event.target)) return;

      // Match the key before asking which grid owns it, so ordinary typing costs nothing.
      const wantsCreate = canCreate && matchesHotkey(event, keys.create);
      const wantsRefresh = !wantsCreate && canRefresh && matchesHotkey(event, keys.refresh);
      const wantsSearch = !wantsCreate && !wantsRefresh && canSearch && !!searchInputRef.current && matchesHotkey(event, keys.search);
      const wantsHelp = !wantsCreate && !wantsRefresh && !wantsSearch && canHelp && matchesHotkey(event, keys.help);
      if (!wantsCreate && !wantsRefresh && !wantsSearch && !wantsHelp) return;
      // RULE RCT-22
      if (!ownsPageKeys(rootRef.current, event)) return;

      event.preventDefault();
      if (wantsCreate) onCreate?.();
      else if (wantsRefresh) onRefresh();
      // RULE RCT-23
      else if (wantsHelp) onHelp();
      else {
        searchInputRef.current?.focus();
        searchInputRef.current?.select();
      }
    };
  });
  useEffect(() => registerPageKeyHandler(handler), []);

  return combos;
}

interface PendingPageFocusOptions<T> {
  data: T[];
  loading: boolean;
  pagination?: PaginationConfig;
  onPageChange?: (page: number, pageSize: number) => void;
  getRowKey: (record: T) => string;
  focusItem: (index: number) => void;
}

/**
 * Turning the page with the keys: `goToPage` asks the consumer for the next or previous page and, once its rows have arrived,
 * focuses the first or last of them. Shared by the table and the card grid.
 */
export function usePendingPageFocus<T>({ data, loading, pagination, onPageChange, getRowKey, focusItem }: PendingPageFocusOptions<T>) {
  const pending = useRef<{ page: number; fromPage: number; where: 'first' | 'last'; fromKeys: string; sawLoading: boolean } | null>(null);

  // Row keys tell the pages apart where the data array's identity cannot (a consumer may rebuild it on every render).
  const rowKeySignature = () => data.map(getRowKey).join('\u0001');

  const goToPage = (delta: number, where: 'first' | 'last'): boolean => {
    if (!pagination || !onPageChange) return false;
    const lastPage = Math.max(1, Math.ceil(pagination.total / pagination.pageSize));
    const page = pagination.current + delta;
    if (page < 1 || page > lastPage) return false;
    pending.current = { page, fromPage: pagination.current, where, fromKeys: rowKeySignature(), sawLoading: false };
    onPageChange(page, pagination.pageSize);
    return true;
  };

  const cancelPendingFocus = () => {
    pending.current = null;
  };

  // The focus waits for the requested page's rows. It is dropped when the grid lands on another page (the change was refused or clamped),
  // and when the user has moved focus elsewhere in the meantime (the pager), so it never steals focus later.
  // The rows have arrived when the grid has gone through loading, or, for a consumer that does not report loading, when the row keys differ.
  useEffect(() => {
    const waiting = pending.current;
    if (!waiting) return;
    if (loading) waiting.sawLoading = true;
    const current = pagination?.current;
    if (current !== waiting.page) {
      if (current !== waiting.fromPage) pending.current = null;
      return;
    }
    if (loading || data.length === 0) return;
    if (!waiting.sawLoading && rowKeySignature() === waiting.fromKeys) return;
    pending.current = null;
    const active = document.activeElement;
    if (active && active !== document.body) return;
    focusItem(waiting.where === 'first' ? 0 : data.length - 1);
  }, [data, loading, pagination?.current]); // eslint-disable-line react-hooks/exhaustive-deps

  return { goToPage, cancelPendingFocus };
}
