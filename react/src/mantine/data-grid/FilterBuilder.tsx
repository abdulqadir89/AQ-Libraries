import { useState } from 'react';
import { ActionIcon, Badge, Button, Group, Paper, Select, Stack, Switch, Tabs, Text } from '@mantine/core';
import { IconArrowDown, IconArrowUp, IconPlus, IconTrash } from '@tabler/icons-react';
import { useAQLocale } from '../locale';
import type { AQMessages } from '../locale';
import type { DataGridColumn, FilterCondition, FilterGroup, FilterOperator, SortCondition } from './DataGrid.types';
import { FilterValueInput, isListOperator } from './FilterValueInput';
import { columnLabel, columnPath, columnSortPath, countConditions } from './filterTree';

type AnyColumn = DataGridColumn<any>; // eslint-disable-line @typescript-eslint/no-explicit-any

const NO_VALUE: FilterOperator[] = ['isnull', 'isnotnull'];

export function operatorsFor(column: AnyColumn | undefined): FilterOperator[] {
  if (column?.type === 'lookup' && column.multiValued) return ['in', 'hasall', 'notin', 'eq', 'ne'];

  switch (column?.type) {
    case 'number':
    case 'date':
    case 'daterange':
      return ['eq', 'ne', 'gt', 'gte', 'lt', 'lte', 'between', 'notbetween', 'isnull', 'isnotnull'];
    case 'boolean':
      return ['eq', 'ne'];
    case 'enum':
    case 'lookup':
      return ['in', 'notin', 'eq', 'ne', 'isnull', 'isnotnull'];
    default:
      return ['contains', 'notcontains', 'eq', 'ne', 'startswith', 'endswith', 'isnull', 'isnotnull'];
  }
}

export function operatorLabel(operator: FilterOperator, column: AnyColumn | undefined, messages: AQMessages): string {
  const builder = messages.filterBuilder;
  if (column?.type === 'lookup' && column.multiValued) {
    switch (operator) {
      case 'in': return builder.hasAnyOf;
      case 'hasall': return builder.hasAllOf;
      case 'notin': return builder.hasNoneOf;
      case 'eq': return builder.has;
      case 'ne': return builder.doesNotHave;
    }
  }
  if (operator === 'hasall') return builder.hasAllOf;
  return messages.columnFilter.operators[operator as keyof AQMessages['columnFilter']['operators']] ?? operator;
}

const defaultOperator = (column: AnyColumn | undefined): FilterOperator => operatorsFor(column)[0];

const emptyValueFor = (operator: FilterOperator): unknown => (isListOperator(operator) ? [] : '');

interface GroupEditorProps {
  columns: AnyColumn[];
  group: FilterGroup;
  isRoot: boolean;
  onChange: (group: FilterGroup) => void;
  onRemove?: () => void;
}

