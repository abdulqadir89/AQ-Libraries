import type { ReactNode } from 'react';
import { MultiSelect, NumberInput, Select, TextInput } from '@mantine/core';
import { DateInput } from '@mantine/dates';
import { useAQLocale } from '../locale';
import type { DataGridColumn, FilterOperator } from './DataGrid.types';

export interface FilterValueInputProps {
  column?: DataGridColumn<any>; // eslint-disable-line @typescript-eslint/no-explicit-any
  operator: FilterOperator;
  value: unknown;
  secondValue?: unknown;
  onChange: (value: unknown, secondValue?: unknown) => void;
}

export const isListOperator = (operator: FilterOperator): boolean =>
  operator === 'in' || operator === 'notin' || operator === 'hasall';

const asText = (value: unknown): string => (Array.isArray(value) ? value.join(',') : String(value ?? ''));
const asList = (value: unknown): string[] => (Array.isArray(value) ? value.map(String) : value ? [String(value)] : []);

const toDateText = (value: Date | string | null): string => {
  if (!value) return '';
  return typeof value === 'string' ? value.slice(0, 10) : value.toISOString().split('T')[0];
};

// The one value editor behind the filter builder; the column type and the operator decide what is shown.
export function FilterValueInput({ column, operator, value, secondValue, onChange }: FilterValueInputProps) {
  const { messages } = useAQLocale();
  const m = messages.columnFilter;

  if (operator === 'isnull' || operator === 'isnotnull') return null;

  const single = (input: (current: string, set: (next: string) => void) => ReactNode) =>
    input(asText(value), next => onChange(next, secondValue));

  switch (column?.type) {
    case 'lookup': {
      if (!column.lookupConfig) return null;
      const list = isListOperator(operator);
      return column.lookupConfig.renderSelect({
        value: asList(value),
        onChange: ids => onChange(list ? ids : ids[ids.length - 1] ?? '', secondValue),
      });
    }

    case 'enum': {
      const options = (column.enumOptions ?? []).map(o => ({ value: String(o.value), label: o.label }));
      return isListOperator(operator) ? (
        <MultiSelect
          size="xs"
          label={m.value}
          data={options}
          value={asList(value)}
          onChange={next => onChange(next, secondValue)}
          placeholder={m.selectValues}
          searchable
        />
      ) : (
        <Select
          size="xs"
          label={m.value}
          data={options}
          value={asText(value) || null}
          onChange={next => onChange(next ?? '', secondValue)}
          placeholder={m.selectValue}
          searchable
        />
      );
    }

    case 'boolean':
      return (
        <Select
          size="xs"
          label={m.value}
          data={[{ value: 'true', label: m.true }, { value: 'false', label: m.false }]}
          value={asText(value) || null}
          onChange={next => onChange(next ?? '', secondValue)}
          allowDeselect={false}
        />
      );

    case 'number':
      return (
        <>
          <NumberInput
            size="xs"
            label={operator === 'between' || operator === 'notbetween' ? m.from : m.value}
            value={asText(value) === '' ? '' : Number(asText(value))}
            onChange={next => onChange(next === '' ? '' : String(next), secondValue)}
            placeholder={m.enterNumber}
          />
          {(operator === 'between' || operator === 'notbetween') && (
            <NumberInput
              size="xs"
              label={m.to}
              value={asText(secondValue) === '' ? '' : Number(asText(secondValue))}
              onChange={next => onChange(value, next === '' ? '' : String(next))}
              placeholder={m.enterNumber}
            />
          )}
        </>
      );

    case 'date':
    case 'daterange':
      return (
        <>
          <DateInput
            size="xs"
            label={operator === 'between' || operator === 'notbetween' ? m.from : m.value}
            value={asText(value) ? new Date(asText(value)) : null}
            onChange={next => onChange(toDateText(next as Date | string | null), secondValue)}
            placeholder={m.selectDate}
          />
          {(operator === 'between' || operator === 'notbetween') && (
            <DateInput
              size="xs"
              label={m.to}
              value={asText(secondValue) ? new Date(asText(secondValue)) : null}
              onChange={next => onChange(value, toDateText(next as Date | string | null))}
              placeholder={m.selectEndDate}
            />
          )}
        </>
      );

    default:
      return (
        <>
          {single((current, set) => (
            <TextInput
              size="xs"
              label={operator === 'between' || operator === 'notbetween' ? m.from : m.value}
              value={current}
              onChange={event => set(event.currentTarget.value)}
              placeholder={m.enterText}
            />
          ))}
          {(operator === 'between' || operator === 'notbetween') && (
            <TextInput
              size="xs"
              label={m.to}
              value={asText(secondValue)}
              onChange={event => onChange(value, event.currentTarget.value)}
              placeholder={m.enterText}
            />
          )}
        </>
      );
  }
}
