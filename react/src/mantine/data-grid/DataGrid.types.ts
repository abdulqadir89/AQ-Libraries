import type { ReactNode } from 'react';

export interface DataGridColumn<T = Record<string, unknown>> {
  key: string;
  title: string;
  dataIndex: keyof T;
  type?: 'string' | 'number' | 'date' | 'boolean' | 'enum';
  width?: number | string;
  minWidth?: number; // Minimum width in pixels (default: 100)
  maxWidth?: number; // Maximum width in pixels
  sortable?: boolean;
  filterable?: boolean;
  render?: (value: unknown, record: T, index: number) => ReactNode;
  align?: 'left' | 'center' | 'right';
  // For enum columns - provide the enum options
  enumOptions?: Array<{ value: string | number; label: string }>;
}

export interface ActionButton<T = Record<string, unknown>> {
  key: string;
  label: string;
  icon?: ReactNode;
  color?: string;
  variant?: 'filled' | 'outline' | 'light' | 'subtle' | 'default';
  onClick: (record: T) => void;
  visible?: (record: T) => boolean;
  disabled?: (record: T) => boolean;
}

// Grid mode configurations
export type GridMode = 'view' | 'action' | 'special';

export interface SpecialModeButtonConfig {
  visible?: boolean;
  disabled?: boolean;
}

export interface SpecialModeConfig {
  overview?: SpecialModeButtonConfig;
  details?: SpecialModeButtonConfig;
  edit?: SpecialModeButtonConfig;
  delete?: SpecialModeButtonConfig;
  create?: SpecialModeButtonConfig;
  search?: { enabled?: boolean };
  filter?: { enabled?: boolean };
}

export interface PaginationConfig {
  current: number;
  pageSize: number;
  total: number;
  showSizeChanger?: boolean;
  pageSizeOptions?: number[];
}

export interface FilterConfig {
  component: ReactNode;
  title?: string;
  width?: number;
}

export interface FormConfig {
  createComponent?: ReactNode;
  editComponent?: ReactNode;
  position?: 'top' | 'bottom' | 'left' | 'right';
  width?: number;
  height?: number;
}

export interface DataGridProps<T = Record<string, unknown>> {
  // Grid identification
  gridId?: string;
  
  // Data and loading
  data: T[];
  loading?: boolean;
  
  // Columns configuration
  columns: DataGridColumn<T>[];
  
  // Grid mode configuration
  mode?: GridMode;
  specialModeConfig?: SpecialModeConfig;
  
  // Actions configuration
  actions?: ActionButton<T>[];
  showActions?: boolean;
  actionsWidth?: number;
  
  // Pagination
  pagination?: PaginationConfig;
  onPageChange?: (page: number, pageSize: number) => void;
  
  // Search
  searchable?: boolean;
  searchPlaceholder?: string;
  onSearch?: (searchText: string) => void;
  
  // Refresh
  refreshable?: boolean;
  onRefresh?: () => void;
  
  // Filter
  filterConfig?: FilterConfig;
  onFilterChange?: (conditions: FilterCondition[], operator?: LogicalOperator) => void;
  
  // Sorting
  sortable?: boolean;
  onSortChange?: (sortExpression: string) => void;
  
  // CRUD Actions - emit events to parent instead of showing dialogs
  onCreate?: () => void;
  createButtonText?: string; // Customizable create button text (default: 'Create')
  createButtonIcon?: ReactNode; // Customizable create button icon (default: IconPlus)
  onEdit?: (record: T) => void;
  onView?: (record: T) => void;
  onDetails?: (record: T) => void; // New separate details action
  
  // Delete confirmation
  onDelete?: (record: T) => void;
  deleteConfirmTitle?: string;
  deleteConfirmContent?: string;
  
  // Table settings
  striped?: boolean;
  highlightOnHover?: boolean;
  withBorder?: boolean;
  withColumnBorders?: boolean;
  
  // Selection
  selectable?: boolean;
  selectedRows?: string[];
  onSelectionChange?: (selectedRowKeys: string[]) => void;
  rowKey?: keyof T | ((record: T) => string);
}

export interface DataGridState {
  searchText: string;
  selectedRows: string[];
  showFilter: boolean;
}

// Filter condition for building filter expressions
export interface FilterCondition {
  property: string;
  operator: FilterOperator;
  value: unknown;
  secondValue?: unknown; // For between operations
}

export type FilterOperator = 
  | 'eq' | 'ne' | 'gt' | 'gte' | 'lt' | 'lte'
  | 'contains' | 'startswith' | 'endswith'
  | 'isnull' | 'isnotnull'
  | 'in' | 'notin'
  | 'between' | 'notbetween';

export type LogicalOperator = 'and' | 'or';

// Filter group for complex filtering
export interface FilterGroup {
  conditions: FilterCondition[];
  groups?: FilterGroup[];
  operator: LogicalOperator;
}

// Sort condition
export interface SortCondition {
  property: string;
  direction: 'asc' | 'desc';
  priority?: number;
}
