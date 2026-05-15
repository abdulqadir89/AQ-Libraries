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

function fmt(d: Date): string {
  const p = (n: number) => String(n).padStart(2, '0');
  return `${d.getFullYear()}-${p(d.getMonth() + 1)}-${p(d.getDate())} ${p(d.getHours())}:${p(d.getMinutes())}:${p(d.getSeconds())}`;
}

function shifted(base: Date, ms: number): Date {
  return new Date(base.getTime() + ms);
}

const DAY = 86_400_000;
const WEEK = 7 * DAY;

function addMonths(d: Date, n: number): Date {
  const r = new Date(d);
  r.setMonth(r.getMonth() + n);
  return r;
}

function addYears(d: Date, n: number): Date {
  const r = new Date(d);
  r.setFullYear(r.getFullYear() + n);
  return r;
}

export interface DateTimePreset {
  /** Datetime string in 'YYYY-MM-DD HH:mm:ss' format, as expected by Mantine DateTimePicker */
  value: string;
  label: string;
}

function buildPresets(now: Date, minDate: Date | undefined, maxDate: Date | undefined): DateTimePreset[] {
  const candidates: DateTimePreset[] = [
    { value: fmt(addYears(now, -1)), label: 'Last year' },
    { value: fmt(addMonths(now, -1)), label: 'Last month' },
    { value: fmt(shifted(now, -WEEK)), label: 'Last week' },
    { value: fmt(shifted(now, -DAY)), label: 'Yesterday' },
    { value: fmt(now), label: 'Now' },
    { value: fmt(shifted(now, DAY)), label: 'Tomorrow' },
    { value: fmt(shifted(now, WEEK)), label: 'Next week' },
    { value: fmt(addMonths(now, 1)), label: 'Next month' },
    { value: fmt(addYears(now, 1)), label: 'Next year' },
  ];

  return candidates.filter(({ value }) => {
    const d = new Date(value);
    if (minDate && d < minDate) return false;
    if (maxDate && d > maxDate) return false;
    return true;
  });
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
  /** Earliest selectable date for both pickers */
  minDate?: Date;
  /** Latest selectable date for both pickers */
  maxDate?: Date;
  /**
   * Presets for the Start picker. Defaults to common relative dates filtered by
   * minDate/maxDate. Pass an empty array to disable presets.
   */
  startPresets?: DateTimePreset[];
  /**
   * Presets for the End picker. Defaults to common relative dates filtered by
   * minDate/maxDate and the selected start. Pass an empty array to disable presets.
   */
  endPresets?: DateTimePreset[];
  /** Additional wrapper style */
  style?: React.CSSProperties;
}

/**
 * A simple form input for a DateTimeOffset range using two Mantine DateTimePickers.
 *
 * - DateTimePicker naturally handles browser's local timezone via JavaScript Date
 * - Stores times as ISO 8601 strings with user's LOCAL timezone offset (not UTC)
 * - Supports open-ended ranges (start only, end only, or both)
 * - End date is constrained to >= start date when a start is selected
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
  minDate,
  maxDate,
  startPresets,
  endPresets,
  style,
}: DateTimeOffsetRangeInputProps) {
  const startDate = toValidDate(value?.start);
  const endDate = toValidDate(value?.end);

  // End picker's lower bound is whichever is later: minDate or the chosen start.
  const endMinDate = startDate && (!minDate || startDate > minDate) ? startDate : minDate;

  const now = new Date();
  const resolvedStartPresets = startPresets ?? buildPresets(now, minDate, maxDate);
  const resolvedEndPresets = endPresets ?? buildPresets(now, endMinDate, maxDate);

  const handleStartChange = (date: unknown) => {
    const normalized = toValidDate(date);

    // Clear end if it would become before the new start.
    const currentEnd = toValidDate(value?.end);
    const end = normalized && currentEnd && currentEnd < normalized ? null : (value?.end ?? null);

    onChange?.({ start: toDateTimeOffsetString(normalized), end });
  };

  const handleEndChange = (date: unknown) => {
    const normalized = toValidDate(date);
    onChange?.({ start: value?.start ?? null, end: toDateTimeOffsetString(normalized) });
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
          minDate={minDate}
          maxDate={maxDate}
          presets={resolvedStartPresets}
        />
        <DateTimePicker
          label="End"
          placeholder="Pick end date & time"
          value={endDate}
          onChange={handleEndChange}
          clearable={clearable}
          disabled={disabled}
          size="sm"
          minDate={endMinDate}
          maxDate={maxDate}
          presets={resolvedEndPresets}
        />
      </Stack>
    </Input.Wrapper>
  );
}
