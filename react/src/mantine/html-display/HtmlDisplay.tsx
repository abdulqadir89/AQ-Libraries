import { useEffect, useRef } from 'react';
import katex from 'katex';
import 'katex/dist/katex.min.css';
import type { HtmlDisplayProps } from './types';

export function HtmlDisplay({ html, className, style }: HtmlDisplayProps) {
  const contentRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const container = contentRef.current;
    if (!container) return;

    const mathNodes = container.querySelectorAll<HTMLElement>('[data-type="inline-math"], [data-type="block-math"]');
    mathNodes.forEach((node) => {
      const latex = node.getAttribute('data-latex');
      if (!latex) return;
      katex.render(latex, node, {
        throwOnError: false,
        displayMode: node.getAttribute('data-type') === 'block-math',
      });
    });
  }, [html]);

  return (
    <div
      ref={contentRef}
      dangerouslySetInnerHTML={{ __html: html }}
      className={className}
      style={style}
    />
  );
}

export default HtmlDisplay;
