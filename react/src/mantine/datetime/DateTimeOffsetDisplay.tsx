import type { CSSProperties } from 'react';
import { Text, Tooltip } from '@mantine/core';
import { formatDateTimeOffset, formatDateTimeOffsetWithOriginal } from '../../utils/DateTimeOffsetUtils';

export interface DateTimeOffsetDisplayProps {
  /**
   * ISO 8601 date string with timezone offset
   */
  value: string | null | undefined;
  
  /**
   * Format options for the display
   */
  format?: Intl.DateTimeFormatOptions;
  
  /**
   * Whether to show a tooltip with original timezone information
   * @default true
   */
  showTooltip?: boolean;
  
  /**
   * Additional props to pass to the Text component
   */
  textProps?: React.ComponentProps<typeof Text>;
}

/**
 * Displays a DateTimeOffset value in the user's local timezone.
 * On hover, shows the original timezone information.
 */
export function DateTimeOffsetDisplay({
  value,
  format,
  showTooltip = true,
  textProps,
}: DateTimeOffsetDisplayProps) {
  if (!value) {
    return <Text {...textProps}>—</Text>;
  }

  const formattedValue = formatDateTimeOffset(value, format);
  
  if (!formattedValue) {
    return <Text {...textProps}>—</Text>;
  }

  if (!showTooltip) {
    return <Text {...textProps}>{formattedValue}</Text>;
  }

  const tooltipLabel = formatDateTimeOffsetWithOriginal(value);

  return (
    <Tooltip label={tooltipLabel} withArrow position="top">
      <Text {...textProps} style={{ cursor: 'help', ...((textProps as { style?: CSSProperties })?.style) }}>
        {formattedValue}
      </Text>
    </Tooltip>
  );
}
