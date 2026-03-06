import { useState, useImperativeHandle, forwardRef } from 'react';
import {
  Popover,
  ActionIcon,
  Stack,
  Select,
  TextInput,
  NumberInput,
  Switch,
  Button,
  Group,
  Text,
  Divider,
  FocusTrap,
  Checkbox,
} from '@mantine/core';
import { DateInput } from '@mantine/dates';
import { IconFilter, IconX } from '@tabler/icons-react';
import type { DataGridColumn } from './DataGrid.types';

export interface ColumnFilterProps<T = Record<string, unknown>> {
  column: DataGridColumn<T>;
  onApplyFilter: (filterExpression: string) => void;
  onClearFilter: (columnKey: string) => void;
  onFilterOpen?: (columnKey: string) => void;
}

export interface ColumnFilterRef {
  reset: () => void;
  setFilterExpression: (expression: string) => void;
  close: () => void;
  isOpen: () => boolean;
}

const STRING_OPERATORS = [
  { value: 'contains', label: 'Contains' },
  { value: 'eq', label: 'Equals' },
  { value: 'ne', label: 'Not Equals' },
  { value: 'startswith', label: 'Starts With' },
  { value: 'endswith', label: 'Ends With' },
  { value: 'isnull', label: 'Is Null' },
  { value: 'isnotnull', label: 'Is Not Null' },
];

