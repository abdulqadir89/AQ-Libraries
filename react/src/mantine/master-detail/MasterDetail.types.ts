import type { ReactNode } from 'react';
import type { FilterCondition, LogicalOperator } from '../data-grid';

export interface MasterDetailSortOption {
  value: string; // full sort-expression segment, e.g. "questionNo,asc"
  label: string;
}

export interface MasterDetailFilterConfig {
  component: ReactNode;
  title?: string;
}

export interface MasterDetailProps<T = Record<string, unknown>> {
  data: T[];
  loading?: boolean;
  rowKey: keyof T | ((record: T) => string);

  // Selection — single, persistent (drives the detail pane instead of navigating away)
  selectedKey?: string | null;
  onSelectionChange?: (key: string | null, record: T | undefined) => void;

  // Row + detail rendering — consumer owns both slots entirely
  renderRow: (record: T, selected: boolean, index: number) => ReactNode;
  renderDetail: (record: T | undefined, selectedKey: string | null) => ReactNode;
  emptyListText?: string;

  // Search — same uncontrolled/debounced/callback shape as DataGrid
  searchable?: boolean;
  searchPlaceholder?: string;
  onSearch?: (searchText: string) => void;

  // Sort — flat dropdown (no columns to click); value is the ready-to-send sort expression
  sortOptions?: MasterDetailSortOption[];
  sortValue?: string | null;
  onSortChange?: (sortExpression: string) => void;

  // Filter — reuse DataGrid's FilterCondition/LogicalOperator shape
  filterConfig?: MasterDetailFilterConfig;
  onFilterChange?: (conditions: FilterCondition[], operator?: LogicalOperator) => void;

  // "Load more" pagination — fits a continuously scrolling list panel better than page numbers
  hasMore?: boolean;
  onLoadMore?: () => void;
  loadingMore?: boolean;
  totalCount?: number;

  refreshable?: boolean;
  onRefresh?: () => void;
  toolbarRightSection?: ReactNode;

  // Layout
  listSpan?: number; // Grid.Col span (md+) for the list panel; default 4
  listHeight?: number | string; // default 'calc(100vh - 260px)'
}
