/** A rectangle on a page, as fractions (0 to 1) of the page width and height, so it holds at any zoom. */
export interface PdfRect {
  x: number;
  y: number;
  width: number;
  height: number;
}

export interface PdfHighlight {
  id: string;
  /** 1-based page number. */
  page: number;
  rects: PdfRect[];
  kind?: 'text' | 'area';
}

/** What the viewer reports when the user selects text or draws an area. */
export interface PdfSelection {
  page: number;
  rects: PdfRect[];
  kind: 'text' | 'area';
  /** The selected text; empty for an area. */
  quote: string;
}
