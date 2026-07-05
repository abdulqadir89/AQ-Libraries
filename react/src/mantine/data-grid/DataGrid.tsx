import { useState, useCallback, useEffect, useRef } from 'react';
import styles from './DataGrid.module.css';
import type { ChangeEvent, MouseEvent as ReactMouseEvent } from 'react';
import {
  Table,
  ScrollArea,
  Group,
  TextInput,
  ActionIcon,
  Button,
  Menu,
  Pagination,
  Select,
  Text,
  Checkbox,
  Flex,
  Box,
  Paper,
  LoadingOverlay,
  UnstyledButton,
} from '@mantine/core';
import { useDebouncedValue, useResizeObserver } from '@mantine/hooks';
import { modals } from '@mantine/modals';
import { ColumnFilter } from './ColumnFilter';
import type { ColumnFilterRef } from './ColumnFilter';
import {
  IconSearch,
  IconRefresh,
  IconPlus,
  IconEdit,
  IconTrash,
  IconEye,
  IconChevronDown,
  IconChevronUp,
  IconSelector,
  IconListDetails,
  IconFilterOff,
  IconStack2,
  IconX,
} from '@tabler/icons-react';
import type {
  BulkAction,
  DataGridProps,
  DataGridState,
  DataGridColumn,
  ActionButton,
  SortCondition,
  FilterCondition,
  SpecialModeConfig,
} from './DataGrid.types';

interface ExtendedDataGridState extends DataGridState {
  columnFilters: Record<string, string>;
}

