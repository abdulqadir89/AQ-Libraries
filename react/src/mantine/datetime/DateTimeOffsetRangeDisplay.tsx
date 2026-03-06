import { Group, Text, Tooltip } from '@mantine/core';
import type { DateTimeOffsetRangeDto } from '../../utils/DateTimeOffsetUtils';
import {
  formatDateTimeOffsetRange,
  formatDateTimeOffsetRangeCompact,
  formatDateTimeOffsetWithOriginal,
} from '../../utils/DateTimeOffsetUtils';

export interface DateTimeOffsetRangeDisplayProps {
  /**
   * DateTimeOffsetRangeDto with start and end
   */
  value: DateTimeOffsetRangeDto | null | undefined;
  
  /**
   * Whether to use compact format (combines date/time on same day)
   * @default false
   */
  compact?: boolean;
  
  /**
   * Separator string to use between start and end
   * @default " - "
   */
  separator?: string;
  
  /**
   * Whether to show tooltips with original timezone information
   * @default true
   */
  showTooltips?: boolean;
  
  /**
   * Additional props to pass to the Text component
   */
  textProps?: React.ComponentProps<typeof Text>;
}

/**
 * Displays a DateTimeOffset range in the user's local timezone.
 * On hover over each date, shows the original timezone information.
 */
export function DateTimeOffsetRangeDisplay({
  value,
  compact = false,
  separator = ' - ',
  showTooltips = true,
  textProps,
}: DateTimeOffsetRangeDisplayProps) {
  if (!value || (!value.start && !value.end)) {
    return <Text {...textProps}>—</Text>;
  }

  // If compact mode, format as a single string and wrap in Text component
  if (compact) {
    const formattedValue = formatDateTimeOffsetRangeCompact(value);
    if (!formattedValue) {
      return <Text {...textProps}>—</Text>;
    }
    return <Text {...textProps}>{formattedValue}</Text>;
  }

  // Non-compact mode: show start and end with individual tooltips
  const startFormatted = value.start ? formatDateTimeOffsetRange({ start: value.start, end: null }, '') : null;
  const endFormatted = value.end ? formatDateTimeOffsetRange({ start: null, end: value.end }, '') : null;

  const startTooltip = value.start ? formatDateTimeOffsetWithOriginal(value.start) : null;
  const endTooltip = value.end ? formatDateTimeOffsetWithOriginal(value.end) : null;

  return (
    <Group gap="xs" style={{ display: 'inline-flex' }}>
      {startFormatted && showTooltips && startTooltip ? (
        <Tooltip label={startTooltip} withArrow position="top">
          <Text {...textProps} style={{ cursor: 'help', ...textProps?.style }}>
            {startFormatted}
          </Text>
        </Tooltip>
      ) : startFormatted ? (
        <Text {...textProps}>{startFormatted}</Text>
      ) : null}

      {startFormatted && endFormatted && (
        <Text {...textProps}>{separator}</Text>
      )}

      {endFormatted && showTooltips && endTooltip ? (
        <Tooltip label={endTooltip} withArrow position="top">
          <Text {...textProps} style={{ cursor: 'help', ...textProps?.style }}>
            {endFormatted}
          </Text>
        </Tooltip>
      ) : endFormatted ? (
        <Text {...textProps}>{endFormatted}</Text>
      ) : null}
    </Group>
  );
}
