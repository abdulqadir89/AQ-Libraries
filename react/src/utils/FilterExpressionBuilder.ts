import type { FilterCondition, LogicalOperator, FilterGroup } from '../mantine/data-grid/DataGrid.types';

export class FilterExpressionBuilder {
  static buildFilterExpression(conditions: FilterCondition[], operator: LogicalOperator = 'and'): string {
    if (conditions.length === 0) return '';
    
    const expressions = conditions.map(condition => {
      const { property, operator: op, value, secondValue } = condition;
      
      if (op === 'between' || op === 'notbetween') {
        return `${property},${op},${value},${secondValue}`;
      } else if (op === 'in' || op === 'notin') {
        const values = Array.isArray(value) ? value.join(',') : value;
        return `${property},${op},${values}`;
      } else if (op === 'isnull' || op === 'isnotnull') {
        return `${property},${op}`;
      } else {
        return `${property},${op},${value}`;
      }
    });
    
    return expressions.join(` ${operator === 'and' ? '&&' : '||'} `);
  }
  
  static buildComplexFilterExpression(group: FilterGroup): string {
    const conditions = group.conditions.map(condition => {
      const { property, operator: op, value, secondValue } = condition;
      
      if (op === 'between' || op === 'notbetween') {
        return `${property},${op},${value},${secondValue}`;
      } else if (op === 'in' || op === 'notin') {
        const values = Array.isArray(value) ? value.join(',') : value;
        return `${property},${op},${values}`;
      } else if (op === 'isnull' || op === 'isnotnull') {
        return `${property},${op}`;
      } else {
        return `${property},${op},${value}`;
      }
    });
    
    const subGroups = group.groups?.map(subGroup => 
      `(${this.buildComplexFilterExpression(subGroup)})`
    ) || [];
    
    const allExpressions = [...conditions, ...subGroups];
    const joinOperator = group.operator === 'and' ? ' && ' : ' || ';
    
    return allExpressions.join(joinOperator);
  }
}