function GroupEditor({ columns, group, isRoot, onChange, onRemove }: GroupEditorProps) {
  const { messages } = useAQLocale();
  const m = messages.filterBuilder;
  const subGroups = group.groups ?? [];

  const columnFor = (property: string) => columns.find(c => columnPath(c) === property);

  const addCondition = () => {
    const column = columns[0];
    if (!column) return;
    const operator = defaultOperator(column);
    onChange({ ...group, conditions: [...group.conditions, { property: columnPath(column), operator, value: emptyValueFor(operator) }] });
  };

  const updateCondition = (index: number, next: FilterCondition) =>
    onChange({ ...group, conditions: group.conditions.map((c, i) => (i === index ? next : c)) });

  const removeCondition = (index: number) =>
    onChange({ ...group, conditions: group.conditions.filter((_, i) => i !== index) });

  const addGroup = () =>
    onChange({ ...group, groups: [...subGroups, { conditions: [], groups: [], operator: group.operator === 'and' ? 'or' : 'and' }] });

  const body = (
    <Stack gap="xs">
      <Group justify="space-between" wrap="wrap" gap="xs">
        <Group gap="xs">
          <Select
            size="xs"
            w={130}
            allowDeselect={false}
            data={[{ value: 'and', label: m.matchAll }, { value: 'or', label: m.matchAny }]}
            value={group.operator}
            onChange={value => onChange({ ...group, operator: value === 'or' ? 'or' : 'and' })}
          />
          <Switch size="xs" label={m.negate} checked={group.negated === true} onChange={event => onChange({ ...group, negated: event.currentTarget.checked })} />
        </Group>
        <Group gap="xs">
          <Button size="compact-xs" variant="light" leftSection={<IconPlus size={14} />} onClick={addCondition}>{m.addCondition}</Button>
          <Button size="compact-xs" variant="subtle" leftSection={<IconPlus size={14} />} onClick={addGroup}>{m.addGroup}</Button>
          {onRemove && (
            <ActionIcon size="sm" variant="subtle" color="red" onClick={onRemove} title={m.removeGroup}>
              <IconTrash size={14} />
            </ActionIcon>
          )}
        </Group>
      </Group>

      {group.conditions.map((condition, index) => {
        const column = columnFor(condition.property);
        const operators = operatorsFor(column);
        // Two columns can filter the same property; the select needs each value once.
        const fieldOptions = columns
          .map(c => ({ value: columnPath(c), label: columnLabel(c) }))
          .filter((option, index, all) => all.findIndex(o => o.value === option.value) === index);
        if (!column) fieldOptions.push({ value: condition.property, label: condition.property });

        return (
          <Paper key={index} p="xs" withBorder>
            <Group align="flex-end" gap="xs" wrap="wrap">
              <Select
                size="xs"
                label={m.field}
                w={150}
                allowDeselect={false}
                data={fieldOptions}
                value={condition.property}
                onChange={value => {
                  const next = columnFor(value ?? '');
                  const operator = defaultOperator(next);
                  updateCondition(index, { property: value ?? condition.property, operator, value: emptyValueFor(operator) });
                }}
              />
              <Select
                size="xs"
                label={m.operator}
                w={150}
                allowDeselect={false}
                data={operators.map(op => ({ value: op, label: operatorLabel(op, column, messages) }))}
                value={operators.includes(condition.operator) ? condition.operator : null}
                onChange={value => {
                  const operator = (value ?? condition.operator) as FilterOperator;
                  const keepsShape = isListOperator(operator) === isListOperator(condition.operator);
                  updateCondition(index, { ...condition, operator, value: keepsShape ? condition.value : emptyValueFor(operator) });
                }}
              />
              {!NO_VALUE.includes(condition.operator) && (
                <div style={{ flex: '1 1 180px', minWidth: 160 }}>
                  <FilterValueInput
                    column={column}
                    operator={condition.operator}
                    value={condition.value}
                    secondValue={condition.secondValue}
                    onChange={(value, secondValue) => updateCondition(index, { ...condition, value, secondValue })}
                  />
                </div>
              )}
              <ActionIcon variant="subtle" color="red" onClick={() => removeCondition(index)} title={m.removeCondition}>
                <IconTrash size={16} />
              </ActionIcon>
            </Group>
          </Paper>
        );
      })}

      {subGroups.map((sub, index) => (
        <GroupEditor
          key={`group-${index}`}
          columns={columns}
          group={sub}
          isRoot={false}
          onChange={next => onChange({ ...group, groups: subGroups.map((g, i) => (i === index ? next : g)) })}
          onRemove={() => onChange({ ...group, groups: subGroups.filter((_, i) => i !== index) })}
        />
      ))}

      {isRoot && group.conditions.length === 0 && subGroups.length === 0 && <Text size="sm" c="dimmed">{m.noFilters}</Text>}
    </Stack>
  );

  return isRoot ? body : <Paper p="xs" withBorder bg="var(--mantine-color-default-hover)">{body}</Paper>;
}

interface SortEditorProps {
  columns: AnyColumn[];
  sort: SortCondition[];
  onChange: (sort: SortCondition[]) => void;
}

