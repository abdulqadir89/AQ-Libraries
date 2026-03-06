import type { SortCondition } from '../mantine/data-grid/DataGrid.types';

export class SortExpressionBuilder {
  static buildSortExpression(conditions: SortCondition[]): string {
    if (conditions.length === 0) return '';
    
    // Sort by priority first (lower values = higher priority)
    const sorted = [...conditions].sort((a, b) => (a.priority || 0) - (b.priority || 0));
    
    return sorted
      .map(condition => `${condition.property},${condition.direction}`)
      .join(';');
  }
}
