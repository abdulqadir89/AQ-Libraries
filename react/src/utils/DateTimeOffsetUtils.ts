/**
 * Utilities for handling DateTimeOffset values with timezone awareness
 */

export interface DateTimeOffsetRangeDto {
  start?: string | null;
  end?: string | null;
}

/**
 * Gets the user's current timezone identifier (e.g., "America/New_York")
 */
export function getUserTimezone(): string {
  return Intl.DateTimeFormat().resolvedOptions().timeZone;
}

/**
 * Gets the user's timezone offset in minutes from UTC
 */
export function getUserTimezoneOffset(): number {
  return -new Date().getTimezoneOffset();
}

/**
 * Formats a DateTimeOffset string to a localized date/time string in the user's timezone
 * @param dateTimeOffset - ISO 8601 date string with timezone offset
 * @param options - Intl.DateTimeFormatOptions for formatting
 * @returns Formatted date/time string in user's timezone
 */
export function formatDateTimeOffset(
  dateTimeOffset: string | null | undefined,
  options?: Intl.DateTimeFormatOptions
): string {
  if (!dateTimeOffset) return '';

  const date = new Date(dateTimeOffset);
  if (isNaN(date.getTime())) return '';

  const defaultOptions: Intl.DateTimeFormatOptions = {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
    timeZoneName: 'short',
    ...options,
  };

  return new Intl.DateTimeFormat(undefined, defaultOptions).format(date);
}

/**
 * Formats a DateTimeOffset string showing both the user's local time and the original timezone
 * @param dateTimeOffset - ISO 8601 date string with timezone offset
 * @returns Formatted string showing both timezones (e.g., "Jan 15, 2024 10:00 AM EST (Original: 9:00 AM CST)")
 */
export function formatDateTimeOffsetWithOriginal(
  dateTimeOffset: string | null | undefined
): string {
  if (!dateTimeOffset) return '';

  const date = new Date(dateTimeOffset);
  if (isNaN(date.getTime())) return '';

  const userTimeFormat = new Intl.DateTimeFormat(undefined, {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
    timeZoneName: 'short',
  }).format(date);

  // Extract original timezone offset from the ISO string
  const match = dateTimeOffset.match(/([+-]\d{2}:\d{2}|Z)$/);
  const originalOffset = match ? match[1] : '';

  if (originalOffset === 'Z') {
    return `${userTimeFormat} (Original: UTC)`;
  } else if (originalOffset) {
    return `${userTimeFormat} (Original: UTC${originalOffset})`;
  }

  return userTimeFormat;
}

/**
 * Formats a DateTimeOffset to a short date string (e.g., "Jan 15, 2024")
 * @param dateTimeOffset - ISO 8601 date string with timezone offset
 * @returns Formatted date string
 */
export function formatDateOnly(dateTimeOffset: string | null | undefined): string {
  if (!dateTimeOffset) return '';

  const date = new Date(dateTimeOffset);
  if (isNaN(date.getTime())) return '';

  return new Intl.DateTimeFormat(undefined, {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  }).format(date);
}

/**
 * Formats a DateTimeOffset to a short time string (e.g., "10:00 AM")
 * @param dateTimeOffset - ISO 8601 date string with timezone offset
 * @returns Formatted time string
 */
export function formatTimeOnly(dateTimeOffset: string | null | undefined): string {
  if (!dateTimeOffset) return '';

  const date = new Date(dateTimeOffset);
  if (isNaN(date.getTime())) return '';

  return new Intl.DateTimeFormat(undefined, {
    hour: '2-digit',
    minute: '2-digit',
  }).format(date);
}

/**
 * Formats a DateTimeOffset range (e.g., "Jan 15, 2024 10:00 AM - Jan 16, 2024 2:00 PM")
 * @param range - DateTimeOffsetRangeDto with start and end
 * @param separator - String to use between start and end (default: " - ")
 * @returns Formatted range string
 */
export function formatDateTimeOffsetRange(
  range: DateTimeOffsetRangeDto | null | undefined,
  separator: string = ' - '
): string {
  if (!range) return '';

  const start = range.start ? formatDateTimeOffset(range.start) : '';
  const end = range.end ? formatDateTimeOffset(range.end) : '';

  if (start && end) {
    return `${start}${separator}${end}`;
  } else if (start) {
    return `From ${start}`;
  } else if (end) {
    return `Until ${end}`;
  }

  return '';
}

/**
 * Formats a DateTimeOffset range in compact form (date only if same day)
 * @param range - DateTimeOffsetRangeDto with start and end
 * @returns Formatted compact range string
 */
