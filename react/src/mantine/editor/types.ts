import type { Editor } from '@tiptap/core';

export interface AttachmentRef {
  id?: string;
  fileName: string;
  contentType: string;
  size: number;
  url: string;
}

export interface RichTextEditorProps {
  value: string;
  onChange: (value: string) => void;
  /** Persistence format for `value`/`onChange` and content loading. Defaults to 'html'. */
  contentFormat?: 'html' | 'markdown';
  label?: string;
  description?: string;
  placeholder?: string;
  error?: string;
  required?: boolean;
  minHeight?: number;
  /** Fixed height of the editing area; content scrolls inside. Overrides `minHeight`. */
  height?: number;
  /** Renders the content without toolbar and blocks editing. */
  readOnly?: boolean;
  /** Called once the underlying Tiptap editor exists, for commands the component does not wrap (e.g. insertContent, selection). */
  onEditorReady?: (editor: Editor) => void;
  /** Called when the selection changes; `hasSelection` is true for a non-empty range. */
  onSelectionChange?: (hasSelection: boolean) => void;
  /** Omit to disable inline image/file upload buttons (e.g. when no attachment entity backs this content). */
  onUploadFile?: (file: File) => Promise<AttachmentRef>;
  /** Required only when onUploadFile is provided, to render authenticated image previews. */
  fetchAuthenticated?: (url: string) => Promise<Response>;
  onError: (err: unknown) => void;
}