const NUMBER_OPERATORS = [
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

const DATE_OPERATORS = [
  { value: 'eq', label: 'Equals' },
  { value: 'ne', label: 'Not Equals' },
  { value: 'gt', label: 'After' },
  { value: 'gte', label: 'On or After' },
  { value: 'lt', label: 'Before' },
  { value: 'lte', label: 'On or Before' },
  { value: 'between', label: 'Between' },
  { value: 'isnull', label: 'Is Null' },
  { value: 'isnotnull', label: 'Is Not Null' },
];

const BOOLEAN_OPERATORS = [
  { value: 'eq', label: 'Equals' },
  { value: 'ne', label: 'Not Equals' },
  { value: 'isnull', label: 'Is Null' },
  { value: 'isnotnull', label: 'Is Not Null' },
];

const ENUM_OPERATORS = [
  { value: 'in', label: 'Is Any Of' },
  { value: 'notin', label: 'Is Not Any Of' },
  { value: 'eq', label: 'Equals' },
  { value: 'ne', label: 'Not Equals' },
  { value: 'isnull', label: 'Is Null' },
  { value: 'isnotnull', label: 'Is Not Null' },
];

function getDefaultOperator(type?: string): string {
  switch (type) {
    case 'string':
      return 'contains';
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

function parseFilterExpression(expression: string): { operator: string; value: string; secondValue?: string } {
  if (!expression) {
    return { operator: '', value: '' };
  }
  
  const parts = expression.split(',');
  if (parts.length < 3) {
    return { operator: '', value: '' };
  }
  
  const operator = parts[1];
  
  // For between: parts[2] and parts[3] are separate encoded values
  if (operator === 'between' && parts.length >= 4) {
    return { 
      operator, 
      value: parts[2]?.replace(/%2C/g, ',') || '', 
      secondValue: parts[3]?.replace(/%2C/g, ',') || '' 
    };
  }
  
  // For other operators: rejoin remaining parts and decode
  const value = parts.slice(2).join(',').replace(/%2C/g, ',');
  return { operator, value };
}

export const ColumnFilter = forwardRef<ColumnFilterRef, ColumnFilterProps>(
  ({ column, onApplyFilter, onClearFilter, onFilterOpen }, ref) => {
    const [opened, setOpened] = useState(false);
    const [hasActiveFilter, setHasActiveFilter] = useState(false);
    
    // Local state for form inputs - store as strings but convert for display
    const [operator, setOperator] = useState(getDefaultOperator(column.type));
    const [value, setValue] = useState('');
    const [secondValue, setSecondValue] = useState('');
    // For enum filters with multiple selection
    const [selectedEnumValues, setSelectedEnumValues] = useState<string[]>([]);

    // Helper functions to convert between string and typed values
    const parseValue = (val: string, type?: string): string | number | Date | boolean => {
      if (!val) return '';
      
      switch (type) {
        case 'number': {
          const num = parseFloat(val);
          return isNaN(num) ? 0 : num;
        }
        case 'date':
          return val ? new Date(val) : new Date();
        case 'boolean':
          return val === 'true';
        default:
          return val;
      }
    };

    const formatValueToString = (val: string | number | Date | boolean): string => {
      if (val === null || val === undefined) return '';
      if (val instanceof Date) return val.toISOString().split('T')[0];
      if (typeof val === 'boolean') return val.toString();
      return String(val);
    };

    // Expose methods to parent
    useImperativeHandle(ref, () => ({
      reset: () => {
        setOperator(getDefaultOperator(column.type));
        setValue('');
        setSecondValue('');
        setSelectedEnumValues([]);
        setHasActiveFilter(false);
      },
      setFilterExpression: (expression: string) => {
        if (!expression) {
          setOperator(getDefaultOperator(column.type));
          setValue('');
          setSecondValue('');
          setSelectedEnumValues([]);
          setHasActiveFilter(false);
          return;
        }
        
        const parsed = parseFilterExpression(expression);
        setOperator(parsed.operator || getDefaultOperator(column.type));
        setValue(parsed.value || '');
        setSecondValue(parsed.secondValue || '');
        
        // Handle enum 'in' and 'notin' operators
        if (column.type === 'enum' && (parsed.operator === 'in' || parsed.operator === 'notin')) {
          const enumValues = parsed.value ? parsed.value.split(',').map(v => v.trim()) : [];
          setSelectedEnumValues(enumValues);
        } else {
          setSelectedEnumValues([]);
        }
        
        setHasActiveFilter(true);
      },
      close: () => {
        setOpened(false);
      },
      isOpen: () => {
        return opened;
      }
    }));

    function getOperators(type?: string) {
      switch (type) {
        case 'string':
          return STRING_OPERATORS;
        case 'number':
          return NUMBER_OPERATORS;
        case 'date':
          return DATE_OPERATORS;
        case 'boolean':
          return BOOLEAN_OPERATORS;
        case 'enum':
          return ENUM_OPERATORS;
        default:
          return STRING_OPERATORS;
      }
    }

    function formatValue(val: string): string {
      if (!val) return '';
      // Encode commas to prevent issues with comma-separated filter format
      return val.replace(/,/g, '%2C');
    }

    function handleOpenFilter() {
      // Notify parent that this filter is opening so it can close others
      onFilterOpen?.(column.key);
      setOpened(true);
    }

    function handlePopoverClose() {
      // Auto-apply if there are values when closing by clicking outside
      if (value.trim() && operator !== 'isnull' && operator !== 'isnotnull') {
        if (operator === 'between') {
          if (secondValue.trim()) {
            handleApply();
            return;
          }
        } else {
          handleApply();
          return;
        }
      }
      
      // If null operators are selected, apply them
      if (operator === 'isnull' || operator === 'isnotnull') {
        handleApply();
        return;
      }
      
      setOpened(false);
    }

    function handleApply() {
      // Handle null operators
      if (operator === 'isnull' || operator === 'isnotnull') {
        const expression = `${column.key},${operator},`;
        onApplyFilter(expression);
        setHasActiveFilter(true);
        setOpened(false);
        return;
      }
      
      // Handle enum 'in' and 'notin' operators with multiple selection
      if (column.type === 'enum' && (operator === 'in' || operator === 'notin')) {
        if (selectedEnumValues.length === 0) {
          return;
        }
        const values = selectedEnumValues.map(v => formatValue(v)).join(',');
        const expression = `${column.key},${operator},${values}`;
        onApplyFilter(expression);
        setHasActiveFilter(true);
        setOpened(false);
        return;
      }
      
      // Handle operators that require values
      if (!value.trim()) {
        return;
      }
      
      let expression: string;
      
      // Handle between operator
      if (operator === 'between') {
        if (!secondValue.trim()) {
          return;
        }
        expression = `${column.key},${operator},${formatValue(value)},${formatValue(secondValue)}`;
      } else {
        expression = `${column.key},${operator},${formatValue(value)}`;
      }
      
      onApplyFilter(expression);
      setHasActiveFilter(true);
      setOpened(false);
    }

    function handleClear() {
      setOperator(getDefaultOperator(column.type));
      setValue('');
      setSecondValue('');
      setSelectedEnumValues([]);
      setHasActiveFilter(false);
      onClearFilter(column.key);
      setOpened(false);
    }

    function renderValueInput() {
      // No value input needed for null operators
      if (operator === 'isnull' || operator === 'isnotnull') {
        return null;
      }

      const handleKeyDown = (event: React.KeyboardEvent) => {
        if (event.key === 'Enter') {
          handleApply();
        } else if (event.key === 'Escape') {
          handleClear();
        }
      };

      switch (column.type) {
        case 'enum':
          // For 'in' and 'notin' operators, show checklist
          if (operator === 'in' || operator === 'notin') {
            if (!column.enumOptions || column.enumOptions.length === 0) {
              return (
                <Text size="sm" c="dimmed">
                  No enum options available
                </Text>
              );
            }
            
            return (
              <Stack gap="xs">
                <Text size="sm" fw={500}>
                  {operator === 'in' ? 'Select values to include:' : 'Select values to exclude:'}
                </Text>
                <Stack gap="xs" mah={200} style={{ overflowY: 'auto' }}>
                  {column.enumOptions.map((option) => (
                    <Checkbox
                      key={option.value}
                      label={option.label}
                      checked={selectedEnumValues.includes(String(option.value))}
                      onChange={(event) => {
                        const valueStr = String(option.value);
                        if (event.currentTarget.checked) {
                          setSelectedEnumValues(prev => [...prev, valueStr]);
                        } else {
                          setSelectedEnumValues(prev => prev.filter(v => v !== valueStr));
                        }
                      }}
                    />
                  ))}
                </Stack>
              </Stack>
            );
          } else {
            // For 'eq' and 'ne' operators, show dropdown
            if (!column.enumOptions || column.enumOptions.length === 0) {
              return (
                <Text size="sm" c="dimmed">
                  No enum options available
                </Text>
              );
            }
            
            return (
              <Select
                label="Value"
                data={column.enumOptions.map(opt => ({ 
                  value: String(opt.value), 
                  label: opt.label 
                }))}
                value={value}
                onChange={(val) => setValue(val || '')}
                onKeyDown={handleKeyDown}
                placeholder="Select value..."
              />
            );
          }
          
        case 'number':
          return (
            <NumberInput
              label="Value"
              value={value ? parseValue(value, 'number') as number : undefined}
              onChange={(val) => setValue(formatValueToString(val || 0))}
              onKeyDown={handleKeyDown}
              placeholder="Enter number..."
            />
          );
          
        case 'date':
          return (
            <DateInput
              label="Value"
              value={value ? parseValue(value, 'date') as Date : null}
              onChange={(val) => setValue(val ? formatValueToString(val) : '')}
              onKeyDown={handleKeyDown}
              placeholder="Select date..."
            />
          );
          
        case 'boolean':
          return (
            <Stack gap="xs">
              <Text size="sm" fw={500}>Value</Text>
              <Switch
                label={parseValue(value, 'boolean') ? 'True' : 'False'}
                checked={parseValue(value, 'boolean') as boolean}
                onChange={(event) => setValue(formatValueToString(event.currentTarget.checked))}
              />
            </Stack>
          );
          
        default:
          return (
            <TextInput
              label="Value"
              value={value}
              onChange={(event) => setValue(event.currentTarget.value)}
              onKeyDown={handleKeyDown}
              placeholder="Enter text..."
            />
          );
      }
    }

    function renderSecondValueInput() {
      if (operator !== 'between') return null;

      const handleKeyDown = (event: React.KeyboardEvent) => {
        if (event.key === 'Enter') {
          handleApply();
        } else if (event.key === 'Escape') {
          handleClear();
        }
      };

      switch (column.type) {
        case 'number':
          return (
            <NumberInput
              label="To"
              value={secondValue ? parseValue(secondValue, 'number') as number : undefined}
              onChange={(val) => setSecondValue(formatValueToString(val || 0))}
              onKeyDown={handleKeyDown}
              placeholder="Enter number..."
            />
          );
          
        case 'date':
          return (
            <DateInput
              label="To"
              value={secondValue ? parseValue(secondValue, 'date') as Date : null}
              onChange={(val) => setSecondValue(val ? formatValueToString(val) : '')}
              onKeyDown={handleKeyDown}
              placeholder="Select date..."
            />
          );
          
        default:
          return (
            <TextInput
              label="To"
              value={secondValue}
              onChange={(event) => setSecondValue(event.currentTarget.value)}
              onKeyDown={handleKeyDown}
              placeholder="Enter text..."
            />
          );
      }
    }

    return (
      <Popover 
        opened={opened} 
        onClose={handlePopoverClose}
        position="bottom-start"
        withArrow
        shadow="md"
        closeOnClickOutside
        closeOnEscape
      >
        <Popover.Target>
          <ActionIcon
            variant={hasActiveFilter ? 'filled' : 'subtle'}
            color={hasActiveFilter ? 'blue' : 'gray'}
            size="sm"
            onClick={() => opened ? setOpened(false) : handleOpenFilter()}
          >
            <IconFilter size={14} />
          </ActionIcon>
        </Popover.Target>
        
        <Popover.Dropdown>
          <FocusTrap active={opened}>
            <Stack gap="md" style={{ minWidth: 250 }}>
              <Text size="sm" fw={500}>Filter by {column.title}</Text>
              
              <Select
                label="Operator"
                data={getOperators(column.type)}
                value={operator}
                onChange={(val) => setOperator(val || getDefaultOperator(column.type))}
                onKeyDown={(event) => {
                  if (event.key === 'Enter') {
                    handleApply();
                  } else if (event.key === 'Escape') {
                    handleClear();
                  }
                }}
              />
              
              {renderValueInput()}
              {renderSecondValueInput()}
              
              <Divider />
              
              <Group justify="space-between">
                <Button
                  variant="light"
                  color="gray"
                  onClick={handleClear}
                  leftSection={<IconX size={14} />}
                >
                  Clear
                </Button>
                
                <Button
                  onClick={handleApply}
                >
                  Apply
                </Button>
              </Group>
            </Stack>
          </FocusTrap>
        </Popover.Dropdown>
      </Popover>
    );
  }
);

ColumnFilter.displayName = 'ColumnFilter';
