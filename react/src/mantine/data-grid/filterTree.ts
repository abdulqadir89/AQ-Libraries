import type { DataGridColumn, FilterCondition, FilterGroup } from './DataGrid.types';
import { FilterExpressionBuilder } from '../../utils/FilterExpressionBuilder';
import { FilterExpressionParser } from '../../utils/FilterExpressionParser';

type ColumnLike = Pick<DataGridColumn, 'key' | 'filterPath'>;

export const columnPath = (column: ColumnLike): string => column.filterPath ?? column.key;

export const emptyGroup = (): FilterGroup => ({ conditions: [], groups: [], operator: 'and' });

export const parseFilter = (expression: string): FilterGroup => FilterExpressionParser.parse(expression);

export const serializeFilter = (root: FilterGroup): string => FilterExpressionBuilder.buildComplexFilterExpression(root);

const groupsOf = (group: FilterGroup): FilterGroup[] => group.groups ?? [];

export function countConditions(group: FilterGroup): number {
  return group.conditions.length + groupsOf(group).reduce((sum, sub) => sum + countConditions(sub), 0);
}

function usesPath(group: FilterGroup, path: string): boolean {
  return group.conditions.some(c => c.property === path) || groupsOf(group).some(sub => usesPath(sub, path));
}

// A bracketed run of 'eq' conditions on one path: "has any of" for a list column.
export function isAnyOfGroup(group: FilterGroup, path: string): boolean {
  return group.operator === 'or'
    && !group.negated
    && groupsOf(group).length === 0
    && group.conditions.length > 0
    && group.conditions.every(c => c.property === path && c.operator === 'eq');
}

export interface ColumnSlice {
  conditions: FilterCondition[];
  groups: FilterGroup[];
  // The column's conditions cannot be shown by its popover (several on one column, inside an OR or a negated group).
  advanced: boolean;
}

// RULE RCT-6
export function sliceForColumn(root: FilterGroup, path: string, multiValued: boolean): ColumnSlice {
  if (root.operator !== 'and' || root.negated) {
    return { conditions: [], groups: [], advanced: usesPath(root, path) };
  }

  const conditions = root.conditions.filter(c => c.property === path);
  const groups = groupsOf(root).filter(g => isAnyOfGroup(g, path));
  const usedElsewhere = groupsOf(root).some(g => !groups.includes(g) && usesPath(g, path));

  let representable: boolean;
  if (multiValued) {
    const isList = (op: string) => conditions.length === 1 && conditions[0].operator === op;
    representable = groups.length === 0
      ? conditions.length === 0 || isList('in') || isList('notin')
        || conditions.every(c => c.operator === 'eq') || conditions.every(c => c.operator === 'ne')
      : groups.length === 1 && conditions.length === 0;
  } else {
    representable = groups.length === 0 && conditions.length <= 1;
  }

  return { conditions, groups, advanced: usedElsewhere || !representable };
}

// Replaces the column's own conditions with the ones the popover produced (null clears them).
export function applySlice(root: FilterGroup, path: string, replacement: FilterGroup | null): FilterGroup {
  const base: FilterGroup = root.operator === 'and' && !root.negated
    ? { ...root, conditions: [...root.conditions], groups: [...groupsOf(root)] }
    : { conditions: [], groups: [root], operator: 'and' };

  base.conditions = base.conditions.filter(c => c.property !== path);
  base.groups = groupsOf(base).filter(g => !isAnyOfGroup(g, path));

  if (replacement) {
    if (replacement.operator === 'and' && !replacement.negated) {
      base.conditions.push(...replacement.conditions);
      base.groups.push(...groupsOf(replacement));
    } else {
      base.groups.push(replacement);
    }
  }

  return base;
}

export function sliceAsGroup(slice: ColumnSlice): FilterGroup {
  return { conditions: slice.conditions, groups: slice.groups, operator: 'and' };
}

// --- The single-condition string the column popover reads and writes: `prop,op,value` with commas as %2C. ---