function SortEditor({ columns, sort, onChange }: SortEditorProps) {
  const { messages } = useAQLocale();
  const m = messages.filterBuilder;
  const options = columns
    .map(c => ({ value: columnSortPath(c), label: columnLabel(c) }))
    .filter((option, index, all) => all.findIndex(o => o.value === option.value) === index);

  const move = (index: number, delta: number) => {
    const next = [...sort];
    const [item] = next.splice(index, 1);
    next.splice(index + delta, 0, item);
    onChange(next.map((s, priority) => ({ ...s, priority })));
  };

  return (
    <Stack gap="xs">
      <Group justify="flex-end">
        <Button
          size="compact-xs"
          variant="light"
          leftSection={<IconPlus size={14} />}
          disabled={options.length === 0}
          onClick={() => onChange([...sort, { property: options[0].value, direction: 'asc', priority: sort.length }])}
        >
          {m.addSort}
        </Button>
      </Group>

      {sort.map((condition, index) => (
        <Paper key={index} p="xs" withBorder>
          <Group align="flex-end" gap="xs" wrap="wrap">
            <Badge variant="light" size="lg">{index + 1}</Badge>
            <Select
              size="xs"
              label={m.field}
              w={180}
              allowDeselect={false}
              data={options}
              value={condition.property}
              onChange={value => onChange(sort.map((s, i) => (i === index ? { ...s, property: value ?? s.property } : s)))}
            />
            <Select
              size="xs"
              w={140}
              allowDeselect={false}
              data={[{ value: 'asc', label: m.ascending }, { value: 'desc', label: m.descending }]}
              value={condition.direction}
              onChange={value => onChange(sort.map((s, i) => (i === index ? { ...s, direction: value === 'desc' ? 'desc' : 'asc' } : s)))}
            />
            <ActionIcon variant="subtle" disabled={index === 0} onClick={() => move(index, -1)} title={m.moveUp}>
              <IconArrowUp size={16} />
            </ActionIcon>
            <ActionIcon variant="subtle" disabled={index === sort.length - 1} onClick={() => move(index, 1)} title={m.moveDown}>
              <IconArrowDown size={16} />
            </ActionIcon>
            <ActionIcon variant="subtle" color="red" onClick={() => onChange(sort.filter((_, i) => i !== index).map((s, priority) => ({ ...s, priority })))} title={m.removeSort}>
              <IconTrash size={16} />
            </ActionIcon>
          </Group>
        </Paper>
      ))}

      {sort.length === 0 && <Text size="sm" c="dimmed">{m.noSort}</Text>}
    </Stack>
  );
}

export interface FilterBuilderPanelProps {
  columns: AnyColumn[];
  filter: FilterGroup;
  sort: SortCondition[];
  sortable: boolean;
  initialTab?: 'filter' | 'sort';
  onApply: (filter: FilterGroup, sort: SortCondition[]) => void;
  onClose: () => void;
}

// The builder edits a draft; nothing reaches the grid until Apply.
export function FilterBuilderPanel({ columns, filter, sort, sortable, initialTab = 'filter', onApply, onClose }: FilterBuilderPanelProps) {
  const { messages } = useAQLocale();
  const m = messages.filterBuilder;
  const [draftFilter, setDraftFilter] = useState<FilterGroup>(filter);
  const [draftSort, setDraftSort] = useState<SortCondition[]>(sort);
  const [tab, setTab] = useState<string>(initialTab);

  const filterColumns = columns.filter(c => c.filterable !== false);
  const sortColumns = columns.filter(c => c.sortable);

  const filterCount = countConditions(draftFilter);

  return (
    <Stack gap="md">
      <Tabs value={tab} onChange={value => setTab(value ?? 'filter')}>
        <Tabs.List>
          <Tabs.Tab value="filter" rightSection={filterCount > 0 ? <Badge size="xs" circle>{filterCount}</Badge> : null}>{m.filters}</Tabs.Tab>
          {sortable && <Tabs.Tab value="sort" rightSection={draftSort.length > 0 ? <Badge size="xs" circle>{draftSort.length}</Badge> : null}>{m.sort}</Tabs.Tab>}
        </Tabs.List>
        <Tabs.Panel value="filter" pt="sm">
          <GroupEditor columns={filterColumns} group={draftFilter} isRoot onChange={setDraftFilter} />
        </Tabs.Panel>
        {sortable && (
          <Tabs.Panel value="sort" pt="sm">
            <SortEditor columns={sortColumns} sort={draftSort} onChange={setDraftSort} />
          </Tabs.Panel>
        )}
      </Tabs>

      <Group justify="space-between">
        <Button variant="light" color="gray" onClick={() => { setDraftFilter({ conditions: [], groups: [], operator: 'and' }); setDraftSort([]); }}>
          {m.clearAll}
        </Button>
        <Group>
          <Button variant="default" onClick={onClose}>{m.cancel}</Button>
          <Button onClick={() => onApply(draftFilter, draftSort)}>{m.apply}</Button>
        </Group>
      </Group>
    </Stack>
  );
}
