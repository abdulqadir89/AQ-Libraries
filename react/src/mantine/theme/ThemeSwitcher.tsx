/**
 * Theme Switcher Component
 * 
 * Allows users to switch between theme presets and color schemes.
 * Uses useTheme() context from ThemeProvider.
 */

import { ActionIcon, Menu, Group, Text } from '@mantine/core';
import { IconPalette, IconSun, IconMoon } from '@tabler/icons-react';
import { useTheme } from './ThemeProvider';

/** Get display name for a theme (capitalizes first letter) */
function getThemeDisplayName(themeName: string): string {
  return themeName.charAt(0).toUpperCase() + themeName.slice(1);
}

export function ThemeSwitcher() {
  const { themeName, setThemeName, availableThemes, colorScheme, toggleColorScheme } = useTheme();

  return (
    <Group gap="xs">
      {/* Color Scheme Toggle */}
      <ActionIcon
        variant="default"
        size="lg"
        onClick={toggleColorScheme}
        aria-label="Toggle color scheme"
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
              aria-label="Change theme"
            >
              <IconPalette size={18} />
            </ActionIcon>
          </Menu.Target>

          <Menu.Dropdown>
            <Menu.Label>Theme</Menu.Label>
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
