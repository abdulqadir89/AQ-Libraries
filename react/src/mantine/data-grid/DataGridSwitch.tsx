import { ActionIcon, Tooltip } from '@mantine/core';
import { IconLayoutGrid, IconTable } from '@tabler/icons-react';
import type { DataGridSwitchProps } from './DataGrid.types';

export function DataGridSwitch({
  value,
  onChange,
  tableLabel = 'Table',
  cardLabel = 'Cards',
}: DataGridSwitchProps) {
  const isTable = value === 'table';
  const nextValue = isTable ? 'card' : 'table';
  const switchLabel = isTable ? `Switch to ${cardLabel}` : `Switch to ${tableLabel}`;

  return (
    <Tooltip label={switchLabel} withArrow>
      <ActionIcon
        variant="light"
        size="lg"
        onClick={() => onChange(nextValue)}
        title={switchLabel}
        aria-label={switchLabel}
      >
        {isTable ? <IconLayoutGrid size={16} /> : <IconTable size={16} />}
      </ActionIcon>
    </Tooltip>
  );
}