export function DataGrid<T extends Record<string, unknown>>({
  data = [],
  loading = false,
  columns = [],
  mode = 'action',
  specialModeConfig,
  actions,
  showActions = true,
  actionsWidth,
  pagination,
  onPageChange,
  searchable = true,
  searchPlaceholder = 'Search...',
  onSearch,
  toolbarRightSection,
  refreshable = true,
  onRefresh,
  sortable = true,
  onSortChange,
  onCreate,
  createButtonText = 'Create',
  createButtonIcon,
  onEdit,
  editHref,
  onView,
  viewHref,
  onDetails,
  detailsHref,
  onDelete,
  deleteConfirmTitle = 'Confirm Delete',
  deleteConfirmContent = 'Are you sure you want to delete this item? This action cannot be undone.',
  striped = true,
  highlightOnHover = true,
  withBorder = true,
  withColumnBorders = false,
  selectable = false,
  selectedRows = [],
  onSelectionChange,
  rowKey = 'id',
  onFilterChange,
  bulkActions,
  actionButtonStyle = 'icon',
}: DataGridProps<T>) {
  const [state, setState] = useState<ExtendedDataGridState>({
    searchText: '',
    selectedRows: selectedRows || [],
    showFilter: false,
    columnFilters: {},
  });

  // Column filter refs for reset functionality
  const columnFilterRefs = useRef<Record<string, ColumnFilterRef | null>>({});

  // Sync selectedRows prop with internal state
  useEffect(() => {
    setState(prev => {
      const newSelection = selectedRows || [];
      if (JSON.stringify(prev.selectedRows) !== JSON.stringify(newSelection)) {
        return { ...prev, selectedRows: newSelection };
      }
      return prev;
    });
  }, [selectedRows]);

  // Determine button visibility based on mode
  const getButtonConfig = (buttonKey: string) => {
    switch (mode) {
      case 'view':
        // In view mode: only overview and details visible
        return {
          overview: { visible: true, disabled: false },
          details: { visible: true, disabled: false },
          edit: { visible: false, disabled: false },
          delete: { visible: false, disabled: false },
          create: { visible: false, disabled: false },
        }[buttonKey] || { visible: false, disabled: false };

      case 'action':
        // In action mode: all buttons visible
        return { visible: true, disabled: false };

      case 'special': {
        // In special mode: use specialModeConfig
        const config = specialModeConfig?.[buttonKey as keyof SpecialModeConfig];
        if (config && 'visible' in config) {
          return {
            visible: config.visible !== false,
            disabled: config.disabled || false
          };
        }
        return { visible: true, disabled: false };
      }

      default:
        return { visible: true, disabled: false };
    }
  };

  // Default action buttons with mode-aware visibility
  const defaultActions: ActionButton<T>[] = [
    {
      key: 'overview',
      label: 'Overview',
      icon: <IconEye size={18} />,
      color: 'blue',
      variant: 'light',
      onClick: onView ? (record) => onView(record) : undefined,
      href: viewHref,
      visible: () => {
        const config = getButtonConfig('overview');
        return config.visible && (!!onView || !!viewHref);
      },
      disabled: () => getButtonConfig('overview').disabled,
    },
    {
      key: 'details',
      label: 'Details',
      icon: <IconListDetails size={18} />,
      color: 'cyan',
      variant: 'light',
      onClick: onDetails ? (record) => onDetails(record) : undefined,
      href: detailsHref,
      visible: () => {
        const config = getButtonConfig('details');
        return config.visible && (!!onDetails || !!detailsHref);
      },
      disabled: () => getButtonConfig('details').disabled,
    },
    {
      key: 'edit',
      label: 'Edit',
      icon: <IconEdit size={18} />,
      color: 'orange',
      variant: 'light',
      onClick: onEdit ? (record) => onEdit(record) : undefined,
      href: editHref,
      visible: () => {
        const config = getButtonConfig('edit');
        return config.visible && (!!onEdit || !!editHref);
      },
      disabled: () => getButtonConfig('edit').disabled,
    },
    {
      key: 'delete',
      label: 'Delete',
      icon: <IconTrash size={18} />,
      color: 'red',
      variant: 'light',
      onClick: (record) => {
        modals.openConfirmModal({
          title: deleteConfirmTitle,
          children: <Text size="sm">{deleteConfirmContent}</Text>,
          labels: { confirm: 'Delete', cancel: 'Cancel' },
          confirmProps: { color: 'red' },
          onConfirm: () => onDelete?.(record),
        });
      },
      visible: () => {
        const config = getButtonConfig('delete');
        return config.visible && !!onDelete;
      },
      disabled: () => getButtonConfig('delete').disabled,
    },
  ];

  const finalActions = actions ? [...defaultActions, ...actions] : defaultActions;

  // Static estimate of visible action buttons for sizing the actions column — evaluated
  // without a record, so per-row `visible()` overrides (rare, row-data-dependent) are
  // assumed visible here; this matches the precision the previous flat default had.
  const visibleActionCount = finalActions.filter(action => {
    try {
      return !action.visible || action.visible({} as T);
    } catch {
      return true;
    }
  }).length;

  // Icon ActionIcon (size="md") is 36px, Group gap="xs" is 10px, plus ~24px of cell
  // padding — computed so grids with fewer/more buttons than the old flat 120px default
  // don't render an actions column with empty space or cramped/wrapping buttons.
  // Width is computed for at least 2 icons' worth of space even when only 1 action is
  // visible, since the "Actions" header label itself needs that much room — a single
  // icon's width alone clips/wraps the header text (e.g. a lone Delete-only grid).
  const actionsWidthColumnCount = Math.max(visibleActionCount, 2);
  const computedActionsWidth = visibleActionCount > 0
    ? actionsWidthColumnCount * 36 + (actionsWidthColumnCount - 1) * 10 + 24
    : 0;
  const resolvedActionsWidth = actionsWidth ?? computedActionsWidth;

  // Sorting state
  const [sortConditions, setSortConditions] = useState<SortCondition[]>([]);

  // Column widths state (for resizing)
  const [columnWidths, setColumnWidths] = useState<Record<string, number>>({});

  // Columns the user has manually dragged to a specific width — once resized, a column
  // is never recalculated again (stays exactly where the user left it).
  const manuallyResizedKeys = useRef<Set<string>>(new Set());

  // Measures the scrollable table container so the primary column can fill/fit it.
  const [scrollAreaRef, scrollAreaRect] = useResizeObserver();

  // Resizing state
  const resizingColumn = useRef<{ key: string; startX: number; startWidth: number } | null>(null);

  const calculateDynamicWidth = useCallback((column: DataGridColumn<T>): number => {
    const DEFAULT_MIN_WIDTH = 100;
    const PADDING = 40;
    const CHAR_WIDTH = 8;
    // Header cell also renders a sort icon (14px + 4px gap, only when column.sortable
    // AND the grid-level `sortable` prop both hold — matches the render condition below
    // exactly) and a filter ActionIcon (30px + 10px "xs" gap, when filterable !== false).
    // The old heuristic only measured title text width, so headers with both icons
    // regularly computed a width narrower than their actual rendered content and wrapped
    // onto two lines even though there was room to fit on one.
    const SORT_ICON_WIDTH = (column.sortable && sortable) ? 14 + 4 : 0;
    const FILTER_ICON_WIDTH = column.filterable !== false ? 30 + 10 : 0;

    let width = column.minWidth || DEFAULT_MIN_WIDTH;
    width = Math.max(width, column.title.length * CHAR_WIDTH + PADDING + SORT_ICON_WIDTH + FILTER_ICON_WIDTH);

    if (column.maxWidth) {
      width = Math.min(width, column.maxWidth);
    }

    return Math.round(width);
  }, [sortable]);

  const baseWidthFor = useCallback((column: DataGridColumn<T>): number => {
    if (column.width) {
      return typeof column.width === 'number' ? column.width : parseInt(String(column.width), 10);
    }
    return calculateDynamicWidth(column);
  }, [calculateDynamicWidth]);

  // At most one column should be marked `primary` — it alone absorbs leftover container
  // width (or shrink pressure), clamped by its own minWidth/maxWidth. Every other column
  // (including Actions and the selection checkbox) stays pinned at its own fixed width,
  // never redistributed. If no column is primary, the table simply sits at its natural
  // total width and scrolls horizontally when the container is narrower.
  const applyPrimaryColumnSizing = useCallback((
    baseWidths: Record<string, number>,
    containerWidth: number,
  ): Record<string, number> => {
    const primary = columns.find(c => c.primary && !manuallyResizedKeys.current.has(c.key));
    if (!primary || containerWidth <= 0) return baseWidths;

    const otherColumnsWidth = columns
      .filter(c => c.key !== primary.key)
      .reduce((sum, c) => sum + (baseWidths[c.key] ?? 0), 0);
    const actionsReserve = showActions ? resolvedActionsWidth : 0;
    const checkboxReserve = selectable ? 40 : 0;
    const fixedWidth = otherColumnsWidth + actionsReserve + checkboxReserve;

    // Reserve a couple of px for the table's own border (withTableBorder/withColumnBorders)
    // which sits inside the measured container box — rounding down (never up) here
    // guarantees the computed total never exceeds the container by even 1px, so a table
    // that exactly fits never shows a phantom horizontal scrollbar.
    const BORDER_SAFETY_MARGIN = 4;
    let primaryWidth = containerWidth - fixedWidth - BORDER_SAFETY_MARGIN;
    if (primary.minWidth != null) primaryWidth = Math.max(primaryWidth, primary.minWidth);
    if (primary.maxWidth != null) primaryWidth = Math.min(primaryWidth, primary.maxWidth);

    return { ...baseWidths, [primary.key]: Math.floor(primaryWidth) };
  }, [columns, showActions, resolvedActionsWidth, selectable]);

  // Initialize column widths
  // Use a stable signature (key + explicit width + primary flag) to avoid recalculating
  // on every parent render when `columns` is defined inline (new reference each render).
  // eslint-disable-next-line react-hooks/exhaustive-deps
  const columnSignature = columns.map(c => `${c.key}:${c.width ?? ''}:${c.primary ? 1 : 0}`).join(',');

  useEffect(() => {
    const baseWidths: Record<string, number> = {};
    columns.forEach(column => { baseWidths[column.key] = baseWidthFor(column); });

    manuallyResizedKeys.current.clear();
    setColumnWidths(
      scrollAreaRect.width > 0 ? applyPrimaryColumnSizing(baseWidths, scrollAreaRect.width) : baseWidths
    );
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [columnSignature]);

  // Re-run primary-column sizing when the container is resized (e.g. narrower viewport,
  // sidebar collapse/expand, zoom change) without disturbing manually-resized columns.
  useEffect(() => {
    if (scrollAreaRect.width <= 0) return;

    setColumnWidths(prev => {
      const baseWidths: Record<string, number> = { ...prev };
      columns.forEach(column => {
        if (manuallyResizedKeys.current.has(column.key)) return;
        baseWidths[column.key] = baseWidthFor(column);
      });
      return applyPrimaryColumnSizing(baseWidths, scrollAreaRect.width);
    });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [scrollAreaRect.width]);

  // Handle column resize move
  const handleResizeMove = useCallback((e: MouseEvent) => {
    if (!resizingColumn.current) return;
    
    const { key, startX, startWidth } = resizingColumn.current;
    const diff = e.clientX - startX;
    const column = columns.find(col => col.key === key);

    if (!column) return;

    manuallyResizedKeys.current.add(key);

    let newWidth = startWidth + diff;
    
    // Apply minWidth constraint (default 100px)
    const minWidth = column.minWidth || 100;
    newWidth = Math.max(newWidth, minWidth);
    
    // Apply maxWidth constraint if specified
    if (column.maxWidth) {
      newWidth = Math.min(newWidth, column.maxWidth);
    }
    
    setColumnWidths(prev => ({
      ...prev,
      [key]: newWidth,
    }));
  }, [columns]);

  // Handle column resize end
  const handleResizeEnd = useCallback(() => {
    resizingColumn.current = null;
    document.removeEventListener('mousemove', handleResizeMove);
    document.removeEventListener('mouseup', handleResizeEnd);
    document.body.style.cursor = '';
    document.body.style.userSelect = '';
  }, [handleResizeMove]);

  // Handle column resize start
  const handleResizeStart = useCallback((
    e: React.MouseEvent,
    column: DataGridColumn<T>
  ) => {
    e.preventDefault();
    e.stopPropagation();
    
    resizingColumn.current = {
      key: column.key,
      startX: e.clientX,
      startWidth: columnWidths[column.key] || 100,
    };
    
    document.addEventListener('mousemove', handleResizeMove);
    document.addEventListener('mouseup', handleResizeEnd);
    document.body.style.cursor = 'col-resize';
    document.body.style.userSelect = 'none';
  }, [columnWidths, handleResizeMove, handleResizeEnd]);

  // Cleanup resize listeners on unmount
  useEffect(() => {
    return () => {
      document.removeEventListener('mousemove', handleResizeMove);
      document.removeEventListener('mouseup', handleResizeEnd);
      document.body.style.cursor = '';
      document.body.style.userSelect = '';
    };
  }, [handleResizeMove, handleResizeEnd]);

  // State for search debouncing
  const [debouncedSearchText] = useDebouncedValue(state.searchText, 500);

  const isInitialMount = useRef(true);
  const suppressNextSearch = useRef(false);

  const onSearchRef = useRef(onSearch);
  useEffect(() => { onSearchRef.current = onSearch; });

  const handleSearch = useCallback((value: string) => {
    setState(prev => ({ ...prev, searchText: value }));
  }, []);

  const handleClearSearch = useCallback(() => {
    setState(prev => ({ ...prev, searchText: '' }));
    onSearchRef.current?.('');
  }, []);

  const handleKeyDown = useCallback((event: React.KeyboardEvent<HTMLInputElement>) => {
    if (event.key === 'Enter') {
      onSearchRef.current?.(state.searchText);
    }
  }, [state.searchText]);

  useEffect(() => {
    if (isInitialMount.current) {
      isInitialMount.current = false;
      return;
    }
    if (suppressNextSearch.current) {
      suppressNextSearch.current = false;
      return;
    }
    onSearchRef.current?.(debouncedSearchText);
  }, [debouncedSearchText]); // eslint-disable-line react-hooks/exhaustive-deps

  const handleRefresh = useCallback(() => {
    suppressNextSearch.current = true;
    setState(prev => ({ ...prev, searchText: '', columnFilters: {} }));
    setSortConditions([]);
    onRefresh?.();
  }, [onRefresh]);

  // Handle reset filters
  const handleResetFilters = useCallback(() => {
    setState(prev => ({ 
      ...prev, 
      columnFilters: {} 
    }));
    onFilterChange?.([], 'and');
    
    // Reset all column filters using refs
    Object.values(columnFilterRefs.current).forEach(ref => {
      ref?.reset();
    });
  }, [onFilterChange]);

  // Check if there are active filters
  const hasActiveFilters = Object.keys(state.columnFilters).length > 0;

  // Get row key
  const getRowKey = useCallback((record: T): string => {
    if (typeof rowKey === 'function') {
      return rowKey(record);
    }
    return String(record[rowKey]);
  }, [rowKey]);

  // Handle selection
  const handleRowSelection = useCallback((rowKey: string, checked: boolean) => {
    const newSelection = checked
      ? [...state.selectedRows, rowKey]
      : state.selectedRows.filter(key => key !== rowKey);
    
    setState(prev => ({ ...prev, selectedRows: newSelection }));
    onSelectionChange?.(newSelection);
  }, [state.selectedRows, onSelectionChange]);

  // Handle sorting
  const handleSort = useCallback((column: string) => {
    if (!sortable || !onSortChange) return;

    const existingSort = sortConditions.find(sort => sort.property === column);
    let newSortConditions: SortCondition[];

    if (existingSort) {
      if (existingSort.direction === 'asc') {
        newSortConditions = sortConditions.map(sort =>
          sort.property === column
            ? { ...sort, direction: 'desc' as const }
            : sort
        );
      } else {
        newSortConditions = sortConditions.filter(sort => sort.property !== column);
      }
    } else {
      const newSort: SortCondition = {
        property: column,
        direction: 'asc',
        priority: sortConditions.length,
      };
      newSortConditions = [...sortConditions, newSort];
    }

    setSortConditions(newSortConditions);
    
    // Build sort expression for backend
    const sortExpression = newSortConditions.length > 0 
      ? newSortConditions
          .sort((a, b) => (a.priority || 0) - (b.priority || 0))
          .map(sort => `${sort.property},${sort.direction}`)
          .join(';')
      : '';
    
    onSortChange(sortExpression);
  }, [sortConditions, sortable, onSortChange]);

  // Handle column filter changes
  const handleApplyFilter = useCallback((filterExpression: string) => {
    // Extract column key from filter expression
    const columnKey = filterExpression.split(',')[0];
    
    setState(prev => {
      const newFilters = { ...prev.columnFilters };
      newFilters[columnKey] = filterExpression;
      
      // Build filter conditions and notify parent
      const filterConditions: FilterCondition[] = Object.values(newFilters)
        .map(expression => {
          const parts = expression.split(',');
          if (parts.length >= 2) {
            const op = parts[1] as FilterCondition['operator'];
            let value: string;
            let secondValue: string | undefined;
            if (op === 'between' || op === 'notbetween') {
              value = parts[2]?.replace(/%2C/g, ',') || '';
              secondValue = parts[3]?.replace(/%2C/g, ',');
            } else {
              value = parts.slice(2).join(',').replace(/%2C/g, ',');
            }
            return { property: parts[0], operator: op, value, secondValue };
          }
          return null;
        })
        .filter(Boolean) as FilterCondition[];

      onFilterChange?.(filterConditions, 'and');

      return {
        ...prev,
        columnFilters: newFilters,
      };
    });
  }, [onFilterChange]);

  const handleClearFilter = useCallback((columnKey: string) => {
    setState(prev => {
      const newFilters = { ...prev.columnFilters };
      delete newFilters[columnKey];

      // Build filter conditions and notify parent
      const filterConditions: FilterCondition[] = Object.values(newFilters)
        .map(expression => {
          const parts = expression.split(',');
          if (parts.length >= 2) {
            const op = parts[1] as FilterCondition['operator'];
            let value: string;
            let secondValue: string | undefined;
            if (op === 'between' || op === 'notbetween') {
              value = parts[2]?.replace(/%2C/g, ',') || '';
              secondValue = parts[3]?.replace(/%2C/g, ',');
            } else {
              value = parts.slice(2).join(',').replace(/%2C/g, ',');
            }
            return { property: parts[0], operator: op, value, secondValue };
          }
          return null;
        })
        .filter(Boolean) as FilterCondition[];
      
      onFilterChange?.(filterConditions, 'and');
      
      return {
        ...prev,
        columnFilters: newFilters,
      };
    });
  }, [onFilterChange]);

  const handleFilterOpen = useCallback((columnKey: string) => {
    // Close all other filters when this one opens
    Object.entries(columnFilterRefs.current).forEach(([key, ref]) => {
      if (key !== columnKey && ref) {
        ref.close();
      }
    });
  }, []);

  // Get sort icon for column
  const getSortIcon = useCallback((column: string) => {
    const sort = sortConditions.find(s => s.property === column);
    if (!sort) return <IconSelector size={14} />;
    return sort.direction === 'asc' 
      ? <IconChevronUp size={14} /> 
      : <IconChevronDown size={14} />;
  }, [sortConditions]);

  // Handle select all — union/subtract only current page to preserve cross-page selections
  const handleSelectAll = useCallback((checked: boolean) => {
    const currentPageKeys = data.map(record => getRowKey(record));
    const newSelection = checked
      ? [...new Set([...state.selectedRows, ...currentPageKeys])]
      : state.selectedRows.filter(key => !currentPageKeys.includes(key));

    setState(prev => ({ ...prev, selectedRows: newSelection }));
    onSelectionChange?.(newSelection);
  }, [data, state.selectedRows, onSelectionChange, getRowKey]);

  // Handle bulk action execution
  const handleBulkAction = useCallback((action: BulkAction) => {
    const execute = () => {
      action.onClick(state.selectedRows);
      setState(prev => ({ ...prev, selectedRows: [] }));
      onSelectionChange?.([]);
    };

    if (action.confirm) {
      modals.openConfirmModal({
        title: action.confirm.title,
        children: <Text size="sm">{action.confirm.content}</Text>,
        labels: { confirm: 'Confirm', cancel: 'Cancel' },
        confirmProps: { color: action.color || 'blue' },
        onConfirm: execute,
      });
    } else {
      execute();
    }
  }, [state.selectedRows, onSelectionChange]);

  // Render action buttons
  const renderActions = useCallback((record: T) => {
    const visibleActions = finalActions.filter(action =>
      !action.visible || action.visible(record)
    );

    if (visibleActions.length === 0) return null;

    return (
      <Group gap="xs" justify="center">
        {visibleActions.map(action => {
          const resolvedHref = typeof action.href === 'function' ? action.href(record) : action.href;
          const isDisabled = action.disabled?.(record);

          // icon mode: always render ActionIcon (requires action.icon to exist)
          if (actionButtonStyle === 'icon' && action.icon) {
            return resolvedHref ? (
              <ActionIcon
                key={action.key}
                component="a"
                href={resolvedHref}
                size="md"
                variant={action.variant || 'light'}
                color={action.color || 'gray'}
                disabled={isDisabled}
                title={action.label}
                onClick={action.onClick ? () => action.onClick!(record) : undefined}
              >
                {action.icon}
              </ActionIcon>
            ) : (
              <ActionIcon
                key={action.key}
                size="md"
                variant={action.variant || 'light'}
                color={action.color || 'gray'}
                disabled={isDisabled}
                onClick={() => action.onClick?.(record)}
                title={action.label}
              >
                {action.icon}
              </ActionIcon>
            );
          }

          // text mode (or icon mode without icon): render Button with label
          return resolvedHref ? (
            <Button
              key={action.key}
              component="a"
              href={resolvedHref}
              size="compact-xs"
              variant={action.variant || 'light'}
              color={action.color || 'gray'}
              disabled={isDisabled}
              leftSection={action.icon}
              onClick={action.onClick ? () => action.onClick!(record) : undefined}
            >
              {action.label}
            </Button>
          ) : (
            <Button
              key={action.key}
              size="compact-xs"
              variant={action.variant || 'light'}
              color={action.color || 'gray'}
              disabled={isDisabled}
              leftSection={action.icon}
              onClick={() => action.onClick?.(record)}
            >
              {action.label}
            </Button>
          );
        })}
      </Group>
    );
  }, [finalActions]);

  // Render cell content
  const renderCellContent = useCallback((
    column: DataGridColumn<T>, 
    record: T, 
    index: number
  ) => {
    if (column.render) {
      return column.render(record[column.dataIndex], record, index);
    }

    if (column.type === 'markdown') {
      const value = record[column.dataIndex] as { html?: string } | null | undefined;
      const html = value?.html ?? '';
      return (
        <Box
          className={styles.markdownCell}
          dangerouslySetInnerHTML={{ __html: html }}
        />
      );
    }

    return String(record[column.dataIndex] || '');
  }, []);

  // Table rows
  const rows = data.map((record, index) => {
    const key = getRowKey(record);
    const isSelected = state.selectedRows.includes(key);

    return (
      <Table.Tr key={key} bg={isSelected ? 'var(--mantine-color-blue-light)' : undefined}>
        {columns.map((column, colIndex) => (
          <Table.Td
            key={String(column.key)}
            style={{
              width: columnWidths[column.key] || column.width,
              textAlign: column.align || 'left'
            }}
          >
            {selectable && colIndex === 0 ? (
              <Group gap="xs" wrap="nowrap">
                <Checkbox
                  size="xs"
                  checked={isSelected}
                  onChange={(event: ChangeEvent<HTMLInputElement>) => handleRowSelection(key, event.currentTarget.checked)}
                />
                {renderCellContent(column, record, index)}
              </Group>
            ) : (
              renderCellContent(column, record, index)
            )}
          </Table.Td>
        ))}
        {showActions && finalActions.length > 0 && (
          <Table.Td style={{ width: resolvedActionsWidth }}>
            {renderActions(record)}
          </Table.Td>
        )}
      </Table.Tr>
    );
  });

  // Explicit total table width (sum of every column + actions + checkbox gutter) paired
  // with table-layout: fixed below — without both, the browser ignores per-cell inline
  // widths and stretches/redistributes columns (typically the last one) to fill the
  // container on its own, which is what caused Actions to balloon on wide screens.
  const tableWidth = columns.reduce((sum, column) => sum + (columnWidths[column.key] || (typeof column.width === 'number' ? column.width : 100)), 0)
    + (showActions && finalActions.length > 0 ? resolvedActionsWidth : 0)
    + (selectable ? 40 : 0);

  // Mantine's ScrollArea auto-shows its scrollbar via a ResizeObserver on the content
  // box, but that observer unreliably misses overflow introduced by a manual column
  // resize (rapid inline-style mousemove updates on an interior column) — reproducibly
  // confirmed some columns' resize handles trigger it and others don't. Since we already
  // compute both the table's true total width and the container's measured width, drive
  // the scrollbar directly from that comparison instead of trusting the observer.
  const tableOverflows = scrollAreaRect.width > 0 && tableWidth > scrollAreaRect.width;

  // Render create button based on mode
  const renderCreateButton = () => {
    const createConfig = getButtonConfig('create');
    if (!createConfig.visible || !onCreate) return null;

    return (
      <Button
        leftSection={createButtonIcon || <IconPlus size={16} />}
        onClick={onCreate}
        disabled={createConfig.disabled}
      >
        {createButtonText}
      </Button>
    );
  };

  return (
    <Box pos="relative">
      <LoadingOverlay visible={loading} />
      
      {/* Toolbar */}
      <Paper p="md" mb="md" withBorder>
        <Flex justify="space-between" align="center" wrap="wrap" gap="md">
          <Group>
            {/* Create Button - mode-aware */}
            {renderCreateButton()}

            {bulkActions && bulkActions.length > 0 && state.selectedRows.length > 0 && (
              <Menu shadow="md" withinPortal>
                <Menu.Target>
                  <Button variant="light" leftSection={<IconStack2 size={16} />}>
                    Bulk Actions ({state.selectedRows.length})
                  </Button>
                </Menu.Target>
                <Menu.Dropdown>
                  {bulkActions.map((action) => (
                    <Menu.Item
                      key={action.key}
                      color={action.color}
                      leftSection={action.icon}
                      onClick={() => handleBulkAction(action)}
                    >
                      {action.label}
                    </Menu.Item>
                  ))}
                </Menu.Dropdown>
              </Menu>
            )}
          </Group>

          <Group>
            {/* Search Input - only show if enabled based on mode */}
            {searchable && (mode !== 'special' || specialModeConfig?.search?.enabled !== false) && (
              <TextInput
                placeholder={searchPlaceholder}
                leftSection={<IconSearch size={16} />}
                rightSection={
                  state.searchText ? (
                    <ActionIcon
                      size="sm"
                      variant="subtle"
                      color="gray"
                      onClick={handleClearSearch}
                      title="Clear search"
                    >
                      <IconX size={14} />
                    </ActionIcon>
                  ) : null
                }
                value={state.searchText}
                onChange={(event: ChangeEvent<HTMLInputElement>) => handleSearch(event.currentTarget.value)}
                onKeyDown={handleKeyDown}
                style={{ width: 300, maxWidth: '100%', flex: '0 1 auto' }}
              />
            )}

            {refreshable && (
              <Button
                variant="light"
                leftSection={<IconRefresh size={16} />}
                onClick={handleRefresh}
              >
                Refresh
              </Button>
            )}

            {hasActiveFilters && (
              <Button
                variant="light"
                color="orange"
                leftSection={<IconFilterOff size={16} />}
                onClick={handleResetFilters}
              >
                Reset Filters
              </Button>
            )}

            {toolbarRightSection}
          </Group>
        </Flex>
      </Paper>

      {/* Table */}
      <Paper withBorder ref={scrollAreaRef}>
        <ScrollArea type={tableOverflows ? 'always' : 'never'} scrollbarSize={10}>
          <Table
            striped={striped}
            highlightOnHover={highlightOnHover}
            withTableBorder={withBorder}
            withColumnBorders={withColumnBorders}
            style={{ tableLayout: 'fixed', width: tableWidth }}
          >
            <Table.Thead>
              <Table.Tr>
                {columns.map((column, colIndex) => (
                  <Table.Th
                    key={String(column.key)}
                    style={{
                      width: columnWidths[column.key] || column.width,
                      textAlign: column.align || 'left',
                      position: 'relative',
                    }}
                  >
                    <Group gap="xs" justify={column.align === 'center' ? 'center' : column.align === 'right' ? 'flex-end' : 'flex-start'}>
                      {selectable && colIndex === 0 && (
                        <Checkbox
                          size="xs"
                          checked={data.length > 0 && data.every(record => state.selectedRows.includes(getRowKey(record)))}
                          indeterminate={data.some(record => state.selectedRows.includes(getRowKey(record))) && !data.every(record => state.selectedRows.includes(getRowKey(record)))}
                          onChange={(event: ChangeEvent<HTMLInputElement>) => handleSelectAll(event.currentTarget.checked)}
                        />
                      )}
                      {column.sortable && sortable ? (
                        <UnstyledButton
                          onClick={() => handleSort(String(column.dataIndex))}
                          style={{
                            display: 'flex',
                            alignItems: 'center',
                            gap: 4,
                          }}
                        >
                          <span>{column.title}</span>
                          {getSortIcon(String(column.dataIndex))}
                        </UnstyledButton>
                      ) : (
                        <span>{column.title}</span>
                      )}
                      
                      {(column.filterable !== false) && (
                        <ColumnFilter
                          ref={(ref) => { columnFilterRefs.current[column.key] = ref; }}
                          column={column as DataGridColumn}
                          onApplyFilter={handleApplyFilter}
                          onClearFilter={handleClearFilter}
                          onFilterOpen={handleFilterOpen}
                        />
                      )}
                    </Group>
                    
                    {/* Resize Handle */}
                    <Box
                      onMouseDown={(e: ReactMouseEvent<HTMLDivElement>) => handleResizeStart(e, column)}
                      style={{
                        position: 'absolute',
                        right: 0,
                        top: 0,
                        bottom: 0,
                        width: 4,
                        cursor: 'col-resize',
                        userSelect: 'none',
                        backgroundColor: 'transparent',
                        borderRight: '2px solid transparent',
                        transition: 'border-color 0.2s',
                        zIndex: 1,
                      }}
                      onMouseEnter={(e: ReactMouseEvent<HTMLDivElement>) => {
                        e.currentTarget.style.borderRightColor = 'var(--mantine-color-blue-6)';
                      }}
                      onMouseLeave={(e: ReactMouseEvent<HTMLDivElement>) => {
                        e.currentTarget.style.borderRightColor = 'transparent';
                      }}
                    />
                  </Table.Th>
                ))}
                {showActions && finalActions.length > 0 && (
                  <Table.Th style={{ width: resolvedActionsWidth, textAlign: 'center' }}>
                    Actions
                  </Table.Th>
                )}
              </Table.Tr>
            </Table.Thead>
            <Table.Tbody>
              {rows.length > 0 ? rows : (
                <Table.Tr>
                  <Table.Td 
                    colSpan={columns.length + (showActions ? 1 : 0) + (selectable ? 1 : 0)}
                    style={{ textAlign: 'center', padding: '2rem' }}
                  >
                    <Text c="dimmed">No data available</Text>
                  </Table.Td>
                </Table.Tr>
              )}
            </Table.Tbody>
          </Table>
        </ScrollArea>

        {/* Pagination */}
        {pagination && (
          <Group justify="space-between" p="md">
            <Text size="sm" c="dimmed">
              {pagination.total === 0
                ? 'No records'
                : `Showing ${(pagination.current - 1) * pagination.pageSize + 1} to ${Math.min(pagination.current * pagination.pageSize, pagination.total)} of ${pagination.total} entries`}
            </Text>
            
            <Group>
              {pagination.showSizeChanger && (
                <Group gap="xs">
                  <Text size="sm">Rows per page:</Text>
                  <Select
                    size="sm"
                    data={pagination.pageSizeOptions?.map(size => ({ 
                      value: String(size), 
                      label: String(size) 
                    })) || ['10', '25', '50', '100']}
                    value={String(pagination.pageSize)}
                    onChange={(value: string | null) => onPageChange?.(1, Number(value) || 10)}
                    style={{ 
                      width: 80,
                      textAlign: 'right'
                    }}
                    styles={{
                      input: { textAlign: 'right' }
                    }}
                    comboboxProps={{ withinPortal: false }}
                  />
                </Group>
              )}
              
              <Pagination
                total={Math.ceil(pagination.total / pagination.pageSize)}
                value={pagination.current}
                onChange={(page: number) => onPageChange?.(page, pagination.pageSize)}
                size="sm"
              />
            </Group>
          </Group>
        )}
      </Paper>
    </Box>
  );
}
