import { useEffect, useRef, useState } from 'react';
import type { PointerEvent as ReactPointerEvent } from 'react';
import { TextLayer, type PDFPageProxy } from 'pdfjs-dist';
import { Box, Button } from '@mantine/core';
import { IconMessagePlus } from '@tabler/icons-react';
import { useAQLocale } from '../locale';
import type { PdfHighlight, PdfRect, PdfSelection } from './types';

export type PdfSelectionMode = 'text' | 'area';

const MIN_AREA = 0.01;

interface PendingSelection {
  selection: PdfSelection;
  anchor: { x: number; y: number };
}

export interface PdfPageViewProps {
  page: PDFPageProxy;
  pageNumber: number;
  scale: number;
  highlights: PdfHighlight[];
  selectedId?: string | null;
  mode: PdfSelectionMode;
  canCreate: boolean;
  onSelect?: (id: string) => void;
  onCreate?: (selection: PdfSelection) => void;
  registerElement: (pageNumber: number, element: HTMLDivElement | null) => void;
}

function toFractions(rect: DOMRect, bounds: DOMRect): PdfRect {
  return {
    x: (rect.left - bounds.left) / bounds.width,
    y: (rect.top - bounds.top) / bounds.height,
    width: rect.width / bounds.width,
    height: rect.height / bounds.height,
  };
}

function contains(rect: PdfRect, x: number, y: number): boolean {
  return x >= rect.x && x <= rect.x + rect.width && y >= rect.y && y <= rect.y + rect.height;
}

function rectStyle(rect: PdfRect) {
  return {
    position: 'absolute' as const,
    left: `${rect.x * 100}%`,
    top: `${rect.y * 100}%`,
    width: `${rect.width * 100}%`,
    height: `${rect.height * 100}%`,
  };
}

