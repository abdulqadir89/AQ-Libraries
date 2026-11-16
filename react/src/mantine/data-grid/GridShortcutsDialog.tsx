'use client';

import { Kbd, Modal, Stack, Table, Text } from '@mantine/core';

export interface GridShortcutItem {
  label: string;
  combo: string | false | undefined;
}

export interface GridShortcutSection {
  title: string;
  items: GridShortcutItem[];
}

export interface GridShortcutsDialogProps {
  opened: boolean;
  onClose: () => void;
  title: string;
  sections: GridShortcutSection[];
  formatKey: (combo: string) => string;
}

// The keys a grid answers to right now, written with the user's own key names. Shared by the table and the card grid.
export function GridShortcutsDialog({ opened, onClose, title, sections, formatKey }: GridShortcutsDialogProps) {
  return (
    <Modal opened={opened} onClose={onClose} title={title} size="md">
      <Stack gap="md">
        {sections.map(section => {
          const items = section.items.filter(item => !!item.combo);
          if (items.length === 0) return null;
          return (
            <div key={section.title}>
              <Text fw={600} size="sm" mb="xs">{section.title}</Text>
              <Table withRowBorders={false} verticalSpacing={4}>
                <Table.Tbody>
                  {items.map(item => (
                    <Table.Tr key={`${item.label}|${item.combo}`}>
                      <Table.Td>{item.label}</Table.Td>
                      <Table.Td ta="right"><Kbd>{formatKey(item.combo as string)}</Kbd></Table.Td>
                    </Table.Tr>
                  ))}
                </Table.Tbody>
              </Table>
            </div>
          );
        })}
      </Stack>
    </Modal>
  );
}
