import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { getDocument, GlobalWorkerOptions, type PDFDocumentProxy, type PDFPageProxy } from 'pdfjs-dist';
import 'pdfjs-dist/web/pdf_viewer.css';
import { ActionIcon, Box, Group, Loader, SegmentedControl, Stack, Text, Tooltip } from '@mantine/core';
import { IconArrowsHorizontal, IconZoomIn, IconZoomOut } from '@tabler/icons-react';
import { useAQLocale } from '../locale';
import { PdfPageView, type PdfSelectionMode } from './PdfPageView';
import type { PdfHighlight, PdfSelection } from './types';

const MIN_SCALE = 0.5;
const MAX_SCALE = 3;
const SCALE_STEP = 0.25;
const PAGE_GAP = 12;
const PAGE_PADDING = 16;

export interface PdfViewerProps {
  /** A URL the browser can fetch, or the file bytes (fetch an authenticated file yourself and pass its bytes). */
  source: string | Uint8Array;
  /** URL of the pdf.js worker; the app supplies it, e.g. `import worker from 'pdfjs-dist/build/pdf.worker.min.mjs?url'`. */
  workerSrc: string;
  highlights?: PdfHighlight[];
  selectedId?: string | null;
  onSelect?: (id: string) => void;
  /** Called when the user confirms a text selection or a drawn area. Highlights are never created without it. */
  onCreateHighlight?: (selection: PdfSelection) => void;
  /** A read-only viewer shows highlights but cannot create them. */
  readOnly?: boolean;
  height?: number | string;
  onError?: (error: unknown) => void;
}

