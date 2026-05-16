import type { ReactNode } from 'react';

export interface DataGridColumn<T = Record<string, unknown>> {
  key: string;
  title: string;
  dataIndex: keyof T;
  cardRole?: 'title' | 'details';
  type?: 'string' | 'number' | 'date' | 'boolean' | 'enum' | 'markdown';
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
  onClick?: (record: T) => void;
  href?: string | ((record: T) => string);
  visible?: (record: T) => boolean;
  disabled?: (record: T) => boolean;
}

export interface BulkAction {
  key: string;
  label: string;
  icon?: ReactNode;
  color?: string;
  onClick: (selectedKeys: string[]) => void;
  confirm?: {
    title: string;
    content: string;
  };
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
  toolbarRightSection?: ReactNode;
  
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
  editHref?: (record: T) => string;
  onView?: (record: T) => void;
  viewHref?: (record: T) => string;
  onDetails?: (record: T) => void; // New separate details action
  detailsHref?: (record: T) => string;
  
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

  // Bulk actions (shown in toolbar when records are selected)
  bulkActions?: BulkAction[];
}

export type GridViewMode = 'table' | 'card';

export interface DataGridToolbarConfig {
  showSearch?: boolean;
  showCreate?: boolean;
  showRefresh?: boolean;
  showOptions?: boolean;
}

export interface FilterPreset {
  key: string;
  label: string;
  conditions: FilterCondition[];
  operator?: LogicalOperator;
}

export interface SortPreset {
  key: string;
  label: string;
  conditions: SortCondition[];
}

export interface CardImageConfig<T = Record<string, unknown>> {
  dataIndex?: keyof T;
  height?: number;
  fit?: 'cover' | 'contain' | 'fill' | 'none' | 'scale-down';
  alt?: string;
  render?: (record: T, index: number) => ReactNode;
}

export interface CardLayoutConfig {
  base?: number;
  xs?: number;
  sm?: number;
  md?: number;
  lg?: number;
  xl?: number;
}

export interface CardDataGridProps<T = Record<string, unknown>> {
  gridId?: string;
  data: T[];
  loading?: boolean;
  columns: DataGridColumn<T>[];
  pagination?: PaginationConfig;
  onPageChange?: (page: number, pageSize: number) => void;
  searchable?: boolean;
  searchPlaceholder?: string;
  onSearch?: (searchText: string) => void;
  toolbarRightSection?: ReactNode;
  refreshable?: boolean;
  onRefresh?: () => void;
  sortable?: boolean;
  onSortChange?: (sortExpression: string) => void;
  onCreate?: () => void;
  createButtonText?: string;
  createButtonIcon?: ReactNode;
  selectable?: boolean;
  selectedRows?: string[];
  onSelectionChange?: (selectedRowKeys: string[]) => void;
  rowKey?: keyof T | ((record: T) => string);
  onFilterChange?: (conditions: FilterCondition[], operator?: LogicalOperator) => void;
  filterPresets?: FilterPreset[];
  sortPresets?: SortPreset[];
  initialFilterConditions?: FilterCondition[];
  initialFilterOperator?: LogicalOperator;
  initialSortConditions?: SortCondition[];
  onFilterExpressionChange?: (filterExpression: string) => void;
  toolbarConfig?: DataGridToolbarConfig;
  cardLayout?: CardLayoutConfig;
  cardImage?: CardImageConfig<T>;
  cardTitle?: (record: T, index: number) => ReactNode;
  cardSubtitle?: (record: T, index: number) => ReactNode;
  renderCard?: (record: T, index: number) => ReactNode;
  emptyStateText?: string;

  // Card-specific interaction
  onCardClick?: (record: T) => void;
  cardHref?: (record: T) => string;

  // Bulk actions (shown in toolbar when records are selected)
  bulkActions?: BulkAction[];
}

export interface DataGridSwitchProps {
  value: GridViewMode;
  onChange: (value: GridViewMode) => void;
  tableLabel?: string;
  cardLabel?: string;
}

export interface DataGridViewSwitcherProps<T = Record<string, unknown>> {
  viewMode: GridViewMode;
  onViewModeChange: (value: GridViewMode) => void;
  showSwitch?: boolean;
  switchTableLabel?: string;
  switchCardLabel?: string;
  tableProps: DataGridProps<T>;
  cardProps?: Partial<CardDataGridProps<T>>;
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
