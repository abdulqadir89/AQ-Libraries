'use client';

import { createContext, useContext, useMemo } from 'react';
import type { ReactNode } from 'react';
import { defaultMessages } from './defaultMessages';
import { deepMerge } from './deepMerge';
import type { AQMessages, DeepPartial } from './types';

export interface AQLocaleContextValue {
  locale?: string;
  messages: AQMessages;
}

const AQLocaleContext = createContext<AQLocaleContextValue | null>(null);

export interface AQLocaleProviderProps {
  /**
   * BCP-47 locale tag passed through to Intl-based formatting inside the
   * library (dates, money, region names). Undefined means "browser default",
   * matching the library's pre-Phase-4 behaviour.
   */
  locale?: string;
  /** Partial message overrides, deep-merged over `defaultMessages`. */
  messages?: DeepPartial<AQMessages>;
  children: ReactNode;
}

export function AQLocaleProvider({ locale, messages, children }: AQLocaleProviderProps) {
  const mergedMessages = useMemo(() => deepMerge(defaultMessages, messages), [messages]);

  const value = useMemo<AQLocaleContextValue>(
    () => ({ locale, messages: mergedMessages }),
    [locale, mergedMessages]
  );

  return <AQLocaleContext.Provider value={value}>{children}</AQLocaleContext.Provider>;
}

/**
 * Reads the active AQ locale/messages. Works without a provider — returns
 * `{ locale: undefined, messages: defaultMessages }` — so other consumers of
 * @AQ/react-components that don't wrap their tree in `AQLocaleProvider` are
 * unaffected.
 */
export function useAQLocale(): AQLocaleContextValue {
  const ctx = useContext(AQLocaleContext);
  if (ctx) return ctx;
  return { locale: undefined, messages: defaultMessages };
}
