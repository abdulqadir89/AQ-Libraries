import { Group, Pill } from '@mantine/core';
import { useAQLocale } from '../locale';
import type { AQMessages } from '../locale';
import type { DataGridColumn, FilterCondition, FilterGroup } from './DataGrid.types';
import { operatorLabel } from './FilterBuilder';
import { columnLabel, columnPath, emptyGroup, isAnyOfGroup } from './filterTree';

type AnyColumn = DataGridColumn<any>; // eslint-disable-line @typescript-eslint/no-explicit-any

interface Chip {
  key: string;
  label: string;
  remove: () => FilterGroup;
}

function describeValue(condition: FilterCondition, column: AnyColumn | undefined, messages: AQMessages): string {
  const values = Array.isArray(condition.value) ? condition.value.map(String) : [String(condition.value ?? '')];

  if (condition.operator === 'between' || condition.operator === 'notbetween') {
    return `${values[0]} – ${String(condition.secondValue ?? '')}`;
  }
  if (column?.type === 'lookup') {
    return values.length > 1 ? messages.filterBuilder.selected(values.length) : values.length === 1 && values[0] ? messages.filterBuilder.selected(1) : '';
  }
  if (column?.type === 'enum') {
    return values.map(v => column.enumOptions?.find(o => String(o.value) === v)?.label ?? v).join(', ');
  }
  return values.join(', ');
}

function describeCondition(condition: FilterCondition, columns: AnyColumn[], messages: AQMessages): string {
  const column = columns.find(c => columnPath(c) === condition.property);
  const title = column ? columnLabel(column) : condition.property;
  const operator = operatorLabel(condition.operator, column, messages);
  const value = condition.operator === 'isnull' || condition.operator === 'isnotnull' ? '' : describeValue(condition, column, messages);
  return [title, operator, value].filter(Boolean).join(' ');
}

function describeGroup(group: FilterGroup, columns: AnyColumn[], messages: AQMessages): string {
  const m = messages.filterBuilder;
  const anyOfColumn = columns.find(c => c.multiValued && isAnyOfGroup(group, columnPath(c)));
  let text: string;

  if (anyOfColumn) {
    text = `${columnLabel(anyOfColumn)} ${m.hasAnyOf} ${m.selected(group.conditions.length)}`;
  } else {
    const parts = [
      ...group.conditions.map(c => describeCondition(c, columns, messages)),
      ...(group.groups ?? []).map(g => `(${describeGroup(g, columns, messages)})`),
    ];
    text = parts.join(group.operator === 'and' ? ` ${m.and} ` : ` ${m.or} `);
  }

  return group.negated ? `${m.not} (${text})` : text;
}

export interface FilterChipsProps {
  columns: AnyColumn[];
  filter: FilterGroup;
  onChange: (filter: FilterGroup) => void;
  onOpen: () => void;
}

export function FilterChips({ columns, filter, onChange, onOpen }: FilterChipsProps) {
  const { messages } = useAQLocale();
  const m = messages.filterBuilder;
  const groups = filter.groups ?? [];

  // Several conditions on one list column (has all of / has none of) read as one chip.
  const listChips: Chip[] = [];
  const listConditions = new Set<FilterCondition>();
  if (filter.operator === 'and' && !filter.negated) {
    columns.filter(c => c.multiValued).forEach(column => {
      const path = columnPath(column);
      for (const operator of ['eq', 'ne'] as const) {
        const matching = filter.conditions.filter(c => c.property === path && c.operator === operator);
        if (matching.length < 2) continue;
        matching.forEach(c => listConditions.add(c));
        listChips.push({
          key: `l-${path}-${operator}`,
          label: `${columnLabel(column)} ${operator === 'eq' ? m.hasAllOf : m.hasNoneOf} ${m.selected(matching.length)}`,
          remove: () => ({ ...filter, conditions: filter.conditions.filter(c => !matching.includes(c)) }),
        });
      }
    });
  }

  const chips: Chip[] = filter.operator === 'and' && !filter.negated
    ? [
        ...listChips,
        ...filter.conditions.filter(condition => !listConditions.has(condition)).map(condition => ({
          key: `c${filter.conditions.indexOf(condition)}`,
          label: describeCondition(condition, columns, messages),
          remove: () => ({ ...filter, conditions: filter.conditions.filter(c => c !== condition) }),
        })),
        ...groups.map((group, index) => ({
          key: `g${index}`,
          label: describeGroup(group, columns, messages),
          remove: () => ({ ...filter, groups: groups.filter((_, i) => i !== index) }),
        })),
      ]
    : [{ key: 'root', label: describeGroup(filter, columns, messages), remove: emptyGroup }];

  if (chips.length === 0) return null;

  return (
    <Group gap="xs">
      {chips.map(chip => (
        <Pill
          key={chip.key}
          withRemoveButton
          removeButtonProps={{ 'aria-label': m.removeChip }}
          onRemove={() => onChange(chip.remove())}
          onClick={onOpen}
          style={{ cursor: 'pointer' }}
        >
          {chip.label}
        </Pill>
      ))}
    </Group>
  );
}
