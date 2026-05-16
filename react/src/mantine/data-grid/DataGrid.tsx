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
import { useDebouncedValue } from '@mantine/hooks';
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
  actionsWidth = 120,
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

  // Sorting state
  const [sortConditions, setSortConditions] = useState<SortCondition[]>([]);

  // Column widths state (for resizing)
  const [columnWidths, setColumnWidths] = useState<Record<string, number>>({});
  
  // Resizing state
  const resizingColumn = useRef<{ key: string; startX: number; startWidth: number } | null>(null);

  const calculateDynamicWidth = useCallback((column: DataGridColumn<T>): number => {
    const DEFAULT_MIN_WIDTH = 100;
    const PADDING = 40;
    const CHAR_WIDTH = 8;

    let width = column.minWidth || DEFAULT_MIN_WIDTH;
    width = Math.max(width, column.title.length * CHAR_WIDTH + PADDING);

    if (column.maxWidth) {
      width = Math.min(width, column.maxWidth);
    }

    return Math.round(width);
  }, []);

  // Initialize column widths
  // Use a stable signature (key + explicit width) to avoid recalculating on
  // every parent render when `columns` is defined inline (new reference each render).
  // eslint-disable-next-line react-hooks/exhaustive-deps
  const columnSignature = columns.map(c => `${c.key}:${c.width ?? ''}`).join(',');

  useEffect(() => {
    const initialWidths: Record<string, number> = {};

    columns.forEach(column => {
      if (column.width) {
        initialWidths[column.key] = typeof column.width === 'number' ? column.width : parseInt(String(column.width), 10);
      } else {
        initialWidths[column.key] = calculateDynamicWidth(column);
      }
    });

    setColumnWidths(initialWidths);
  }, [columnSignature]); // eslint-disable-line react-hooks/exhaustive-deps

  // Handle column resize move
  const handleResizeMove = useCallback((e: MouseEvent) => {
    if (!resizingColumn.current) return;
    
    const { key, startX, startWidth } = resizingColumn.current;
    const diff = e.clientX - startX;
    const column = columns.find(col => col.key === key);
    
    if (!column) return;
    
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
            return {
              property: parts[0],
              operator: parts[1] as FilterCondition['operator'],
              value: parts[2]?.replace(/%2C/g, ',') || '',
              secondValue: parts[3]?.replace(/%2C/g, ','),
            };
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
            return {
              property: parts[0],
              operator: parts[1] as FilterCondition['operator'],
              value: parts[2]?.replace(/%2C/g, ',') || '',
              secondValue: parts[3]?.replace(/%2C/g, ','),
            };
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

          if (action.icon) {
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

          return resolvedHref ? (
            <Button
              key={action.key}
              component="a"
              href={resolvedHref}
              size="compact-xs"
              variant={action.variant || 'light'}
              color={action.color || 'gray'}
              disabled={isDisabled}
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
          <Table.Td style={{ width: actionsWidth }}>
            {renderActions(record)}
          </Table.Td>
        )}
      </Table.Tr>
    );
  });

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
                  <Button variant="light" color="blue" leftSection={<IconStack2 size={16} />}>
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
                style={{ minWidth: 300 }}
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
      <Paper withBorder>
        <ScrollArea>
          <Table
            striped={striped}
            highlightOnHover={highlightOnHover}
            withTableBorder={withBorder}
            withColumnBorders={withColumnBorders}
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
                  <Table.Th style={{ width: actionsWidth, textAlign: 'center' }}>
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
