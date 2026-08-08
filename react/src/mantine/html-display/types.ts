import type { CSSProperties } from 'react';

export interface HtmlDisplayProps {
  /** The HTML content to render (from a rich-text/HtmlContent field) */
  html: string;
  /** Custom className for consumer-owned styling */
  className?: string;
  /** Inline style overrides */
  style?: CSSProperties;
}
