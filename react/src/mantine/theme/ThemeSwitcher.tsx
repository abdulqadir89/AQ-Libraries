/**
 * Theme Switcher Component
 * 
 * Allows users to switch between theme presets and color schemes.
 * Uses useTheme() context from ThemeProvider.
 */

import { ActionIcon, Menu, Group, Text } from '@mantine/core';
import { IconPalette, IconSun, IconMoon } from '@tabler/icons-react';
import { useTheme } from './ThemeProvider';
import { useAQLocale } from '../locale';

/** Get display name for a theme (capitalizes first letter) */
function getThemeDisplayName(themeName: string): string {
  return themeName.charAt(0).toUpperCase() + themeName.slice(1);
}

export function ThemeSwitcher() {
  const { themeName, setThemeName, availableThemes, colorScheme, toggleColorScheme } = useTheme();
  const { messages: aqMessages } = useAQLocale();
  const m = aqMessages.themeSwitcher;

  return (
    <Group gap="xs">
      {/* Color Scheme Toggle */}
      <ActionIcon
        variant="default"
        size="lg"
        onClick={toggleColorScheme}
        aria-label={m.toggleColorScheme}
      >
        {colorScheme === 'dark' ? <IconSun size={18} /> : <IconMoon size={18} />}
      </ActionIcon>

      {/* Theme Preset Menu */}
      {availableThemes.length > 1 && (
        <Menu shadow="md" width={200}>
          <Menu.Target>
            <ActionIcon
              variant="default"
              size="lg"
              aria-label={m.changeTheme}
            >
              <IconPalette size={18} />
            </ActionIcon>
          </Menu.Target>

          <Menu.Dropdown>
            <Menu.Label>{m.theme}</Menu.Label>
            {availableThemes.map((theme) => (
              <Menu.Item
                key={theme}
                onClick={() => setThemeName(theme)}
                rightSection={themeName === theme ? '✓' : null}
              >
                <Group gap="xs">
                  <Text size="sm">{getThemeDisplayName(theme)}</Text>
                </Group>
              </Menu.Item>
            ))}
          </Menu.Dropdown>
        </Menu>
      )}
    </Group>
  );
}
