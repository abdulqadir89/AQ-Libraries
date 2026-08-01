export interface AttachmentRef {
  id?: string;
  fileName: string;
  contentType: string;
  size: number;
  url: string;
}

export interface RichMarkdownEditorProps {
  value: string;
  onChange: (value: string) => void;
  label?: string;
  description?: string;
  placeholder?: string;
  error?: string;
  required?: boolean;
  minHeight?: number;
  /** Omit to disable inline image/file upload buttons (e.g. when no attachment entity backs this content). */
  onUploadFile?: (file: File) => Promise<AttachmentRef>;
  /** Required only when onUploadFile is provided, to render authenticated image previews. */
  fetchAuthenticated?: (url: string) => Promise<Response>;
  onError: (err: unknown) => void;
}