export function PdfPageView({
  page, pageNumber, scale, highlights, selectedId, mode, canCreate, onSelect, onCreate, registerElement,
}: PdfPageViewProps) {
  const { messages } = useAQLocale();
  const t = messages.pdfViewer;
  const wrapperRef = useRef<HTMLDivElement | null>(null);
  const canvasRef = useRef<HTMLCanvasElement | null>(null);
  const textRef = useRef<HTMLDivElement | null>(null);
  const [visible, setVisible] = useState(false);
  const [pending, setPending] = useState<PendingSelection | null>(null);
  const [drawing, setDrawing] = useState<{ startX: number; startY: number; rect: PdfRect } | null>(null);

  const viewport = page.getViewport({ scale });

  useEffect(() => {
    const element = wrapperRef.current;
    if (!element) return undefined;
    const observer = new IntersectionObserver(
      (entries) => { if (entries.length > 0) setVisible(entries[entries.length - 1].isIntersecting); },
      { rootMargin: '800px 0px' },
    );
    observer.observe(element);
    return () => observer.disconnect();
  }, []);

  useEffect(() => {
    const canvas = canvasRef.current;
    const textContainer = textRef.current;
    if (!canvas || !textContainer) return undefined;

    // A page far from the viewport gives its bitmap and text back; the wrapper keeps its size, so nothing moves
    if (!visible) {
      canvas.width = 0;
      canvas.height = 0;
      textContainer.replaceChildren();
      return undefined;
    }

    const renderViewport = page.getViewport({ scale });
    const ratio = window.devicePixelRatio || 1;
    canvas.width = Math.floor(renderViewport.width * ratio);
    canvas.height = Math.floor(renderViewport.height * ratio);

    const renderTask = page.render({
      canvas,
      viewport: renderViewport,
      transform: ratio === 1 ? undefined : [ratio, 0, 0, ratio, 0, 0],
    });
    renderTask.promise.catch(() => undefined);

    textContainer.replaceChildren();
    const textLayer = new TextLayer({
      textContentSource: page.streamTextContent(),
      container: textContainer,
      viewport: renderViewport,
    });
    textLayer.render().catch(() => undefined);

    return () => {
      renderTask.cancel();
      textLayer.cancel();
    };
  }, [visible, page, scale]);

  useEffect(() => { setPending(null); }, [mode, scale]);

  const point = (event: { clientX: number; clientY: number }) => {
    const bounds = wrapperRef.current!.getBoundingClientRect();
    return {
      bounds,
      x: (event.clientX - bounds.left) / bounds.width,
      y: (event.clientY - bounds.top) / bounds.height,
    };
  };

  const handleTextMouseUp = (event: MouseEvent) => {
    if (!canCreate || mode !== 'text') return;
    if ((event.target as Element | null)?.closest?.('[data-pdf-pending]')) return;
    const selection = window.getSelection();
    const textContainer = textRef.current;
    if (!selection || selection.isCollapsed) {
      setPending((current) => (current?.selection.kind === 'text' ? null : current));
      return;
    }
    if (!textContainer || selection.rangeCount === 0) return;
    const range = selection.getRangeAt(0);
    if (!textContainer.contains(range.startContainer)) return;

    // A triple click or a drag past the end of the page ends the range in a later block; keep only this page's part
    const pageRange = range.cloneRange();
    if (!textContainer.contains(range.endContainer) && textContainer.lastChild) {
      pageRange.setEndAfter(textContainer.lastChild);
    }

    const bounds = wrapperRef.current!.getBoundingClientRect();
    const rects = Array.from(pageRange.getClientRects())
      .filter((r) => r.width > 1 && r.height > 1)
      .map((r) => toFractions(r, bounds));
    const quote = pageRange.toString().trim();
    if (rects.length === 0 || !quote) return;

    const last = rects[rects.length - 1];
    setPending({
      selection: { page: pageNumber, rects, kind: 'text', quote },
      anchor: { x: Math.min(last.x + last.width, 0.85), y: last.y + last.height },
    });
  };

  // The mouse can be released on another page than the one the selection started on, so every page listens on the document
  const textMouseUpRef = useRef(handleTextMouseUp);
  useEffect(() => { textMouseUpRef.current = handleTextMouseUp; });
  useEffect(() => {
    const listener = (event: MouseEvent) => textMouseUpRef.current(event);
    document.addEventListener('mouseup', listener);
    return () => document.removeEventListener('mouseup', listener);
  }, []);

  const handleClick = (event: React.MouseEvent) => {
    if (!onSelect) return;
    if (window.getSelection()?.isCollapsed === false) return;
    const { x, y } = point(event);
    const hit = highlights
      .filter((h) => h.rects.some((r) => contains(r, x, y)))
      .sort((a, b) => area(a) - area(b))[0];
    if (hit) onSelect(hit.id);
  };

  const handleAreaDown = (event: ReactPointerEvent<HTMLDivElement>) => {
    if (!canCreate || mode !== 'area') return;
    event.currentTarget.setPointerCapture(event.pointerId);
    const { x, y } = point(event);
    setPending(null);
    setDrawing({ startX: x, startY: y, rect: { x, y, width: 0, height: 0 } });
  };

  const handleAreaMove = (event: ReactPointerEvent<HTMLDivElement>) => {
    if (!drawing) return;
    const raw = point(event);
    const x = Math.min(1, Math.max(0, raw.x));
    const y = Math.min(1, Math.max(0, raw.y));
    setDrawing({
      ...drawing,
      rect: {
        x: Math.min(drawing.startX, x),
        y: Math.min(drawing.startY, y),
        width: Math.abs(x - drawing.startX),
        height: Math.abs(y - drawing.startY),
      },
    });
  };

  const handleAreaUp = () => {
    if (!drawing) return;
    const { rect } = drawing;
    setDrawing(null);
    if (rect.width < MIN_AREA || rect.height < MIN_AREA) return;
    setPending({
      selection: { page: pageNumber, rects: [rect], kind: 'area', quote: '' },
      anchor: { x: Math.min(rect.x + rect.width, 0.85), y: rect.y + rect.height },
    });
  };

  const confirm = () => {
    if (!pending) return;
    onCreate?.(pending.selection);
    setPending(null);
    window.getSelection()?.removeAllRanges();
  };

  return (
    <Box
      ref={(element: HTMLDivElement | null) => {
        wrapperRef.current = element;
        registerElement(pageNumber, element);
      }}
      onClick={handleClick}
      style={{
        position: 'relative',
        width: viewport.width,
        height: viewport.height,
        background: 'white',
        boxShadow: 'var(--mantine-shadow-sm)',
        ['--scale-factor' as string]: scale,
        ['--total-scale-factor' as string]: scale,
        ['--scale-round-x' as string]: '1px',
        ['--scale-round-y' as string]: '1px',
      }}
    >
      <canvas ref={canvasRef} style={{ width: viewport.width, height: viewport.height, display: 'block' }} />
      <div
        ref={textRef}
        className="textLayer"
        style={{ position: 'absolute', inset: 0, width: viewport.width, height: viewport.height }}
      />

      {highlights.map((highlight) => highlight.rects.map((rect, index) => (
        <div
          key={`${highlight.id}-${index}`}
          style={{
            ...rectStyle(rect),
            pointerEvents: 'none',
            mixBlendMode: 'multiply',
            background: highlight.id === selectedId ? 'rgba(255, 196, 0, 0.55)' : 'rgba(255, 224, 102, 0.4)',
            outline: highlight.kind === 'area' ? '2px solid rgba(240, 140, 0, 0.8)' : undefined,
          }}
        />
      )))}

      {drawing && <div style={{ ...rectStyle(drawing.rect), border: '2px dashed var(--mantine-primary-color-filled)', pointerEvents: 'none' }} />}
      {pending?.selection.kind === 'area' && (
        <div style={{ ...rectStyle(pending.selection.rects[0]), border: '2px dashed var(--mantine-primary-color-filled)', pointerEvents: 'none' }} />
      )}

      {canCreate && mode === 'area' && (
        <div
          onPointerDown={handleAreaDown}
          onPointerMove={handleAreaMove}
          onPointerUp={handleAreaUp}
          style={{ position: 'absolute', inset: 0, cursor: 'crosshair', touchAction: 'none' }}
        />
      )}

      {pending && (
        <Button
          data-pdf-pending
          size="compact-xs"
          leftSection={<IconMessagePlus size={14} />}
          onClick={(event) => { event.stopPropagation(); confirm(); }}
          style={{ position: 'absolute', left: `${pending.anchor.x * 100}%`, top: `${pending.anchor.y * 100}%`, zIndex: 5 }}
        >
          {t.addComment}
        </Button>
      )}
    </Box>
  );
}

function area(highlight: PdfHighlight): number {
  return highlight.rects.reduce((sum, r) => sum + r.width * r.height, 0);
}
