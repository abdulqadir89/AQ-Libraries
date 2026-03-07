import { Input, Stack } from '@mantine/core';
import { DateTimePicker } from '@mantine/dates';
import type { DateTimeOffsetRangeDto } from '../../utils/DateTimeOffsetUtils';
import { toDateTimeOffsetString } from '../../utils/DateTimeOffsetUtils';

function toValidDate(value: unknown): Date | null {
  if (!value) return null;

  if (value instanceof Date) {
    return Number.isNaN(value.getTime()) ? null : value;
  }

  if (typeof value === 'string' || typeof value === 'number') {
    const parsed = new Date(value);
    return Number.isNaN(parsed.getTime()) ? null : parsed;
  }

  // Support Dayjs-like objects that expose toDate().
  if (typeof value === 'object' && value !== null && 'toDate' in value && typeof value.toDate === 'function') {
    const parsed = value.toDate();
    if (parsed instanceof Date && !Number.isNaN(parsed.getTime())) {
      return parsed;
    }
  }

  return null;
}

export interface DateTimeOffsetRangeInputProps {
  /** DateTimeOffsetRangeDto with start and end ISO strings */
  value?: DateTimeOffsetRangeDto | null;
  /** Called when start or end changes */
  onChange?: (value: DateTimeOffsetRangeDto) => void;
  label?: string;
  description?: string;
  error?: string | React.ReactNode;
  required?: boolean;
  disabled?: boolean;
  clearable?: boolean;
  /** Additional wrapper style */
  style?: React.CSSProperties;
}

/**
 * A simple form input for a DateTimeOffset range using two Mantine DateTimePickers.
 * 
 * - DateTimePicker naturally handles browser's local timezone via JavaScript Date
 * - Stores times as ISO 8601 strings with user's LOCAL timezone offset (not UTC)
 * - Supports open-ended ranges (start only, end only, or both)
 */
export function DateTimeOffsetRangeInput({
  value,
  onChange,
  label,
  description,
  error,
  required,
  disabled,
  clearable = true,
  style,
}: DateTimeOffsetRangeInputProps) {
  // Convert ISO strings to Date objects for the pickers
  // JavaScript Date constructor automatically handles timezone conversion
  const startDate = toValidDate(value?.start);
  const endDate = toValidDate(value?.end);

  const handleStartChange = (date: unknown) => {
    const normalized = toValidDate(date);

    // Store as wall-clock local time with user's timezone offset (not UTC conversion)
    onChange?.({
      start: toDateTimeOffsetString(normalized),
      end: value?.end ?? null,
    });
  };

  const handleEndChange = (date: unknown) => {
    const normalized = toValidDate(date);

    // Store as wall-clock local time with user's timezone offset (not UTC conversion)
    onChange?.({
      start: value?.start ?? null,
      end: toDateTimeOffsetString(normalized),
    });
  };

  return (
    <Input.Wrapper label={label} description={description} error={error} required={required} style={style}>
      <Stack gap="sm" mt={label ? 8 : 0}>
        <DateTimePicker
          label="Start"
          placeholder="Pick start date & time"
          value={startDate}
          onChange={handleStartChange}
          clearable={clearable}
          disabled={disabled}
          size="sm"
        />
        <DateTimePicker
          label="End"
          placeholder="Pick end date & time"
          value={endDate}
          onChange={handleEndChange}
          clearable={clearable}
          disabled={disabled}
          size="sm"
        />
      </Stack>
    </Input.Wrapper>
  );
}