export function formatDateTimeOffsetRangeCompact(
  range: DateTimeOffsetRangeDto | null | undefined
): string {
  if (!range) return '';

  const startDate = range.start ? new Date(range.start) : null;
  const endDate = range.end ? new Date(range.end) : null;

  if (!startDate && !endDate) return '';

  if (startDate && endDate) {
    const sameDay =
      startDate.getFullYear() === endDate.getFullYear() &&
      startDate.getMonth() === endDate.getMonth() &&
      startDate.getDate() === endDate.getDate();

    if (sameDay) {
      const dateStr = formatDateOnly(range.start);
      const startTime = formatTimeOnly(range.start);
      const endTime = formatTimeOnly(range.end);
      return `${dateStr} ${startTime} - ${endTime}`;
    }

    return formatDateTimeOffsetRange(range);
  } else if (startDate) {
    return `From ${formatDateTimeOffset(range.start)}`;
  } else if (endDate) {
    return `Until ${formatDateTimeOffset(range.end)}`;
  }

  return '';
}

/**
 * Converts a Date object to an ISO 8601 string with the user's timezone offset
 * @param date - Date object to convert
 * @returns ISO 8601 string with timezone offset (e.g., "2024-01-15T10:00:00-05:00")
 */
export function toDateTimeOffsetString(date: Date | null | undefined): string | null {
  if (!date || !(date instanceof Date)) return null;

  const iso = date.toISOString();
  const offset = -date.getTimezoneOffset();
  const offsetHours = Math.floor(Math.abs(offset) / 60);
  const offsetMinutes = Math.abs(offset) % 60;
  const offsetSign = offset >= 0 ? '+' : '-';
  const offsetString = `${offsetSign}${String(offsetHours).padStart(2, '0')}:${String(offsetMinutes).padStart(2, '0')}`;

  // Replace 'Z' with the actual offset
  return iso.replace('Z', offsetString);
}

/**
 * Converts a DateTimeOffset string to a Date object
 * @param dateTimeOffset - ISO 8601 date string with timezone offset
 * @returns Date object or null if invalid
 */
export function parseDateTimeOffset(dateTimeOffset: string | null | undefined): Date | null {
  if (!dateTimeOffset) return null;

  const date = new Date(dateTimeOffset);
  return isNaN(date.getTime()) ? null : date;
}

/**
 * Creates a DateTimeOffsetRangeDto from two Date objects
 * @param start - Start date
 * @param end - End date
 * @returns DateTimeOffsetRangeDto
 */
export function createDateTimeOffsetRange(
  start: Date | null | undefined,
  end: Date | null | undefined
): DateTimeOffsetRangeDto {
  return {
    start: start ? toDateTimeOffsetString(start) : null,
    end: end ? toDateTimeOffsetString(end) : null,
  };
}

/**
 * Gets a human-readable relative time string (e.g., "2 hours ago", "in 3 days")
 * @param dateTimeOffset - ISO 8601 date string with timezone offset
 * @returns Relative time string
 */
export function getRelativeTime(dateTimeOffset: string | null | undefined): string {
  if (!dateTimeOffset) return '';

  const date = parseDateTimeOffset(dateTimeOffset);
  if (!date) return '';

  const now = new Date();
  const diffMs = now.getTime() - date.getTime();
  const diffSec = Math.floor(diffMs / 1000);
  const diffMin = Math.floor(diffSec / 60);
  const diffHour = Math.floor(diffMin / 60);
  const diffDay = Math.floor(diffHour / 24);
  const diffMonth = Math.floor(diffDay / 30);
  const diffYear = Math.floor(diffDay / 365);

  if (Math.abs(diffSec) < 60) {
    return 'just now';
  } else if (Math.abs(diffMin) < 60) {
    return diffMin > 0 ? `${diffMin} minute${diffMin !== 1 ? 's' : ''} ago` : `in ${-diffMin} minute${diffMin !== -1 ? 's' : ''}`;
  } else if (Math.abs(diffHour) < 24) {
    return diffHour > 0 ? `${diffHour} hour${diffHour !== 1 ? 's' : ''} ago` : `in ${-diffHour} hour${diffHour !== -1 ? 's' : ''}`;
  } else if (Math.abs(diffDay) < 30) {
    return diffDay > 0 ? `${diffDay} day${diffDay !== 1 ? 's' : ''} ago` : `in ${-diffDay} day${diffDay !== -1 ? 's' : ''}`;
  } else if (Math.abs(diffMonth) < 12) {
    return diffMonth > 0 ? `${diffMonth} month${diffMonth !== 1 ? 's' : ''} ago` : `in ${-diffMonth} month${diffMonth !== -1 ? 's' : ''}`;
  } else {
    return diffYear > 0 ? `${diffYear} year${diffYear !== 1 ? 's' : ''} ago` : `in ${-diffYear} year${diffYear !== -1 ? 's' : ''}`;
  }
}
