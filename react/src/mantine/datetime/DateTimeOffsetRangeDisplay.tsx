import type { CSSProperties } from 'react';
import { Text, Tooltip } from '@mantine/core';
import type { DateTimeOffsetRangeDto } from '../../utils/DateTimeOffsetUtils';
import {
  getTimezoneSuffix,
  buildCompactRangeValue,
  buildRangeIntervalTooltip,
  formatDateTimeNoTz,
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
 * Timezone suffix is shown only once at the end and only when different from the user's timezone.
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

  const timezoneSuffix = getTimezoneSuffix(value);

  if (compact) {
    const compactValue = buildCompactRangeValue(value, separator, timezoneSuffix);
    if (!compactValue) {
      return <Text {...textProps}>—</Text>;
    }

    if (!showTooltips) {
      return <Text {...textProps}>{compactValue}</Text>;
    }

    const tooltipLabel = buildRangeIntervalTooltip(value);
    if (!tooltipLabel) {
      return <Text {...textProps}>{compactValue}</Text>;
    }

    return (
      <Tooltip label={tooltipLabel} withArrow position="top">
        <Text {...textProps} style={{ cursor: 'help', ...((textProps as { style?: CSSProperties })?.style) }}>
          {compactValue}
        </Text>
      </Tooltip>
    );
  }

  const startFormatted = value.start ? formatDateTimeNoTz(value.start) : null;
  const endFormatted = value.end ? formatDateTimeNoTz(value.end) : null;

  const displayValue = startFormatted && endFormatted
    ? `${startFormatted}${separator}${endFormatted}${timezoneSuffix}`
    : startFormatted
      ? `From ${startFormatted}${timezoneSuffix}`
      : endFormatted
        ? `Until ${endFormatted}${timezoneSuffix}`
        : '';

  if (!displayValue) {
    return <Text {...textProps}>—</Text>;
  }

  if (!showTooltips) {
    return <Text {...textProps}>{displayValue}</Text>;
  }

  const tooltipLabel = buildRangeIntervalTooltip(value);
  if (!tooltipLabel) {
    return <Text {...textProps}>{displayValue}</Text>;
  }

  return (
    <Tooltip label={tooltipLabel} withArrow position="top">
      <Text {...textProps} style={{ cursor: 'help', ...((textProps as { style?: CSSProperties })?.style) }}>
        {displayValue}
      </Text>
    </Tooltip>
  );
}
