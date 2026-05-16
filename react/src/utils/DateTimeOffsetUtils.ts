/**
 * Utilities for handling DateTimeOffset values with timezone awareness
 */

export interface DateTimeOffsetRangeDto {
  start?: string | null;
  end?: string | null;
}

/**
 * Simple date range with ISO date strings (YYYY-MM-DD format)
 */
export interface DateRangeDto {
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
 * Converts a Date object to an ISO 8601 string with the user's timezone offset.
 * Produces wall-clock local time with offset (e.g., "2024-01-15T10:00:00-05:00").
 * @param date - Date object to convert
 * @param timezoneId - Optional IANA timezone id (e.g. "America/New_York"). Defaults to the user's local timezone.
 * @returns ISO 8601 string with timezone offset
 */
export function toDateTimeOffsetString(date: Date | null | undefined, timezoneId?: string): string | null {
  if (!date || !(date instanceof Date)) return null;

  const tz = timezoneId || getUserTimezone();

  // Get the offset minutes for this timezone at this instant
  const offsetMinutes = getTimezoneOffsetMinutes(date, tz);
  const offsetSign = offsetMinutes >= 0 ? '+' : '-';
  const absOffset = Math.abs(offsetMinutes);
  const offsetHours = Math.floor(absOffset / 60);
  const offsetMins = absOffset % 60;
  const offsetString = `${offsetSign}${String(offsetHours).padStart(2, '0')}:${String(offsetMins).padStart(2, '0')}`;

  // Get local wall-clock time in the target timezone
  const parts = new Intl.DateTimeFormat('en-CA', {
    timeZone: tz,
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit',
    hour12: false,
  }).formatToParts(date);

  const get = (type: string) => parts.find(p => p.type === type)?.value ?? '00';
  const localDateStr = `${get('year')}-${get('month')}-${get('day')}T${get('hour')}:${get('minute')}:${get('second')}`;

  return `${localDateStr}${offsetString}`;
}

/**
 * Gets the UTC offset in minutes for a given timezone at a given instant.
 * Positive = ahead of UTC (e.g., UTC+5 returns 300).
 */
export function getTimezoneOffsetMinutes(date: Date, timezoneId: string): number {
  // Format the date both in UTC and in the target timezone
  const utcStr = new Intl.DateTimeFormat('en-CA', {
    timeZone: 'UTC',
    year: 'numeric', month: '2-digit', day: '2-digit',
    hour: '2-digit', minute: '2-digit', second: '2-digit', hour12: false,
  }).format(date);
  const tzStr = new Intl.DateTimeFormat('en-CA', {
    timeZone: timezoneId,
    year: 'numeric', month: '2-digit', day: '2-digit',
    hour: '2-digit', minute: '2-digit', second: '2-digit', hour12: false,
  }).format(date);

  const utcDate = new Date(utcStr.replace(', ', 'T').replace(' ', 'T') + 'Z');
  const tzDate = new Date(tzStr.replace(', ', 'T').replace(' ', 'T') + 'Z');

  return Math.round((tzDate.getTime() - utcDate.getTime()) / 60000);
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
 * Parses an ISO 8601 DateTimeOffset string into its wall-clock components.
 * Returns the date, time (HH:mm), and timezone offset string as stored (not converted to local).
 */
export interface DateTimeOffsetParts {
  /** Local date at the stored offset, e.g. "2024-01-15" */
  date: string;
  /** Local time at the stored offset, e.g. "10:30" */
  time: string;
  /** Offset string, e.g. "+05:00" or "-07:00" or "Z" */
  offset: string;
}

export function parseDateTimeOffsetParts(dateTimeOffset: string | null | undefined): DateTimeOffsetParts | null {
  if (!dateTimeOffset) return null;
  // Match: YYYY-MM-DDTHH:mm:ss(.fff)?(+|-HH:mm|Z)
  const match = dateTimeOffset.match(
    /^(\d{4}-\d{2}-\d{2})T(\d{2}:\d{2})(?::\d{2}(?:\.\d+)?)?([+-]\d{2}:\d{2}|Z)$/
  );
  if (!match) return null;
  return {
    date: match[1],
    time: match[2],
    offset: match[3],
  };
}

/**
 * Converts an IANA timezone id to an offset string like "+05:00" at a given instant.
 */
export function timezoneIdToOffsetString(timezoneId: string, at: Date = new Date()): string {
  const offsetMinutes = getTimezoneOffsetMinutes(at, timezoneId);
  const sign = offsetMinutes >= 0 ? '+' : '-';
  const abs = Math.abs(offsetMinutes);
  const h = Math.floor(abs / 60);
  const m = abs % 60;
  return `${sign}${String(h).padStart(2, '0')}:${String(m).padStart(2, '0')}`;
}

/**
 * Returns a list of common IANA timezone identifiers grouped by region, with display labels.
 */
export interface TimezoneOption {
  value: string; // IANA id
  label: string; // Display name with offset
  group: string;
}

export function getCommonTimezones(at: Date = new Date()): TimezoneOption[] {
  const tzList: Array<{ value: string; group: string }> = [
    // UTC
    { value: 'UTC', group: 'UTC' },
    // Americas
    { value: 'America/New_York', group: 'Americas' },
    { value: 'America/Chicago', group: 'Americas' },
    { value: 'America/Denver', group: 'Americas' },
    { value: 'America/Phoenix', group: 'Americas' },
    { value: 'America/Los_Angeles', group: 'Americas' },
    { value: 'America/Anchorage', group: 'Americas' },
    { value: 'America/Adak', group: 'Americas' },
    { value: 'Pacific/Honolulu', group: 'Americas' },
    { value: 'America/Toronto', group: 'Americas' },
    { value: 'America/Vancouver', group: 'Americas' },
    { value: 'America/Mexico_City', group: 'Americas' },
    { value: 'America/Bogota', group: 'Americas' },
    { value: 'America/Lima', group: 'Americas' },
    { value: 'America/Santiago', group: 'Americas' },
    { value: 'America/Buenos_Aires', group: 'Americas' },
    { value: 'America/Sao_Paulo', group: 'Americas' },
    // Europe
    { value: 'Europe/London', group: 'Europe' },
    { value: 'Europe/Dublin', group: 'Europe' },
    { value: 'Europe/Lisbon', group: 'Europe' },
    { value: 'Europe/Paris', group: 'Europe' },
    { value: 'Europe/Berlin', group: 'Europe' },
    { value: 'Europe/Rome', group: 'Europe' },
    { value: 'Europe/Madrid', group: 'Europe' },
    { value: 'Europe/Amsterdam', group: 'Europe' },
    { value: 'Europe/Brussels', group: 'Europe' },
    { value: 'Europe/Zurich', group: 'Europe' },
    { value: 'Europe/Stockholm', group: 'Europe' },
    { value: 'Europe/Helsinki', group: 'Europe' },
    { value: 'Europe/Warsaw', group: 'Europe' },
    { value: 'Europe/Prague', group: 'Europe' },
    { value: 'Europe/Vienna', group: 'Europe' },
    { value: 'Europe/Budapest', group: 'Europe' },
    { value: 'Europe/Bucharest', group: 'Europe' },
    { value: 'Europe/Athens', group: 'Europe' },
    { value: 'Europe/Istanbul', group: 'Europe' },
    { value: 'Europe/Moscow', group: 'Europe' },
    { value: 'Europe/Kiev', group: 'Europe' },
    // Middle East / Africa
    { value: 'Asia/Dubai', group: 'Middle East' },
    { value: 'Asia/Riyadh', group: 'Middle East' },
    { value: 'Asia/Baghdad', group: 'Middle East' },
    { value: 'Asia/Tehran', group: 'Middle East' },
    { value: 'Asia/Kuwait', group: 'Middle East' },
    { value: 'Africa/Cairo', group: 'Africa' },
    { value: 'Africa/Johannesburg', group: 'Africa' },
    { value: 'Africa/Lagos', group: 'Africa' },
    { value: 'Africa/Nairobi', group: 'Africa' },
    // Asia
    { value: 'Asia/Karachi', group: 'Asia' },
    { value: 'Asia/Kolkata', group: 'Asia' },
    { value: 'Asia/Colombo', group: 'Asia' },
    { value: 'Asia/Dhaka', group: 'Asia' },
    { value: 'Asia/Rangoon', group: 'Asia' },
    { value: 'Asia/Bangkok', group: 'Asia' },
    { value: 'Asia/Jakarta', group: 'Asia' },
    { value: 'Asia/Singapore', group: 'Asia' },
    { value: 'Asia/Kuala_Lumpur', group: 'Asia' },
    { value: 'Asia/Manila', group: 'Asia' },
    { value: 'Asia/Shanghai', group: 'Asia' },
    { value: 'Asia/Hong_Kong', group: 'Asia' },
    { value: 'Asia/Taipei', group: 'Asia' },
    { value: 'Asia/Seoul', group: 'Asia' },
    { value: 'Asia/Tokyo', group: 'Asia' },
    // Oceania
    { value: 'Australia/Perth', group: 'Oceania' },
    { value: 'Australia/Darwin', group: 'Oceania' },
    { value: 'Australia/Adelaide', group: 'Oceania' },
    { value: 'Australia/Brisbane', group: 'Oceania' },
    { value: 'Australia/Sydney', group: 'Oceania' },
    { value: 'Australia/Melbourne', group: 'Oceania' },
    { value: 'Pacific/Auckland', group: 'Oceania' },
    { value: 'Pacific/Fiji', group: 'Oceania' },
  ];

  return tzList.map(tz => {
    try {
      const offset = timezoneIdToOffsetString(tz.value, at);
      const shortName = new Intl.DateTimeFormat('en', { timeZone: tz.value, timeZoneName: 'short' })
        .formatToParts(at)
        .find(p => p.type === 'timeZoneName')?.value ?? '';
      return {
        value: tz.value,
        label: `(UTC${offset}) ${tz.value.replace(/_/g, ' ')}${shortName ? ` — ${shortName}` : ''}`,
        group: tz.group,
      };
    } catch {
      return null;
    }
  }).filter((t): t is TimezoneOption => t !== null);
}

/**
 * Creates a DateTimeOffsetRangeDto from two Date objects
 * @param start - Start date
 * @param end - End date
 * @param timezoneId - Optional IANA timezone id. Defaults to the user's local timezone.
 * @returns DateTimeOffsetRangeDto
 */
export function createDateTimeOffsetRange(
  start: Date | null | undefined,
  end: Date | null | undefined,
  timezoneId?: string
): DateTimeOffsetRangeDto {
  return {
    start: start ? toDateTimeOffsetString(start, timezoneId) : null,
    end: end ? toDateTimeOffsetString(end, timezoneId) : null,
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

/**
 * Parses the UTC offset in minutes from an ISO 8601 string.
 * @param iso - ISO 8601 date string with timezone offset (e.g., "2024-01-15T10:00:00+05:00" or "2024-01-15T10:00:00Z")
 * @returns Offset in minutes (positive = ahead of UTC, negative = behind UTC), or null if unparseable
 */
export function parseOffsetMinutesFromIso(iso: string | null | undefined): number | null {
  if (!iso) return null;

  const match = iso.match(/([+-])(\d{2}):(\d{2})$|Z$/);
  if (!match) {
    return null;
  }

  if (iso.endsWith('Z')) {
    return 0;
  }

  const sign = match[1] === '-' ? -1 : 1;
  const hours = Number.parseInt(match[2], 10);
  const minutes = Number.parseInt(match[3], 10);
  return sign * (hours * 60 + minutes);
}

/**
 * Formats an offset in minutes to a GMT label string (e.g., "GMT+5", "GMT-7:30").
 * @param offsetMinutes - Offset in minutes from UTC
 * @returns Formatted GMT label
 */
export function toGmtLabel(offsetMinutes: number): string {
  const sign = offsetMinutes >= 0 ? '+' : '-';
  const abs = Math.abs(offsetMinutes);
  const hours = Math.floor(abs / 60);
  const minutes = abs % 60;

  if (minutes === 0) {
    return `GMT${sign}${hours}`;
  }

  return `GMT${sign}${hours}:${String(minutes).padStart(2, '0')}`;
}

/**
 * Formats a DateTimeOffset string to a localized date and time WITHOUT timezone suffix.
 * @param value - ISO 8601 date string with timezone offset
 * @returns Formatted date/time string without timezone (e.g., "Mar 2, 2026, 12:00 AM")
 */
export function formatDateTimeNoTz(value: string | null | undefined): string {
  if (!value) return '';

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return '';

  return new Intl.DateTimeFormat(undefined, {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  }).format(date);
}

/**
 * Formats a DateTimeOffset string to a localized date ONLY (e.g., "Mar 2, 2026").
 * @param value - ISO 8601 date string with timezone offset
 * @returns Formatted date string without timezone
 */
export function formatDateOnlyNoTz(value: string | null | undefined): string {
  if (!value) return '';

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return '';

  return new Intl.DateTimeFormat(undefined, {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  }).format(date);
}

/**
 * Formats a DateTimeOffset string to a localized time ONLY (e.g., "12:00 AM").
 * @param value - ISO 8601 date string with timezone offset
 * @returns Formatted time string
 */
export function formatTimeOnlyNoTz(value: string | null | undefined): string {
  if (!value) return '';

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return '';

  return new Intl.DateTimeFormat(undefined, {
    hour: '2-digit',
    minute: '2-digit',
  }).format(date);
}

/**
 * Determines the timezone suffix for a date range, shown only when different from user timezone.
 * Returns empty string if both dates are in the user's timezone, or the GMT label if different.
 * @param value - DateTimeOffsetRangeDto with start and/or end
 * @returns Timezone suffix string (e.g., " (GMT+5)" or " (GMT+5 to GMT-7)"), or empty string
 */
export function getTimezoneSuffix(value: DateTimeOffsetRangeDto | null | undefined): string {
  const start = value?.start ?? null;
  const end = value?.end ?? null;

  const startDate = start ? new Date(start) : null;
  const endDate = end ? new Date(end) : null;

  const startOriginalOffset = parseOffsetMinutesFromIso(start);
  const endOriginalOffset = parseOffsetMinutesFromIso(end);

  const startUserOffset = startDate ? -startDate.getTimezoneOffset() : null;
  const endUserOffset = endDate ? -endDate.getTimezoneOffset() : null;

  const startDifferent = startOriginalOffset !== null && startUserOffset !== null && startOriginalOffset !== startUserOffset;
  const endDifferent = endOriginalOffset !== null && endUserOffset !== null && endOriginalOffset !== endUserOffset;

  if (!startDifferent && !endDifferent) {
    return '';
  }

  if (startDifferent && endDifferent && startOriginalOffset !== null && endOriginalOffset !== null) {
    if (startOriginalOffset === endOriginalOffset) {
      return ` (${toGmtLabel(startOriginalOffset)})`;
    }

    return ` (${toGmtLabel(startOriginalOffset)} to ${toGmtLabel(endOriginalOffset)})`;
  }

  if (startDifferent && startOriginalOffset !== null) {
    return ` (${toGmtLabel(startOriginalOffset)})`;
  }

  if (endDifferent && endOriginalOffset !== null) {
    return ` (${toGmtLabel(endOriginalOffset)})`;
  }

  return '';
}

/**
 * Builds a compact formatted range string, combining date/time of same day or showing full dates.
 * Includes timezone suffix only once at the end if different from user timezone.
 * @param value - DateTimeOffsetRangeDto with start and/or end
 * @param separator - Separator between start and end (default: " - ")
 * @param timezoneSuffix - Timezone suffix from getTimezoneSuffix()
 * @returns Formatted compact range string
 */
export function buildCompactRangeValue(
  value: DateTimeOffsetRangeDto,
  separator: string = ' - ',
  timezoneSuffix: string = ''
): string {
  const start = value.start ?? null;
  const end = value.end ?? null;

  if (!start && !end) return '';

  if (start && end) {
    const startDate = new Date(start);
    const endDate = new Date(end);

    if (Number.isNaN(startDate.getTime()) || Number.isNaN(endDate.getTime())) {
      return '';
    }

    const sameDay =
      startDate.getFullYear() === endDate.getFullYear() &&
      startDate.getMonth() === endDate.getMonth() &&
      startDate.getDate() === endDate.getDate();

    if (sameDay) {
      return `${formatDateOnlyNoTz(start)} ${formatTimeOnlyNoTz(start)}${separator}${formatTimeOnlyNoTz(end)}${timezoneSuffix}`;
    }

    return `${formatDateTimeNoTz(start)}${separator}${formatDateTimeNoTz(end)}${timezoneSuffix}`;
  }

  if (start) {
    return `From ${formatDateTimeNoTz(start)}${timezoneSuffix}`;
  }

  return `Until ${formatDateTimeNoTz(end)}${timezoneSuffix}`;
}

/**
 * Calculates the duration between two dates and returns a human-readable string.
 * @param start - Start date
 * @param end - End date
 * @returns Duration string (e.g., "2 days, 5 hours, 30 minutes")
 */
export function calculateDateSpan(start: Date | null, end: Date | null): string {
  if (!start || !end || isNaN(start.getTime()) || isNaN(end.getTime())) {
    return '';
  }

  const diffMs = end.getTime() - start.getTime();
  if (diffMs === 0) return '0 minutes';

  const isNegative = diffMs < 0;
  const absDiffMs = Math.abs(diffMs);

  const days = Math.floor(absDiffMs / (1000 * 60 * 60 * 24));
  const hours = Math.floor((absDiffMs % (1000 * 60 * 60 * 24)) / (1000 * 60 * 60));
  const minutes = Math.floor((absDiffMs % (1000 * 60 * 60)) / (1000 * 60));

  const parts: string[] = [];

  if (days > 0) parts.push(`${days} day${days !== 1 ? 's' : ''}`);
  if (hours > 0) parts.push(`${hours} hour${hours !== 1 ? 's' : ''}`);
  if (minutes > 0) parts.push(`${minutes} minute${minutes !== 1 ? 's' : ''}`);

  let result = parts.join(', ');
  if (isNegative) result = `${result} (reversed)`;

  return result;
}

/**
 * Builds a full interval tooltip string showing original timezone for each date and timespan.
 * @param value - DateTimeOffsetRangeDto with start and/or end
 * @returns Tooltip label string showing both dates with original timezone info and timespan
 */
export function buildRangeIntervalTooltip(value: DateTimeOffsetRangeDto): string {
  const start = value.start ? formatDateTimeOffsetWithOriginal(value.start) : null;
  const end = value.end ? formatDateTimeOffsetWithOriginal(value.end) : null;

  let tooltip = '';
  if (start && end) {
    tooltip = `${start} - ${end}`;
    const startDate = new Date(value.start!);
    const endDate = new Date(value.end!);
    const span = calculateDateSpan(startDate, endDate);
    if (span) {
      tooltip += `\n(${span})`;
    }
  } else if (start) {
    tooltip = `From ${start}`;
  } else if (end) {
    tooltip = `Until ${end}`;
  }

  return tooltip;
}

/**
 * Formats a date string (YYYY-MM-DD ISO format) to a localized date string.
 * @param dateStr - ISO date string (e.g., "2024-01-15")
 * @returns Formatted date string in user's locale (e.g., "Jan 15, 2024")
 */
export function formatDate(dateStr: string | null | undefined): string {
  if (!dateStr) return '';

  try {
    const date = new Date(dateStr + 'T00:00:00Z');
    if (Number.isNaN(date.getTime())) return '';

    return new Intl.DateTimeFormat(undefined, {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
    }).format(date);
  } catch {
    return '';
  }
}

/**
 * Builds a compact formatted date range string.
 * @param value - DateRangeDto with start and/or end
 * @param separator - Separator between start and end (default: " - ")
 * @returns Formatted compact date range string
 */
export function buildCompactDateRangeValue(
  value: DateRangeDto,
  separator: string = ' - '
): string {
  const start = value.start ?? null;
  const end = value.end ?? null;

  if (!start && !end) return '';

  if (start && end) {
    return `${formatDate(start)}${separator}${formatDate(end)}`;
  }

  if (start) {
    return `From ${formatDate(start)}`;
  }

  return `Until ${formatDate(end)}`;
}

/**
 * Builds a date range tooltip string showing timespan.
 * @param value - DateRangeDto with start and/or end
 * @returns Tooltip label string showing dates and timespan
 */
export function buildDateRangeTooltip(value: DateRangeDto): string {
  const start = value.start ? formatDate(value.start) : null;
  const end = value.end ? formatDate(value.end) : null;

  let tooltip = '';
  if (start && end) {
    tooltip = `${start} - ${end}`;
    try {
      const startDate = new Date(value.start! + 'T00:00:00Z');
      const endDate = new Date(value.end! + 'T00:00:00Z');
      const span = calculateDateSpan(startDate, endDate);
      if (span) {
        tooltip += `\n(${span})`;
      }
    } catch {
      // Silently ignore if date parsing fails
    }
  } else if (start) {
    tooltip = `From ${start}`;
  } else if (end) {
    tooltip = `Until ${end}`;
  }

  return tooltip;
}
