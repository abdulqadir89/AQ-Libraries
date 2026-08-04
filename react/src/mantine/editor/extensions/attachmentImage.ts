import { Node, mergeAttributes } from '@tiptap/core';
import { ReactNodeViewRenderer } from '@tiptap/react';
import type { AttachmentRef } from '../types';
import { AttachmentImageView } from './AttachmentImageView';

export interface AttachmentImageOptions {
  onUploadFile: (file: File) => Promise<AttachmentRef>;
  fetchAuthenticated: (url: string) => Promise<Response>;
  onError: (err: unknown) => void;
  pendingFiles: Map<string, File>;
}

declare module '@tiptap/core' {
  interface Commands<ReturnType> {
    attachmentImage: {
      uploadAttachmentImage: (file: File) => ReturnType;
    };
  }
}

export const AttachmentImage = Node.create<AttachmentImageOptions>({
  name: 'attachmentImage',
  group: 'block',
  atom: true,
  draggable: true,

  addOptions() {
    return {
      onUploadFile: () => Promise.reject(new Error('onUploadFile not configured')),
      fetchAuthenticated: () => Promise.reject(new Error('fetchAuthenticated not configured')),
      onError: () => {},
      pendingFiles: new Map(),
    };
  },

  addAttributes() {
    return {
      src: { default: null },
      alt: { default: '' },
      uploadId: { default: null },
    };
  },

  parseHTML() {
    return [{ tag: 'img[src]' }];
  },

  renderHTML({ HTMLAttributes, node }) {
    return [
      'img',
      mergeAttributes(HTMLAttributes, {
        src: node.attrs.src,
        alt: node.attrs.alt,
      }),
    ];
  },

  addNodeView() {
    return ReactNodeViewRenderer(AttachmentImageView);
  },

  addCommands() {
    return {
      uploadAttachmentImage:
        (file: File) =>
        ({ commands }) => {
          const uploadId = crypto.randomUUID();
          this.options.pendingFiles.set(uploadId, file);
          return commands.insertContent({
            type: this.name,
            attrs: { src: null, alt: file.name, uploadId },
          });
        },
    };
  },
});
