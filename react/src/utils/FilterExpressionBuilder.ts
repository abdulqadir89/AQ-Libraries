import type { FilterCondition, LogicalOperator, FilterGroup } from '../mantine/data-grid/DataGrid.types';

export class FilterExpressionBuilder {
  // A value is quoted when it holds a character the grammar reads as structure; the quote cannot also occur inside it.
  static quoteValue(value: string): string {
    if (!/[,()&|"]|^\s|\s$|^'/.test(value)) return value;
    return value.includes('"') ? `'${value.replace(/'/g, '"')}'` : `"${value}"`;
  }

  static serializeCondition(condition: FilterCondition): string {
    const { property, operator: op, value, secondValue } = condition;
    const quote = (v: unknown) => FilterExpressionBuilder.quoteValue(String(v ?? ''));

    if (op === 'between' || op === 'notbetween') {
      return `${property},${op},${quote(value)},${quote(secondValue)}`;
    }
    if (op === 'isnull' || op === 'isnotnull') {
      return `${property},${op}`;
    }
    if (op === 'in' || op === 'notin') {
      const values = Array.isArray(value) ? value : [value];
      return `${property},${op},${values.map(quote).join(',')}`;
    }
    return `${property},${op},${quote(value)}`;
  }

  // RULE RCT-4
  static buildFilterExpression(conditions: FilterCondition[], operator: LogicalOperator = 'and'): string {
    if (conditions.length === 0) return '';

    return conditions
      .map(condition => FilterExpressionBuilder.serializeCondition(condition))
      .join(` ${operator === 'and' ? '&&' : '||'} `);
  }

  // Nested groups are always bracketed, so the result reads the same under the server's precedence (&& before ||).
  static buildComplexFilterExpression(group: FilterGroup): string {
    return FilterExpressionBuilder.serializeGroup(group, true);
  }

  private static serializeGroup(group: FilterGroup, isRoot: boolean): string {
    const parts = [
      ...group.conditions.flatMap(condition => FilterExpressionBuilder.serializeBuilderCondition(condition, group.operator)),
      ...(group.groups ?? []).map(sub => FilterExpressionBuilder.serializeGroup(sub, false)),
    ].filter(Boolean);

    if (parts.length === 0) return '';

    const joined = parts.join(group.operator === 'and' ? ' && ' : ' || ');
    if (group.negated) return `!(${joined})`;
    return !isRoot && parts.length > 1 ? `(${joined})` : joined;
  }

  // 'hasall' is a builder shorthand for one 'eq' per value; inside an 'or' group it is bracketed to keep its meaning.
  private static serializeBuilderCondition(condition: FilterCondition, groupOperator: LogicalOperator): string[] {
    if (condition.operator !== 'hasall') return [FilterExpressionBuilder.serializeCondition(condition)];

    const values = Array.isArray(condition.value) ? condition.value : [condition.value];
    const parts = values.map(value => FilterExpressionBuilder.serializeCondition({ property: condition.property, operator: 'eq', value }));
    return groupOperator === 'or' && parts.length > 1 ? [`(${parts.join(' && ')})`] : parts;
  }
}
