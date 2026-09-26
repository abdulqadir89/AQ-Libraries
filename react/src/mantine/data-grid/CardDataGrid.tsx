import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import styles from './DataGrid.module.css';
import type { ChangeEvent } from 'react';
import {
  ActionIcon,
  Box,
  Button,
  Card,
  Checkbox,
  Flex,
  Group,
  Image,
  LoadingOverlay,
  Menu,
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
  IconFilterOff,
  IconPlus,
  IconRefresh,
  IconSearch,
  IconStack2,
  IconTrash,
  IconX,
} from '@tabler/icons-react';
import { FilterExpressionBuilder } from '../../utils/FilterExpressionBuilder';
import { SortExpressionBuilder } from '../../utils/SortExpressionBuilder';
import { useAQLocale } from '../locale';
import type { CardDataGridMessages } from '../locale';
import type {
  BulkAction,
  CardDataGridProps,
  DataGridColumn,
  FilterCondition,
  FilterOperator,
  LogicalOperator,
  SortCondition,
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

function getOperators(
  type: DataGridColumn['type'] | undefined,
  m: CardDataGridMessages
): Array<{ value: FilterOperator; label: string }> {
  if (type === 'number') {
    return [
      { value: 'eq', label: m.operatorEquals },
      { value: 'ne', label: m.operatorNotEquals },
      { value: 'gt', label: m.operatorGreaterThan },
      { value: 'gte', label: m.operatorGreaterThanOrEqual },
      { value: 'lt', label: m.operatorLessThan },
      { value: 'lte', label: m.operatorLessThanOrEqual },
      { value: 'between', label: m.operatorBetween },
      { value: 'isnull', label: m.operatorIsNull },
      { value: 'isnotnull', label: m.operatorIsNotNull },
    ];
  }

  if (type === 'date') {
    return [
      { value: 'eq', label: m.operatorOn },
      { value: 'ne', label: m.operatorNotOn },
      { value: 'gt', label: m.operatorAfter },
      { value: 'gte', label: m.operatorOnOrAfter },
      { value: 'lt', label: m.operatorBefore },
      { value: 'lte', label: m.operatorOnOrBefore },
      { value: 'between', label: m.operatorBetween },
      { value: 'isnull', label: m.operatorIsNull },
      { value: 'isnotnull', label: m.operatorIsNotNull },
    ];
  }

  if (type === 'boolean') {
    return [
      { value: 'eq', label: m.operatorEquals },
      { value: 'ne', label: m.operatorNotEquals },
      { value: 'isnull', label: m.operatorIsNull },
      { value: 'isnotnull', label: m.operatorIsNotNull },
    ];
  }

  if (type === 'enum') {
    return [
      { value: 'in', label: m.operatorIsAnyOf },
      { value: 'notin', label: m.operatorIsNotAnyOf },
      { value: 'eq', label: m.operatorEquals },
      { value: 'ne', label: m.operatorNotEquals },
      { value: 'isnull', label: m.operatorIsNull },
      { value: 'isnotnull', label: m.operatorIsNotNull },
    ];
  }

  return [
    { value: 'contains', label: m.operatorContains },
    { value: 'eq', label: m.operatorEquals },
    { value: 'ne', label: m.operatorNotEquals },
    { value: 'startswith', label: m.operatorStartsWith },
    { value: 'endswith', label: m.operatorEndsWith },
    { value: 'isnull', label: m.operatorIsNull },
    { value: 'isnotnull', label: m.operatorIsNotNull },
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
  pagination,
  onPageChange,
  searchable = true,
  searchPlaceholder,
  onSearch,
  toolbarRightSection,
  refreshable = true,
  onRefresh,
  sortable = true,
  onSortChange,
  onCreate,
  createButtonText,
  createButtonIcon,
  selectable = false,
  selectedRows,
  onSelectionChange,
  rowKey = 'id',
  onFilterChange,
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
  emptyStateText,
  onCardClick,
  cardHref,
  bulkActions,
}: CardDataGridProps<T>) {
  const { messages: aqMessages } = useAQLocale();
  const m = aqMessages.cardDataGrid;
  const resolvedSearchPlaceholder = searchPlaceholder ?? m.searchPlaceholder;
  const resolvedCreateButtonText = createButtonText ?? m.create;
  const resolvedEmptyStateText = emptyStateText ?? m.noData;
  const [searchText, setSearchText] = useState('');
  const [debouncedSearchText] = useDebouncedValue(searchText, 500);
  const [optionsOpen, setOptionsOpen] = useState(false);
  const [filterOperator, setFilterOperator] = useState<LogicalOperator>(initialFilterOperator);
  const [filterDrafts, setFilterDrafts] = useState<FilterDraft[]>(
    initialFilterConditions.map(toFilterDraft)
  );
  const [sortConditions, setSortConditions] = useState<SortCondition[]>(initialSortConditions);
  const controlledSelectedRows = selectedRows ?? EMPTY_SELECTED_ROWS;
  const [internalSelection, setInternalSelection] = useState<string[]>(controlledSelectedRows);

  const onSearchRef = useRef(onSearch);
  const isInitialMount = useRef(true);
  const suppressNextSearch = useRef(false);

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
    if (suppressNextSearch.current) {
      suppressNextSearch.current = false;
      return;
    }
    onSearchRef.current?.(debouncedSearchText);
  }, [debouncedSearchText]);

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
    suppressNextSearch.current = true;
    setSearchText('');
    setFilterDrafts([]);
    setSortConditions([]);
    onRefresh?.();
  }, [onRefresh]);

  const handleClearFilters = useCallback(() => {
    setFilterDrafts([]);
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
      const currentPageKeys = data.map((record) => getRowKey(record));
      const nextSelection = checked
        ? [...new Set([...internalSelection, ...currentPageKeys])]
        : internalSelection.filter((key) => !currentPageKeys.includes(key));
      setInternalSelection(nextSelection);
      onSelectionChange?.(nextSelection);
    },
    [data, getRowKey, internalSelection, onSelectionChange]
  );

  const handleBulkAction = useCallback(
    (action: BulkAction) => {
      const execute = () => {
        action.onClick(internalSelection);
        setInternalSelection([]);
        onSelectionChange?.([]);
      };

      if (action.confirm) {
        modals.openConfirmModal({
          title: action.confirm.title,
          children: <Text size="sm">{action.confirm.content}</Text>,
          labels: { confirm: m.confirm, cancel: m.cancel },
          confirmProps: { color: action.color || 'blue' },
          onConfirm: execute,
        });
      } else {
        execute();
      }
    },
    [internalSelection, onSelectionChange, m.confirm, m.cancel]
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
              placeholder={m.selectValues}
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
            placeholder={m.selectValue}
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
            placeholder={m.selectValue}
            data={[
              { value: 'true', label: m.true },
              { value: 'false', label: m.false },
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
            placeholder={m.enumValue}
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
            placeholder={m.pickDate}
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
          placeholder={m.enumValue}
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
    [handleFilterDraftChange, m]
  );

  const cards = data.map((record, index) => {
    const key = getRowKey(record);
    const isSelected = internalSelection.includes(key);
    const imageUrl = cardImage?.dataIndex ? (record[cardImage.dataIndex] as string | undefined) : undefined;

    if (renderCard) {
      return (
        <Box key={key}>
          {renderCard(record, index)}
        </Box>
      );
    }

    const resolvedCardHref = cardHref?.(record);
    return (
      <Card
        key={key}
        component={resolvedCardHref ? 'a' : undefined}
        href={resolvedCardHref}
        withBorder
        radius="md"
        shadow="sm"
        onClick={onCardClick ? () => onCardClick(record) : undefined}
        className={onCardClick || resolvedCardHref ? styles.clickableCard : undefined}
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
                  alt={cardImage?.alt || m.cardImageAlt}
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
                      : m.item}
                </Box>
              ) : (
                <Text component="div" fw={600} lineClamp={2}>
                  {cardTitle
                    ? cardTitle(record, index)
                    : cardTitleColumn
                      ? renderFieldValue(cardTitleColumn, record, index)
                      : m.item}
                </Text>
              )}
              {cardSubtitle ? (
                <Text c="dimmed">{cardSubtitle(record, index)}</Text>
              ) : cardDetailsColumn?.type === 'markdown' ? (
                <Box style={{ color: 'var(--mantine-color-gray-6)' }}>
                  {renderFieldValue(cardDetailsColumn, record, index)}
                </Box>
              ) : cardDetailsColumn ? (
                cardDetailsColumn.render ? (
                  renderFieldValue(cardDetailsColumn, record, index)
                ) : (
                  <Text component="div" c="dimmed">{renderFieldValue(cardDetailsColumn, record, index)}</Text>
                )
              ) : null}
            </Stack>
            {selectable && (
              <Checkbox
                checked={isSelected}
                onChange={(event: ChangeEvent<HTMLInputElement>) => handleRowSelection(key, event.currentTarget.checked)}
                onClick={(event) => event.stopPropagation()}
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
            {toolbar.showCreate && onCreate && (
              <Button
                leftSection={createButtonIcon || <IconPlus size={16} />}
                onClick={onCreate}
              >
                {resolvedCreateButtonText}
              </Button>
            )}

            {selectable && (
              <Checkbox
                label={m.selectAll}
                checked={internalSelection.length > 0 && data.every((record) => internalSelection.includes(getRowKey(record)))}
                indeterminate={internalSelection.length > 0 && !data.every((record) => internalSelection.includes(getRowKey(record)))}
                onChange={(event: ChangeEvent<HTMLInputElement>) => handleSelectAll(event.currentTarget.checked)}
              />
            )}

            {bulkActions && bulkActions.length > 0 && internalSelection.length > 0 && (
              <Menu shadow="md" withinPortal>
                <Menu.Target>
                  <Button variant="light" leftSection={<IconStack2 size={16} />}>
                    {m.bulkActions(internalSelection.length)}
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
            {toolbar.showSearch && (
              <TextInput
                placeholder={resolvedSearchPlaceholder}
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
                      title={m.clearSearch}
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
                {m.refresh}
              </Button>
            )}

            {hasActiveFilters && (
              <Button variant="light" color="orange" leftSection={<IconFilterOff size={16} />} onClick={handleClearFilters}>
                {m.resetFilters}
              </Button>
            )}

            {toolbar.showOptions && (
              <Button
                variant="light"
                leftSection={<IconAdjustmentsHorizontal size={16} />}
                onClick={() => setOptionsOpen(true)}
              >
                {m.options}
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
              {resolvedEmptyStateText}
            </Text>
          </Paper>
        )}
      </SimpleGrid>

      {pagination && (
        <Group justify="space-between" p="md">
          <Text size="sm" c="dimmed">
            {pagination.total === 0
              ? m.noRecords
              : m.showingEntries(
                  (pagination.current - 1) * pagination.pageSize + 1,
                  Math.min(pagination.current * pagination.pageSize, pagination.total),
                  pagination.total
                )}
          </Text>

          <Group>
            {pagination.showSizeChanger && (
              <Group gap="xs">
                <Text size="sm">{m.rowsPerPage}</Text>
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
        title={m.filterAndSort}
        size="xl"
      >
        <Stack gap="md">
          <Group justify="space-between">
            <Group>
              <Text fw={600}>{m.filterConditions}</Text>
            </Group>
            <Group>
              <Select
                size="xs"
                data={[
                  { value: 'and', label: m.matchAll },
                  { value: 'or', label: m.matchAny },
                ]}
                value={filterOperator}
                onChange={(value: string | null) => setFilterOperator((value as LogicalOperator) || 'and')}
                w={180}
              />
              <Button size="xs" variant="light" onClick={handleAddFilter}>
                {m.addFilter}
              </Button>
            </Group>
          </Group>

          <Stack gap="xs">
            {filterDrafts.map((draft) => {
              const column = availableFilterColumns.find((item) => String(item.key) === draft.property);
              const operatorOptions = getOperators(column?.type, m);

              return (
                <Paper key={draft.id} p="sm" withBorder>
                  <Group grow align="flex-end">
                    <Select
                      label={m.field}
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
                      label={m.operator}
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
                        label={m.secondValue}
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
                      title={m.removeFilter}
                    >
                      <IconTrash size={16} />
                    </ActionIcon>
                  </Group>
                </Paper>
              );
            })}

            {filterDrafts.length === 0 && <Text c="dimmed">{m.noFiltersConfigured}</Text>}
          </Stack>

          <Group justify="space-between" align="flex-end">
            <Text fw={600}>{m.sort}</Text>
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
              {m.addSort}
            </Button>
          </Group>

          <Stack gap="xs">
            {sortConditions.map((condition, index) => (
              <Paper key={`${condition.property}-${index}`} p="sm" withBorder>
                <Group grow align="flex-end">
                  <Select
                    label={m.field}
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
                    label={m.direction}
                    data={[
                      { value: 'asc', label: m.ascending },
                      { value: 'desc', label: m.descending },
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
                    title={m.removeSort}
                  >
                    <IconTrash size={16} />
                  </ActionIcon>
                </Group>
              </Paper>
            ))}

            {sortConditions.length === 0 && <Text c="dimmed">{m.noSortConfigured}</Text>}
          </Stack>

          <Group justify="space-between">
            <Button
              variant="subtle"
              color="gray"
              onClick={() => {
                setFilterDrafts([]);
                setSortConditions([]);
              }}
            >
              {m.resetOptions}
            </Button>

            <Group>
              <Button variant="default" onClick={() => setOptionsOpen(false)}>
                {m.cancel}
              </Button>
              <Button onClick={handleApplyOptions}>{m.apply}</Button>
            </Group>
          </Group>
        </Stack>
      </Modal>
    </Box>
  );
}
