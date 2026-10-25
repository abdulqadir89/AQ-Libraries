import type { FilterCondition, FilterGroup, FilterOperator } from '../mantine/data-grid/DataGrid.types';

const OPERATORS: ReadonlySet<string> = new Set<FilterOperator>([
  'eq', 'ne', 'gt', 'gte', 'lt', 'lte',
  'contains', 'notcontains', 'startswith', 'endswith',
  'isnull', 'isnotnull', 'in', 'notin', 'between', 'notbetween',
]);

type Token = { kind: 'open' | 'close' | 'and' | 'or' | 'not' } | { kind: 'condition'; text: string };

// A quote opens a quoted value only straight after a comma, so an apostrophe inside a word (O'Brien) stays plain text.
function tokenize(expression: string): Token[] {
  const tokens: Token[] = [];
  let text = '';
  let quote = '';

  const flush = () => {
    const trimmed = text.trim();
    if (trimmed) tokens.push({ kind: 'condition', text: trimmed });
    text = '';
  };

  const nextNonSpace = (from: number): string => {
    for (let i = from; i < expression.length; i++) {
      if (!/\s/.test(expression[i])) return expression[i];
    }
    return '';
  };

  for (let i = 0; i < expression.length; i++) {
    const c = expression[i];

    if (quote) {
      text += c;
      if (c === quote) quote = '';
      continue;
    }

    if ((c === '"' || c === "'") && text.endsWith(',')) {
      quote = c;
      text += c;
    } else if (c === '(' || c === ')') {
      flush();
      tokens.push({ kind: c === '(' ? 'open' : 'close' });
    } else if (c === '&' && expression[i + 1] === '&') {
      flush();
      tokens.push({ kind: 'and' });
      i++;
    } else if (c === '|' && expression[i + 1] === '|') {
      flush();
      tokens.push({ kind: 'or' });
      i++;
    } else if (c === '!' && text.trim() === '' && (nextNonSpace(i + 1) === '(' || nextNonSpace(i + 1) === '!')) {
      tokens.push({ kind: 'not' });
    } else {
      text += c;
    }
  }

  flush();
  return tokens;
}

function splitFields(text: string): string[] {
  const fields: string[] = [];
  let current = '';
  let quote = '';

  for (let i = 0; i < text.length; i++) {
    const c = text[i];
    if (quote) {
      current += c;
      if (c === quote) quote = '';
    } else if ((c === '"' || c === "'") && (current === '' || current.endsWith(','))) {
      quote = c;
      current += c;
    } else if (c === ',') {
      fields.push(current);
      current = '';
    } else {
      current += c;
    }
  }

  fields.push(current);
  return fields;
}

function unquote(value: string): string {
  const trimmed = value.trim();
  if (trimmed.length >= 2 && (trimmed[0] === '"' || trimmed[0] === "'") && trimmed[trimmed.length - 1] === trimmed[0]) {
    return trimmed.slice(1, -1);
  }
  return trimmed;
}

function parseCondition(text: string): FilterCondition | null {
  const fields = splitFields(text);
  if (fields.length < 2) return null;

  const property = fields[0].trim();
  const operator = fields[1].trim().toLowerCase();
  if (!property || !OPERATORS.has(operator)) return null;

  const values = fields.slice(2).map(unquote);

  if (operator === 'isnull' || operator === 'isnotnull') {
    return { property, operator, value: '' };
  }
  if (values.length === 0) return null;
  if (operator === 'in' || operator === 'notin') {
    return { property, operator, value: values };
  }
  if (operator === 'between' || operator === 'notbetween') {
    if (values.length < 2) return null;
    return { property, operator, value: values[0], secondValue: values[1] };
  }
  return { property, operator: operator as FilterOperator, value: values[0] };
}

type Node = { condition: FilterCondition; group?: undefined } | { group: FilterGroup; condition?: undefined };

function addTo(group: FilterGroup, node: Node) {
  if (node.condition) group.conditions.push(node.condition);
  else (group.groups ??= []).push(node.group);
}

// Each bracket and ! recurses once, so the depth is capped well below what the stack can take.
const MAX_DEPTH = 32;

class Reader {
  private position = 0;
  private depth = 0;

  constructor(private readonly tokens: Token[]) {}

  private peek(): Token | undefined {
    return this.tokens[this.position];
  }

  readRoot(): FilterGroup {
    const node = this.readOr();
    if (!node) return { conditions: [], groups: [], operator: 'and' };
    if (node.condition) return { conditions: [node.condition], groups: [], operator: 'and' };
    return node.group.negated ? { conditions: [], groups: [node.group], operator: 'and' } : node.group;
  }

  private readOr(): Node | null {
    return this.readChain(() => this.readAnd(), 'or', 'or');
  }

  private readAnd(): Node | null {
    return this.readChain(() => this.readUnary(), 'and', 'and');
  }

  private readChain(readOperand: () => Node | null, separator: 'and' | 'or', operator: 'and' | 'or'): Node | null {
    const operands: Node[] = [];
    const first = readOperand();
    if (first) operands.push(first);

    while (this.peek()?.kind === separator) {
      this.position++;
      const next = readOperand();
      if (next) operands.push(next);
    }

    if (operands.length === 0) return null;
    if (operands.length === 1) return operands[0];

    const group: FilterGroup = { conditions: [], groups: [], operator };
    operands.forEach(operand => addTo(group, operand));
    return { group };
  }

  private readUnary(): Node | null {
    const token = this.peek();
    if (!token) return null;

    if (token.kind === 'not' || token.kind === 'open') {
      if (++this.depth > MAX_DEPTH) throw new Error(`A filter expression cannot nest deeper than ${MAX_DEPTH} levels.`);
      try {
        return this.readNested(token);
      } finally {
        this.depth--;
      }
    }

    return this.readNested(token);
  }

  private readNested(token: Token): Node | null {
    if (token.kind === 'not') {
      this.position++;
      const operand = this.readUnary();
      if (!operand) return null;
      const negated: FilterGroup = { conditions: [], groups: [], operator: 'and', negated: true };
      addTo(negated, operand);
      return { group: negated };
    }

    if (token.kind === 'open') {
      this.position++;
      const inner = this.readOr();
      if (this.peek()?.kind === 'close') this.position++;
      return inner;
    }

    if (token.kind !== 'condition') return null;

    this.position++;
    const condition = parseCondition(token.text);
    return condition ? { condition } : null;
  }
}

export class FilterExpressionParser {
  // Mirrors the server grammar (FLT-3, FLT-8): && binds tighter than ||, a leading ! negates a group, a part that
  // is not a valid condition is dropped.
  // RULE RCT-7
  static parse(expression: string): FilterGroup {
    try {
      return new Reader(tokenize(expression ?? '')).readRoot();
    } catch {
      // Too deeply nested: the server refuses it too, so show no filter rather than crash the page.
      return { conditions: [], groups: [], operator: 'and' };
    }
  }
}
