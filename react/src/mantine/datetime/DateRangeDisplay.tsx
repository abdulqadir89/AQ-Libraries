import { Text, Tooltip } from '@mantine/core';
import type { DateRangeDto } from '../../utils/DateTimeOffsetUtils';
import {
  buildCompactDateRangeValue,
  buildDateRangeTooltip,
  formatDate,
} from '../../utils/DateTimeOffsetUtils';

export interface DateRangeDisplayProps {
  /**
   * DateRangeDto with start and end ISO date strings (YYYY-MM-DD format)
   */
  value: DateRangeDto | null | undefined;

  /**
   * Whether to use compact format
   * @default false
   */
  compact?: boolean;

  /**
   * Separator string to use between start and end
   * @default " - "
   */
  separator?: string;

  /**
   * Whether to show tooltips with timespan information
   * @default true
   */
  showTooltips?: boolean;

  /**
   * Additional props to pass to the Text component
   */
  textProps?: React.ComponentProps<typeof Text>;
}

/**
 * Displays a date range (date only, no time component).
 * Supports compact mode with optional tooltip showing timespan between dates.
 */
export function DateRangeDisplay({
  value,
  compact = false,
  separator = ' - ',
  showTooltips = true,
  textProps,
}: DateRangeDisplayProps) {
  if (!value || (!value.start && !value.end)) {
    return <Text {...textProps}>—</Text>;
  }

  if (compact) {
    const compactValue = buildCompactDateRangeValue(value, separator);
    if (!compactValue) {
      return <Text {...textProps}>—</Text>;
    }

    if (!showTooltips) {
      return <Text {...textProps}>{compactValue}</Text>;
    }

    const tooltipLabel = buildDateRangeTooltip(value);
    if (!tooltipLabel) {
      return <Text {...textProps}>{compactValue}</Text>;
    }

    return (
      <Tooltip label={tooltipLabel} withArrow position="top" multiline>
        <Text {...textProps} style={{ cursor: 'help', ...textProps?.style }}>
          {compactValue}
        </Text>
      </Tooltip>
    );
  }

  const startFormatted = value.start ? formatDate(value.start) : null;
  const endFormatted = value.end ? formatDate(value.end) : null;

  const displayValue = startFormatted && endFormatted
    ? `${startFormatted}${separator}${endFormatted}`
    : startFormatted
      ? `From ${startFormatted}`
      : endFormatted
        ? `Until ${endFormatted}`
        : '';

  if (!displayValue) {
    return <Text {...textProps}>—</Text>;
  }

  if (!showTooltips) {
    return <Text {...textProps}>{displayValue}</Text>;
  }

  const tooltipLabel = buildDateRangeTooltip(value);
  if (!tooltipLabel) {
    return <Text {...textProps}>{displayValue}</Text>;
  }

  return (
    <Tooltip label={tooltipLabel} withArrow position="top" multiline>
      <Text {...textProps} style={{ cursor: 'help', ...textProps?.style }}>
        {displayValue}
      </Text>
    </Tooltip>
  );
}
