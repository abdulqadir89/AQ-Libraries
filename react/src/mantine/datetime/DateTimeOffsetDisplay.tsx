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
  // Default to an inline element so this component is safe to nest inside another
  // Text/<p>-rendering ancestor (e.g. table cells, AuditInfo lines) — Mantine's Text
  // defaults to <p>, and a <p> inside a <p> is invalid HTML that trips React hydration
  // warnings. Callers can still override via textProps.component.
  const mergedTextProps = { component: 'span' as const, ...textProps };

  if (!value) {
    return <Text {...mergedTextProps}>—</Text>;
  }

  const formattedValue = formatDateTimeOffset(value, format);

  if (!formattedValue) {
    return <Text {...mergedTextProps}>—</Text>;
  }

  if (!showTooltip) {
    return <Text {...mergedTextProps}>{formattedValue}</Text>;
  }

  const tooltipLabel = formatDateTimeOffsetWithOriginal(value);

  return (
    <Tooltip label={tooltipLabel} withArrow position="top">
      <Text {...mergedTextProps} style={{ cursor: 'help', ...((mergedTextProps as { style?: CSSProperties })?.style) }}>
        {formattedValue}
      </Text>
    </Tooltip>
  );
}
