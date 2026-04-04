import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import styles from './DataGrid.module.css';
import type { ChangeEvent } from 'react';
import {
  ActionIcon,
  Badge,
  Box,
  Button,
  Card,
  Checkbox,
  Flex,
  Group,
  Image,
  LoadingOverlay,
  Modal,
  MultiSelect,
  Pagination,
  Paper,
  Select,
  SimpleGrid,
  Stack,
  Text,
  TextInput,
} from '@mantine/core';
import { DateInput } from '@mantine/dates';
import { useDebouncedValue } from '@mantine/hooks';
import { modals } from '@mantine/modals';
import {
  IconAdjustmentsHorizontal,
  IconEdit,
  IconEye,
  IconFilterOff,
  IconListDetails,
  IconPlus,
  IconRefresh,
  IconSearch,
  IconTrash,
  IconX,
} from '@tabler/icons-react';
import { FilterExpressionBuilder } from '../../utils/FilterExpressionBuilder';
import { SortExpressionBuilder } from '../../utils/SortExpressionBuilder';
import type {
  ActionButton,
  CardDataGridProps,
  DataGridColumn,
  FilterCondition,
  FilterOperator,
  LogicalOperator,
  SortCondition,
  SpecialModeConfig,
} from './DataGrid.types';

interface FilterDraft extends FilterCondition {
  id: string;
}

const EMPTY_SELECTED_ROWS: string[] = [];

function areStringArraysEqual(left: string[], right: string[]): boolean {
  if (left === right) {
    return true;
  }

  if (left.length !== right.length) {
    return false;
  }

  for (let i = 0; i < left.length; i += 1) {
    if (left[i] !== right[i]) {
      return false;
    }
  }

  return true;
}

function getDefaultOperator(type?: DataGridColumn['type']): FilterOperator {
  switch (type) {
    case 'number':
      return 'eq';
    case 'date':
      return 'eq';
    case 'boolean':
      return 'eq';
    case 'enum':
      return 'in';
    default:
      return 'contains';
  }
}

function getOperators(type?: DataGridColumn['type']): Array<{ value: FilterOperator; label: string }> {
  if (type === 'number') {
    return [
      { value: 'eq', label: 'Equals' },
      { value: 'ne', label: 'Not Equals' },
      { value: 'gt', label: 'Greater Than' },
      { value: 'gte', label: 'Greater Than or Equal' },
      { value: 'lt', label: 'Less Than' },
      { value: 'lte', label: 'Less Than or Equal' },
      { value: 'between', label: 'Between' },
      { value: 'isnull', label: 'Is Null' },
      { value: 'isnotnull', label: 'Is Not Null' },
    ];
  }

  if (type === 'date') {
    return [
      { value: 'eq', label: 'On' },
      { value: 'ne', label: 'Not On' },
      { value: 'gt', label: 'After' },
      { value: 'gte', label: 'On or After' },
      { value: 'lt', label: 'Before' },
      { value: 'lte', label: 'On or Before' },
      { value: 'between', label: 'Between' },
      { value: 'isnull', label: 'Is Null' },
      { value: 'isnotnull', label: 'Is Not Null' },
    ];
  }

  if (type === 'boolean') {
    return [
      { value: 'eq', label: 'Equals' },
      { value: 'ne', label: 'Not Equals' },
      { value: 'isnull', label: 'Is Null' },
      { value: 'isnotnull', label: 'Is Not Null' },
    ];
  }

  if (type === 'enum') {
    return [
      { value: 'in', label: 'Is Any Of' },
      { value: 'notin', label: 'Is Not Any Of' },
      { value: 'eq', label: 'Equals' },
      { value: 'ne', label: 'Not Equals' },
      { value: 'isnull', label: 'Is Null' },
      { value: 'isnotnull', label: 'Is Not Null' },
    ];
  }

  return [
    { value: 'contains', label: 'Contains' },
    { value: 'eq', label: 'Equals' },
    { value: 'ne', label: 'Not Equals' },
    { value: 'startswith', label: 'Starts With' },
    { value: 'endswith', label: 'Ends With' },
    { value: 'isnull', label: 'Is Null' },
    { value: 'isnotnull', label: 'Is Not Null' },
  ];
}