export function PdfViewer({
  source, workerSrc, highlights = [], selectedId, onSelect, onCreateHighlight, readOnly, height = '100%', onError,
}: PdfViewerProps) {
  const { messages } = useAQLocale();
  const t = messages.pdfViewer;
  const scrollRef = useRef<HTMLDivElement | null>(null);
  const pageElements = useRef(new Map<number, HTMLDivElement>());
  const [pdf, setPdf] = useState<PDFDocumentProxy | null>(null);
  const [pages, setPages] = useState<PDFPageProxy[]>([]);
  const [failed, setFailed] = useState(false);
  const [scale, setScale] = useState(1);
  const [mode, setMode] = useState<PdfSelectionMode>('text');
  const [currentPage, setCurrentPage] = useState(1);

  // RULE RCT-15
  GlobalWorkerOptions.workerSrc = workerSrc;

  useEffect(() => {
    let cancelled = false;
    setPdf(null);
    setPages([]);
    setFailed(false);
    const task = typeof source === 'string' ? getDocument({ url: source }) : getDocument({ data: source.slice() });
    task.promise
      .then(async (document) => {
        const loaded = await Promise.all(Array.from({ length: document.numPages }, (_, i) => document.getPage(i + 1)));
        if (cancelled) return;
        setPdf(document);
        setPages(loaded);
        const width = scrollRef.current?.clientWidth;
        if (width && loaded.length > 0) {
          const natural = loaded[0].getViewport({ scale: 1 }).width;
          setScale(Math.min(MAX_SCALE, Math.max(MIN_SCALE, (width - PAGE_PADDING * 2) / natural)));
        }
      })
      .catch((error: unknown) => {
        if (cancelled) return;
        setFailed(true);
        onError?.(error);
      });
    return () => {
      cancelled = true;
      task.destroy();
    };
  }, [source, onError]);

  const registerElement = useCallback((pageNumber: number, element: HTMLDivElement | null) => {
    if (element) pageElements.current.set(pageNumber, element);
    else pageElements.current.delete(pageNumber);
  }, []);

  const byPage = useMemo(() => {
    const map = new Map<number, PdfHighlight[]>();
    for (const highlight of highlights) {
      const list = map.get(highlight.page) ?? [];
      list.push(highlight);
      map.set(highlight.page, list);
    }
    return map;
  }, [highlights]);

  useEffect(() => {
    if (!selectedId) return;
    const highlight = highlights.find((h) => h.id === selectedId);
    const element = highlight ? pageElements.current.get(highlight.page) : undefined;
    const scroller = scrollRef.current;
    if (!highlight || !element || !scroller || highlight.rects.length === 0) return;
    const top = element.offsetTop + highlight.rects[0].y * element.offsetHeight - 80;
    scroller.scrollTo({ top: Math.max(0, top), behavior: 'smooth' });
    // eslint-disable-next-line react-hooks/exhaustive-deps -- scroll only when the selection changes, not when highlights are re-created
  }, [selectedId]);

  const handleScroll = () => {
    const scroller = scrollRef.current;
    if (!scroller) return;
    const middle = scroller.scrollTop + scroller.clientHeight / 2;
    let found = 1;
    for (const [number, element] of pageElements.current) {
      if (element.offsetTop <= middle) found = Math.max(found, number);
    }
    setCurrentPage(found);
  };

  // RULE RCT-16
  const canCreate = !readOnly && !!onCreateHighlight;

  const fitWidth = () => {
    const width = scrollRef.current?.clientWidth;
    if (!width || pages.length === 0) return;
    const natural = pages[0].getViewport({ scale: 1 }).width;
    setScale(Math.min(MAX_SCALE, Math.max(MIN_SCALE, (width - PAGE_PADDING * 2) / natural)));
  };

  return (
    <Stack gap={0} style={{ height, minHeight: 0 }}>
      <Group justify="space-between" p="xs" wrap="nowrap" style={{ borderBottom: '1px solid var(--mantine-color-default-border)' }}>
        <Text size="sm" c="dimmed">{pdf ? t.pageOf(currentPage, pdf.numPages) : ''}</Text>
        <Group gap="xs" wrap="nowrap">
          {canCreate && (
            <SegmentedControl
              size="xs"
              value={mode}
              onChange={(value) => setMode(value as PdfSelectionMode)}
              data={[{ value: 'text', label: t.selectText }, { value: 'area', label: t.selectArea }]}
            />
          )}
          <Tooltip label={t.zoomOut}>
            <ActionIcon variant="subtle" onClick={() => setScale((s) => Math.max(MIN_SCALE, s - SCALE_STEP))} aria-label={t.zoomOut}>
              <IconZoomOut size={16} />
            </ActionIcon>
          </Tooltip>
          <Text size="xs" w={40} ta="center">{Math.round(scale * 100)}%</Text>
          <Tooltip label={t.zoomIn}>
            <ActionIcon variant="subtle" onClick={() => setScale((s) => Math.min(MAX_SCALE, s + SCALE_STEP))} aria-label={t.zoomIn}>
              <IconZoomIn size={16} />
            </ActionIcon>
          </Tooltip>
          <Tooltip label={t.fitWidth}>
            <ActionIcon variant="subtle" onClick={fitWidth} aria-label={t.fitWidth}>
              <IconArrowsHorizontal size={16} />
            </ActionIcon>
          </Tooltip>
        </Group>
      </Group>

      <Box
        ref={scrollRef}
        onScroll={handleScroll}
        style={{ flex: 1, minHeight: 0, overflow: 'auto', background: 'var(--mantine-color-gray-light)', padding: PAGE_PADDING }}
      >
        {failed ? (
          <Text c="red" size="sm">{t.loadFailed}</Text>
        ) : !pdf ? (
          <Group gap="xs"><Loader size="xs" /><Text size="sm" c="dimmed">{t.loading}</Text></Group>
        ) : (
          <Stack gap={PAGE_GAP} align="center" style={{ width: 'fit-content', minWidth: '100%' }}>
            {pages.map((page, index) => (
              <PdfPageView
                key={index + 1}
                page={page}
                pageNumber={index + 1}
                scale={scale}
                highlights={byPage.get(index + 1) ?? []}
                selectedId={selectedId}
                mode={mode}
                canCreate={canCreate}
                onSelect={onSelect}
                onCreate={onCreateHighlight}
                registerElement={registerElement}
              />
            ))}
          </Stack>
        )}
      </Box>
    </Stack>
  );
}
