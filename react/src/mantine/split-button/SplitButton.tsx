'use client';

import { Button, Menu, Tooltip } from '@mantine/core';
import { IconChevronDown } from '@tabler/icons-react';
import type { ButtonProps, MenuProps, MantineColor } from '@mantine/core';
import type { ReactNode } from 'react';

export interface SplitButtonItem {
  key: string;
  label: string;
  color?: MantineColor;
  leftSection?: ReactNode;
  bold?: boolean;
  onClick: () => void;
}

export interface SplitButtonSection {
  label?: string;
  items: SplitButtonItem[];
}

export interface SplitButtonProps {
  /** Label shown on the main button */
  label: string;
  /** Icon shown to the left of the label */
  leftSection?: ReactNode;
  /** Called when the main button body is clicked */
  onClick: () => void;
  loading?: boolean;
  disabled?: boolean;
  /** Sections of items in the dropdown menu */
  sections: SplitButtonSection[];
  /** Tooltip for the main button */
  tooltip?: string;
  /** Tooltip for the chevron */
  chevronTooltip?: string;
  /** Passed to both button halves */
  color?: MantineColor;
  variant?: ButtonProps['variant'];
  size?: ButtonProps['size'];
  menuPosition?: MenuProps['position'];
}

export function SplitButton({
  label,
  leftSection,
  onClick,
  loading,
  disabled,
  sections,
  tooltip,
  chevronTooltip,
  color,
  variant = 'default',
  size = 'xs',
  menuPosition = 'bottom-end',
}: SplitButtonProps) {
  const mainButton = (
    <Button
      size={size}
      variant={variant}
      color={color}
      loading={loading}
      disabled={disabled}
      leftSection={leftSection}
      onClick={onClick}
    >
      {label}
    </Button>
  );

  return (
    <Button.Group>
      {tooltip ? (
        <Tooltip label={tooltip} withArrow>
          {mainButton}
        </Tooltip>
      ) : mainButton}

      <Menu position={menuPosition} withinPortal>
        <Menu.Target>
          {chevronTooltip ? (
            <Tooltip label={chevronTooltip} withArrow>
              <Button
                size={size}
                variant={variant}
                color={color}
                disabled={disabled}
                px={6}
                aria-label="More options"
              >
                <IconChevronDown size={12} />
              </Button>
            </Tooltip>
          ) : (
            <Button
              size={size}
              variant={variant}
              color={color}
              disabled={disabled}
              px={6}
              aria-label="More options"
            >
              <IconChevronDown size={12} />
            </Button>
          )}
        </Menu.Target>

        <Menu.Dropdown>
          {sections.map((section, si) => (
            <div key={si}>
              {si > 0 && <Menu.Divider />}
              {section.label && <Menu.Label>{section.label}</Menu.Label>}
              {section.items.map(item => (
                <Menu.Item
                  key={item.key}
                  color={item.color}
                  leftSection={item.leftSection}
                  onClick={item.onClick}
                  fw={item.bold ? 700 : undefined}
                >
                  {item.label}
                </Menu.Item>
              ))}
            </div>
          ))}
        </Menu.Dropdown>
      </Menu>
    </Button.Group>
  );
}