function shouldRequireValue(operator: FilterOperator): boolean {
  return operator !== 'isnull' && operator !== 'isnotnull';
}

function toFilterDraft(condition: FilterCondition, index: number): FilterDraft {
  return {
    id: `${condition.property}-${condition.operator}-${index}`,
    property: condition.property,
    operator: condition.operator,
    value: condition.value,
    secondValue: condition.secondValue,
  };
}

export function CardDataGrid<T extends Record<string, unknown>>({
  data = [],
  loading = false,
  columns = [],
  mode = 'action',
  specialModeConfig,
  actions,
  showActions = true,
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
  onView,
  onDetails,
  onDelete,
  deleteConfirmTitle = 'Confirm Delete',
  deleteConfirmContent = 'Are you sure you want to delete this item? This action cannot be undone.',
  selectable = false,
  selectedRows,
  onSelectionChange,
  rowKey = 'id',
  onFilterChange,
  filterPresets = [],
  sortPresets = [],
  initialFilterConditions = [],
  initialFilterOperator = 'and',
  initialSortConditions = [],
  onFilterExpressionChange,
  toolbarConfig,
  cardLayout,
  cardImage,
  cardTitle,
  cardSubtitle,
  renderCard,
  emptyStateText = 'No data available',
}: CardDataGridProps<T>) {
  const [searchText, setSearchText] = useState('');
  const [debouncedSearchText] = useDebouncedValue(searchText, 500);
  const [optionsOpen, setOptionsOpen] = useState(false);
  const [selectedFilterPreset, setSelectedFilterPreset] = useState<string | null>(null);
  const [selectedSortPreset, setSelectedSortPreset] = useState<string | null>(null);
  const [filterOperator, setFilterOperator] = useState<LogicalOperator>(initialFilterOperator);
  const [filterDrafts, setFilterDrafts] = useState<FilterDraft[]>(
    initialFilterConditions.map(toFilterDraft)
  );
  const [sortConditions, setSortConditions] = useState<SortCondition[]>(initialSortConditions);
  const controlledSelectedRows = selectedRows ?? EMPTY_SELECTED_ROWS;
  const [internalSelection, setInternalSelection] = useState<string[]>(controlledSelectedRows);

  const onSearchRef = useRef(onSearch);
  const isInitialMount = useRef(true);

  useEffect(() => {
    onSearchRef.current = onSearch;
  }, [onSearch]);

  useEffect(() => {
    setInternalSelection((previous) =>
      areStringArraysEqual(previous, controlledSelectedRows)
        ? previous
        : [...controlledSelectedRows]
    );
  }, [controlledSelectedRows]);

  useEffect(() => {
    if (isInitialMount.current) {
      isInitialMount.current = false;
      return;
    }
    onSearchRef.current?.(debouncedSearchText);
  }, [debouncedSearchText]);

  const getButtonConfig = useCallback(
    (buttonKey: string) => {
      switch (mode) {
        case 'view':
          return {
            overview: { visible: true, disabled: false },
            details: { visible: true, disabled: false },
            edit: { visible: false, disabled: false },
            delete: { visible: false, disabled: false },
            create: { visible: false, disabled: false },
          }[buttonKey] || { visible: false, disabled: false };
        case 'special': {
          const config = specialModeConfig?.[buttonKey as keyof SpecialModeConfig];
          if (config && 'visible' in config) {
            return {
              visible: config.visible !== false,
              disabled: config.disabled || false,
            };
          }
          return { visible: true, disabled: false };
        }
        default:
          return { visible: true, disabled: false };
      }
    },
    [mode, specialModeConfig]
  );

  const defaultActions: ActionButton<T>[] = useMemo(
    () => [
      {
        key: 'overview',
        label: 'Overview',
        icon: <IconEye size={16} />,
        color: 'blue',
        variant: 'light',
        onClick: (record) => onView?.(record),
        visible: () => {
          const config = getButtonConfig('overview');
          return config.visible && !!onView;
        },
        disabled: () => getButtonConfig('overview').disabled,
      },
      {
        key: 'details',
        label: 'Details',
        icon: <IconListDetails size={16} />,
        color: 'cyan',
        variant: 'light',
        onClick: (record) => onDetails?.(record),
        visible: () => {
          const config = getButtonConfig('details');
          return config.visible && !!onDetails;
        },
        disabled: () => getButtonConfig('details').disabled,
      },
      {
        key: 'edit',
        label: 'Edit',
        icon: <IconEdit size={16} />,
        color: 'orange',
        variant: 'light',
        onClick: (record) => onEdit?.(record),
        visible: () => {
          const config = getButtonConfig('edit');
          return config.visible && !!onEdit;
        },
        disabled: () => getButtonConfig('edit').disabled,
      },
      {
        key: 'delete',
        label: 'Delete',
        icon: <IconTrash size={16} />,
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
    ],
    [
      onView,
      onDetails,
      onEdit,
      onDelete,
      deleteConfirmTitle,
      deleteConfirmContent,
      getButtonConfig,
    ]
  );

  const finalActions = actions || defaultActions;

  const availableFilterColumns = useMemo(
    () => columns.filter((column) => column.filterable !== false),
    [columns]
  );

  const availableSortColumns = useMemo(
    () => columns.filter((column) => column.sortable !== false),
    [columns]
  );

  const hasActiveFilters = filterDrafts.length > 0;

  const toolbar = {
    showSearch: toolbarConfig?.showSearch ?? searchable,
    showCreate: toolbarConfig?.showCreate ?? true,
    showRefresh: toolbarConfig?.showRefresh ?? refreshable,
    showOptions: toolbarConfig?.showOptions ?? true,
  };

  const gridCols = {
    base: cardLayout?.base ?? 1,
    xs: cardLayout?.xs,
    sm: cardLayout?.sm,
    md: cardLayout?.md,
    lg: cardLayout?.lg,
    xl: cardLayout?.xl,
  };

  const getRowKey = useCallback(
    (record: T): string => {
      if (typeof rowKey === 'function') {
        return rowKey(record);
      }
      return String(record[rowKey]);
    },
    [rowKey]
  );

  const applyStateToParent = useCallback(
    (nextFilters: FilterCondition[], nextOperator: LogicalOperator, nextSort: SortCondition[]) => {
      onFilterChange?.(nextFilters, nextOperator);
      onFilterExpressionChange?.(FilterExpressionBuilder.buildFilterExpression(nextFilters, nextOperator));
      onSortChange?.(SortExpressionBuilder.buildSortExpression(nextSort));
    },
    [onFilterChange, onFilterExpressionChange, onSortChange]
  );

  const handleRefresh = useCallback(() => {
    setSearchText('');
    setFilterDrafts([]);
    setSortConditions([]);
    setSelectedFilterPreset(null);
    setSelectedSortPreset(null);
    applyStateToParent([], filterOperator, []);
    onSearchRef.current?.('');
    onRefresh?.();
  }, [applyStateToParent, filterOperator, onRefresh]);

  const handleClearFilters = useCallback(() => {
    setFilterDrafts([]);
    setSelectedFilterPreset(null);
    applyStateToParent([], filterOperator, sortConditions);
  }, [applyStateToParent, filterOperator, sortConditions]);

  const handleAddFilter = useCallback(() => {
    const firstColumn = availableFilterColumns[0];
    if (!firstColumn) {
      return;
    }

    setFilterDrafts((prev) => [
      ...prev,
      {
        id: `${String(firstColumn.key)}-${prev.length + 1}`,
        property: String(firstColumn.key),
        operator: getDefaultOperator(firstColumn.type),
        value: '',
      },
    ]);
  }, [availableFilterColumns]);

  const handleFilterDraftChange = useCallback((id: string, updater: (draft: FilterDraft) => FilterDraft) => {
    setFilterDrafts((prev) => prev.map((draft) => (draft.id === id ? updater(draft) : draft)));
  }, []);

  const handleRemoveFilter = useCallback((id: string) => {
    setFilterDrafts((prev) => prev.filter((draft) => draft.id !== id));
  }, []);

  const handleApplyOptions = useCallback(() => {
    const validFilters = filterDrafts.filter((draft) => {
      if (!shouldRequireValue(draft.operator)) {
        return true;
      }

      if (draft.operator === 'between') {
        return String(draft.value ?? '').trim() !== '' && String(draft.secondValue ?? '').trim() !== '';
      }

      if (draft.operator === 'in' || draft.operator === 'notin') {
        if (Array.isArray(draft.value)) {
          return draft.value.length > 0;
        }
      }

      return String(draft.value ?? '').trim() !== '';
    });

    const normalizedSort = sortConditions.map((condition, index) => ({
      ...condition,
      priority: index,
    }));

    applyStateToParent(validFilters, filterOperator, normalizedSort);
    setOptionsOpen(false);
  }, [applyStateToParent, filterDrafts, filterOperator, sortConditions]);

  const handleFilterPresetChange = useCallback(
    (presetKey: string | null) => {
      setSelectedFilterPreset(presetKey);
      if (!presetKey) {
        return;
      }

      const preset = filterPresets.find((item) => item.key === presetKey);
      if (!preset) {
        return;
      }

      const nextOperator = preset.operator || 'and';
      setFilterOperator(nextOperator);
      setFilterDrafts(preset.conditions.map(toFilterDraft));
    },
    [filterPresets]
  );

  const handleSortPresetChange = useCallback(
    (presetKey: string | null) => {
      setSelectedSortPreset(presetKey);
      if (!presetKey) {
        return;
      }

      const preset = sortPresets.find((item) => item.key === presetKey);
      if (!preset) {
        return;
      }

      setSortConditions(preset.conditions);
    },
    [sortPresets]
  );

  const handleRowSelection = useCallback(
    (key: string, checked: boolean) => {
      const nextSelection = checked
        ? [...internalSelection, key]
        : internalSelection.filter((item) => item !== key);

      setInternalSelection(nextSelection);
      onSelectionChange?.(nextSelection);
    },
    [internalSelection, onSelectionChange]
  );

  const handleSelectAll = useCallback(
    (checked: boolean) => {
      const nextSelection = checked ? data.map((record) => getRowKey(record)) : [];
      setInternalSelection(nextSelection);
      onSelectionChange?.(nextSelection);
    },
    [data, getRowKey, onSelectionChange]
  );

  const renderFieldValue = useCallback(
    (column: DataGridColumn<T>, record: T, index: number) => {
      if (column.render) {
        return column.render(record[column.dataIndex], record, index);
      }

      const value = record[column.dataIndex];
      if (value === null || value === undefined || value === '') {
        return '-';
      }

      if (column.type === 'markdown') {
        const markdownValue = value as { html?: string };
        const html = markdownValue?.html ?? '';
        return (
          <Box
            className={styles.markdownCell}
            dangerouslySetInnerHTML={{ __html: html }}
          />
        );
      }

      return String(value);
    },
    []
  );

  const cardTitleColumn = useMemo(
    () => columns.find((column) => column.cardRole === 'title') || columns[0],
    [columns]
  );

  const cardDetailsColumn = useMemo(
    () => columns.find((column) => column.cardRole === 'details'),
    [columns]
  );

  const cardBodyColumns = useMemo(
    () =>
      columns
        .filter((column) => column !== cardTitleColumn && column !== cardDetailsColumn)
        .slice(0, 6),
    [columns, cardDetailsColumn, cardTitleColumn]
  );

  const renderFilterValueInput = useCallback(
    (draft: FilterDraft, column?: DataGridColumn<T>) => {
      if (!column || !shouldRequireValue(draft.operator)) {
        return null;
      }

      if (column.type === 'enum') {
        const options =
          column.enumOptions?.map((opt) => ({ value: String(opt.value), label: opt.label })) || [];

        if (draft.operator === 'in' || draft.operator === 'notin') {
          return (
            <MultiSelect
              placeholder="Select values"
              data={options}
              value={Array.isArray(draft.value) ? (draft.value as string[]) : []}
              onChange={(value: string[]) => {
                handleFilterDraftChange(draft.id, (current) => ({ ...current, value }));
              }}
              searchable
              clearable
            />
          );
        }

        return (
          <Select
            placeholder="Select value"
            data={options}
            value={draft.value ? String(draft.value) : null}
            onChange={(value: string | null) => {
              handleFilterDraftChange(draft.id, (current) => ({ ...current, value: value || '' }));
            }}
            clearable
          />
        );
      }

      if (column.type === 'boolean') {
        return (
          <Select
            placeholder="Select value"
            data={[
              { value: 'true', label: 'True' },
              { value: 'false', label: 'False' },
            ]}
            value={draft.value ? String(draft.value) : null}
            onChange={(value: string | null) => {
              handleFilterDraftChange(draft.id, (current) => ({ ...current, value: value || '' }));
            }}
            clearable
          />
        );
      }

      if (column.type === 'number') {
        return (
          <TextInput
            type="number"
            placeholder="Value"
            value={draft.value ? String(draft.value) : ''}
            onChange={(event: ChangeEvent<HTMLInputElement>) => {
              handleFilterDraftChange(draft.id, (current) => ({
                ...current,
                value: event.currentTarget.value,
              }));
            }}
          />
        );
      }

      if (column.type === 'date') {
        return (
          <DateInput
            value={draft.value ? new Date(String(draft.value)) : null}
            placeholder="Pick date"
            onChange={(value) => {
              handleFilterDraftChange(draft.id, (current) => ({
                ...current,
                value: value ? String(value) : '',
              }));
            }}
            clearable
          />
        );
      }

      return (
        <TextInput
          placeholder="Value"
          value={draft.value ? String(draft.value) : ''}
          onChange={(event: ChangeEvent<HTMLInputElement>) => {
            handleFilterDraftChange(draft.id, (current) => ({
              ...current,
              value: event.currentTarget.value,
            }));
          }}
        />
      );
    },
    [handleFilterDraftChange]
  );

  const cards = data.map((record, index) => {
    const key = getRowKey(record);
    const isSelected = internalSelection.includes(key);
    const visibleActions = finalActions.filter((action) => !action.visible || action.visible(record));
    const imageUrl = cardImage?.dataIndex ? (record[cardImage.dataIndex] as string | undefined) : undefined;

    if (renderCard) {
      return (
        <Box key={key}>
          {renderCard(record, index)}
        </Box>
      );
    }

    return (
      <Card
        key={key}
        withBorder
        radius="md"
        shadow="sm"
        style={{
          height: '100%',
          borderColor: isSelected ? 'var(--mantine-color-blue-5)' : undefined,
        }}
      >
        <Stack gap="sm" h="100%">
          {(cardImage?.render || imageUrl) && (
            <Box>
              {cardImage?.render ? (
                cardImage.render(record, index)
              ) : (
                <Image
                  src={imageUrl}
                  alt={cardImage?.alt || 'Card image'}
                  h={cardImage?.height || 180}
                  fit={cardImage?.fit || 'cover'}
                  radius="sm"
                />
              )}
            </Box>
          )}

          <Group justify="space-between" align="flex-start">
            <Stack gap={2} style={{ flex: 1 }}>
              {cardTitleColumn?.type === 'markdown' ? (
                <Box>
                  {cardTitle
                    ? cardTitle(record, index)
                    : cardTitleColumn
                      ? renderFieldValue(cardTitleColumn, record, index)
                      : 'Item'}
                </Box>
              ) : (
                <Text component="div" fw={600} lineClamp={2}>
                  {cardTitle
                    ? cardTitle(record, index)
                    : cardTitleColumn
                      ? renderFieldValue(cardTitleColumn, record, index)
                      : 'Item'}
                </Text>
              )}
              {cardSubtitle ? (
                <Text c="dimmed">{cardSubtitle(record, index)}</Text>
              ) : cardDetailsColumn?.type === 'markdown' ? (
                <Box style={{ color: 'var(--mantine-color-gray-6)' }}>
                  {renderFieldValue(cardDetailsColumn, record, index)}
                </Box>
              ) : cardDetailsColumn ? (
                <Text c="dimmed">{renderFieldValue(cardDetailsColumn, record, index)}</Text>
              ) : null}
            </Stack>
            {selectable && (
              <Checkbox
                checked={isSelected}
                onChange={(event: ChangeEvent<HTMLInputElement>) => handleRowSelection(key, event.currentTarget.checked)}
              />
            )}
          </Group>

          <Stack gap="xs" style={{ flex: 1 }}>
            {cardBodyColumns.map((column) => (
              <Group key={String(column.key)} justify="space-between" align="flex-start" wrap="nowrap">
                <Text size="sm" c="dimmed">
                  {column.title}
                </Text>
                <Box style={{ textAlign: 'right' }}>{renderFieldValue(column, record, index)}</Box>
              </Group>
            ))}
          </Stack>

          {showActions && visibleActions.length > 0 && (
            <Group gap="xs" mt="sm">
              {visibleActions.map((action) => (
                action.icon ? (
                  <ActionIcon
                    key={action.key}
                    size="sm"
                    variant={action.variant || 'light'}
                    color={action.color || 'gray'}
                    disabled={action.disabled?.(record)}
                    onClick={() => action.onClick(record)}
                    title={action.label}
                  >
                    {action.icon}
                  </ActionIcon>
                ) : (
                  <Button
                    key={action.key}
                    size="compact-xs"
                    variant={action.variant || 'light'}
                    color={action.color || 'gray'}
                    disabled={action.disabled?.(record)}
                    onClick={() => action.onClick(record)}
                  >
                    {action.label}
                  </Button>
                )
              ))}
            </Group>
          )}
        </Stack>
      </Card>
    );
  });

  return (
    <Box pos="relative">
      <LoadingOverlay visible={loading} />

      <Paper p="md" mb="md" withBorder>
        <Flex justify="space-between" align="center" wrap="wrap" gap="md">
          <Group>
            {toolbar.showCreate && onCreate && getButtonConfig('create').visible && (
              <Button
                leftSection={createButtonIcon || <IconPlus size={16} />}
                onClick={onCreate}
                disabled={getButtonConfig('create').disabled}
              >
                {createButtonText}
              </Button>
            )}

            {selectable && (
              <Checkbox
                label="Select all"
                checked={internalSelection.length === data.length && data.length > 0}
                indeterminate={internalSelection.length > 0 && internalSelection.length < data.length}
                onChange={(event: ChangeEvent<HTMLInputElement>) => handleSelectAll(event.currentTarget.checked)}
              />
            )}
          </Group>

          <Group>
            {toolbar.showSearch && (mode !== 'special' || specialModeConfig?.search?.enabled !== false) && (
              <TextInput
                placeholder={searchPlaceholder}
                leftSection={<IconSearch size={16} />}
                rightSection={
                  searchText ? (
                    <ActionIcon
                      size="sm"
                      variant="subtle"
                      color="gray"
                      onClick={() => {
                        setSearchText('');
                        onSearchRef.current?.('');
                      }}
                      title="Clear search"
                    >
                      <IconX size={14} />
                    </ActionIcon>
                  ) : null
                }
                value={searchText}
                onChange={(event: ChangeEvent<HTMLInputElement>) => setSearchText(event.currentTarget.value)}
                style={{ minWidth: 280 }}
              />
            )}

            {toolbar.showRefresh && (
              <Button variant="light" leftSection={<IconRefresh size={16} />} onClick={handleRefresh}>
                Refresh
              </Button>
            )}

            {hasActiveFilters && (
              <Button variant="light" color="orange" leftSection={<IconFilterOff size={16} />} onClick={handleClearFilters}>
                Reset Filters
              </Button>
            )}

            {toolbar.showOptions && (
              <Button
                variant="light"
                leftSection={<IconAdjustmentsHorizontal size={16} />}
                onClick={() => setOptionsOpen(true)}
              >
                Options
              </Button>
            )}

            {toolbarRightSection}
          </Group>
        </Flex>
      </Paper>

      <SimpleGrid cols={gridCols}>
        {cards.length > 0 ? cards : (
          <Paper withBorder p="xl">
            <Text c="dimmed" ta="center">
              {emptyStateText}
            </Text>
          </Paper>
        )}
      </SimpleGrid>

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
                  data={pagination.pageSizeOptions?.map((size) => ({
                    value: String(size),
                    label: String(size),
                  })) || ['10', '25', '50', '100']}
                  value={String(pagination.pageSize)}
                  onChange={(value: string | null) => onPageChange?.(1, Number(value) || 10)}
                  style={{ width: 80 }}
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

      <Modal
        opened={optionsOpen}
        onClose={() => setOptionsOpen(false)}
        title="Filter and Sort"
        size="xl"
      >
        <Stack gap="md">
          <Group grow>
            <Select
              label="Filter preset"
              placeholder="Select preset"
              data={filterPresets.map((preset) => ({ value: preset.key, label: preset.label }))}
              value={selectedFilterPreset}
              onChange={handleFilterPresetChange}
              clearable
            />

            <Select
              label="Sort preset"
              placeholder="Select preset"
              data={sortPresets.map((preset) => ({ value: preset.key, label: preset.label }))}
              value={selectedSortPreset}
              onChange={handleSortPresetChange}
              clearable
            />
          </Group>

          <Group justify="space-between">
            <Group>
              <Text fw={600}>Filter conditions</Text>
              <Badge variant="light">{filterDrafts.length}</Badge>
            </Group>
            <Group>
              <Select
                size="xs"
                data={[
                  { value: 'and', label: 'Match all (AND)' },
                  { value: 'or', label: 'Match any (OR)' },
                ]}
                value={filterOperator}
                onChange={(value: string | null) => setFilterOperator((value as LogicalOperator) || 'and')}
                w={180}
              />
              <Button size="xs" variant="light" onClick={handleAddFilter}>
                Add filter
              </Button>
            </Group>
          </Group>

          <Stack gap="xs">
            {filterDrafts.map((draft) => {
              const column = availableFilterColumns.find((item) => String(item.key) === draft.property);
              const operatorOptions = getOperators(column?.type);

              return (
                <Paper key={draft.id} p="sm" withBorder>
                  <Group grow align="flex-end">
                    <Select
                      label="Field"
                      data={availableFilterColumns.map((item) => ({
                        value: String(item.key),
                        label: item.title,
                      }))}
                      value={draft.property}
                      onChange={(value: string | null) => {
                        const nextColumn = availableFilterColumns.find((item) => String(item.key) === value);
                        if (!value || !nextColumn) {
                          return;
                        }
                        handleFilterDraftChange(draft.id, (current) => ({
                          ...current,
                          property: value,
                          operator: getDefaultOperator(nextColumn.type),
                          value: '',
                          secondValue: undefined,
                        }));
                      }}
                    />

                    <Select
                      label="Operator"
                      data={operatorOptions}
                      value={draft.operator}
                      onChange={(value: string | null) => {
                        if (!value) {
                          return;
                        }
                        handleFilterDraftChange(draft.id, (current) => ({
                          ...current,
                          operator: value as FilterOperator,
                          value: '',
                          secondValue: undefined,
                        }));
                      }}
                    />

                    {renderFilterValueInput(draft, column)}

                    {draft.operator === 'between' && (
                      <TextInput
                        label="Second value"
                        value={draft.secondValue ? String(draft.secondValue) : ''}
                        onChange={(event: ChangeEvent<HTMLInputElement>) => {
                          handleFilterDraftChange(draft.id, (current) => ({
                            ...current,
                            secondValue: event.currentTarget.value,
                          }));
                        }}
                      />
                    )}

                    <ActionIcon
                      variant="subtle"
                      color="red"
                      onClick={() => handleRemoveFilter(draft.id)}
                      title="Remove filter"
                    >
                      <IconTrash size={16} />
                    </ActionIcon>
                  </Group>
                </Paper>
              );
            })}

            {filterDrafts.length === 0 && <Text c="dimmed">No filters configured.</Text>}
          </Stack>

          <Group justify="space-between" align="flex-end">
            <Text fw={600}>Sort</Text>
            <Button
              size="xs"
              variant="light"
              onClick={() => {
                const column = availableSortColumns[0];
                if (!column) {
                  return;
                }
                setSortConditions((prev) => [
                  ...prev,
                  {
                    property: String(column.key),
                    direction: 'asc',
                    priority: prev.length,
                  },
                ]);
              }}
            >
              Add sort
            </Button>
          </Group>

          <Stack gap="xs">
            {sortConditions.map((condition, index) => (
              <Paper key={`${condition.property}-${index}`} p="sm" withBorder>
                <Group grow align="flex-end">
                  <Select
                    label="Field"
                    data={availableSortColumns.map((column) => ({
                      value: String(column.key),
                      label: column.title,
                    }))}
                    value={condition.property}
                    onChange={(value: string | null) => {
                      if (!value) {
                        return;
                      }
                      setSortConditions((prev) =>
                        prev.map((item, itemIndex) =>
                          itemIndex === index ? { ...item, property: value } : item
                        )
                      );
                    }}
                  />

                  <Select
                    label="Direction"
                    data={[
                      { value: 'asc', label: 'Ascending' },
                      { value: 'desc', label: 'Descending' },
                    ]}
                    value={condition.direction}
                    onChange={(value: string | null) => {
                      if (!value) {
                        return;
                      }
                      setSortConditions((prev) =>
                        prev.map((item, itemIndex) =>
                          itemIndex === index
                            ? { ...item, direction: value as SortCondition['direction'] }
                            : item
                        )
                      );
                    }}
                  />

                  <ActionIcon
                    variant="subtle"
                    color="red"
                    onClick={() => {
                      setSortConditions((prev) => prev.filter((_, itemIndex) => itemIndex !== index));
                    }}
                    title="Remove sort"
                  >
                    <IconTrash size={16} />
                  </ActionIcon>
                </Group>
              </Paper>
            ))}

            {sortConditions.length === 0 && <Text c="dimmed">No sort configured.</Text>}
          </Stack>

          <Group justify="space-between">
            <Button
              variant="subtle"
              color="gray"
              onClick={() => {
                setFilterDrafts([]);
                setSortConditions([]);
                setSelectedFilterPreset(null);
                setSelectedSortPreset(null);
              }}
            >
              Reset options
            </Button>

            <Group>
              <Button variant="default" onClick={() => setOptionsOpen(false)}>
                Cancel
              </Button>
              <Button onClick={handleApplyOptions}>Apply</Button>
            </Group>
          </Group>
        </Stack>
      </Modal>
    </Box>
  );
}