const encodeComma = (value: unknown): string => String(value ?? '').replace(/,/g, '%2C');
const decodeComma = (value: string): string => value.replace(/%2C/g, ',');

export function conditionToLegacy(condition: FilterCondition): string {
  const { property, operator, value, secondValue } = condition;
  if (operator === 'isnull' || operator === 'isnotnull') return `${property},${operator},`;
  if (operator === 'between' || operator === 'notbetween') return `${property},${operator},${encodeComma(value)},${encodeComma(secondValue)}`;
  const values = Array.isArray(value) ? value : [value];
  return `${property},${operator},${values.map(encodeComma).join(',')}`;
}

export function legacyToCondition(expression: string): FilterCondition | null {
  const parts = expression.split(',');
  if (parts.length < 2) return null;

  const property = parts[0];
  const operator = parts[1] as FilterCondition['operator'];
  if (operator === 'isnull' || operator === 'isnotnull') return { property, operator, value: '' };
  if (operator === 'between' || operator === 'notbetween') {
    return { property, operator, value: decodeComma(parts[2] ?? ''), secondValue: decodeComma(parts[3] ?? '') };
  }
  if (operator === 'in' || operator === 'notin') {
    return { property, operator, value: parts.slice(2).map(decodeComma) };
  }
  return { property, operator, value: decodeComma(parts.slice(2).join(',')) };
}

// TODO-003: per-column legacy filters (initial state in, snapshot out); remove with the legacy callbacks.
export function expressionFromLegacy(columnFilters: Record<string, string>): string {
  const conditions = Object.values(columnFilters)
    .map(legacyToCondition)
    .filter((c): c is FilterCondition => c !== null);
  return serializeFilter({ conditions, groups: [], operator: 'and' });
}

// TODO-003: see expressionFromLegacy.
export function legacyColumnFilters(root: FilterGroup, columns: DataGridColumn[]): Record<string, string> {
  const result: Record<string, string> = {};
  columns.forEach(column => {
    const slice = sliceForColumn(root, columnPath(column), column.multiValued === true);
    if (slice.advanced) return;

    if (column.multiValued) {
      const text = serializeFilter(sliceAsGroup(slice));
      if (text) result[column.key] = text;
    } else if (slice.conditions.length === 1) {
      result[column.key] = conditionToLegacy(slice.conditions[0]);
    }
  });
  return result;
}

// Drops what the builder left unfinished: conditions without a value and groups with nothing in them.
export function pruneFilter(group: FilterGroup): FilterGroup {
  const hasValue = (c: FilterCondition): boolean => {
    if (c.operator === 'isnull' || c.operator === 'isnotnull') return true;
    const filled = (v: unknown) => (Array.isArray(v) ? v.length > 0 : String(v ?? '').trim() !== '');
    return c.operator === 'between' || c.operator === 'notbetween' ? filled(c.value) && filled(c.secondValue) : filled(c.value);
  };

  const groups = groupsOf(group).map(pruneFilter).filter(g => g.conditions.length > 0 || groupsOf(g).length > 0);
  return { ...group, conditions: group.conditions.filter(hasValue), groups };
}

// TODO-003: the flat condition list the legacy onFilterChange callback receives (list values joined by commas).
export function legacyConditions(root: FilterGroup): FilterCondition[] {
  if (root.operator !== 'and' || root.negated) return [];
  return root.conditions.map(c => ({ ...c, value: Array.isArray(c.value) ? c.value.join(',') : c.value }));
}

// A filter written in the URL or by an older grid may still be the single `property,operator,value` form with commas as %2C;
// anything bracketed, quoted or combined is already a full expression.
// TODO-003: drop the single-condition form with the legacy callbacks.
export function normalizeFilterText(text: string): string {
  if (/[()"']|&&|\|\|/.test(text)) return text;
  const condition = legacyToCondition(text);
  return condition ? serializeFilter({ conditions: [condition], groups: [], operator: 'and' }) : text;
}
