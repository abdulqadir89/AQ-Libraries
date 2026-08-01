'use client';

import { useEffect, useMemo, useRef } from 'react';
import { useEditor, EditorContent } from '@tiptap/react';
import { StarterKit } from '@tiptap/starter-kit';
import { Markdown } from '@tiptap/markdown';
import {
  ActionIcon, Divider, FileButton, Group, Paper, Tooltip,
} from '@mantine/core';
import { modals } from '@mantine/modals';
import {
  IconBold, IconFile, IconItalic, IconLink, IconMovie, IconMusic, IconPhoto,
} from '@tabler/icons-react';
import { VideoEmbed } from './extensions/videoEmbed';
import { AudioEmbed } from './extensions/audioEmbed';
import { AttachmentImage } from './extensions/attachmentImage';
import type { RichMarkdownEditorProps } from './types';

function promptForUrl(title: string, onSubmit: (url: string) => void) {
  let url = '';
  modals.open({
    title,
    children: (
      <input
        type="url"
        placeholder="https://..."
        autoFocus
        style={{ width: '100%', padding: 8 }}
        onChange={(e) => { url = e.currentTarget.value; }}
        onKeyDown={(e) => {
          if (e.key === 'Enter' && url) {
            modals.closeAll();
            onSubmit(url);
          }
        }}
      />
    ),
  });
}

export function RichMarkdownEditor({
  value,
  onChange,
  label,
  description,
  placeholder,
  error,
  required,
  minHeight = 300,
  onUploadFile,
  fetchAuthenticated,
  onError,
}: RichMarkdownEditorProps) {
  const pendingImageFiles = useMemo(() => new Map<string, File>(), []);
  const lastEmittedValue = useRef(value);

  const editor = useEditor({
    immediatelyRender: false,
    extensions: [
      StarterKit.configure({ link: { openOnClick: false } }),
      Markdown,
      VideoEmbed,
      AudioEmbed,
      ...(onUploadFile && fetchAuthenticated
        ? [AttachmentImage.configure({ onUploadFile, fetchAuthenticated, onError, pendingFiles: pendingImageFiles })]
        : []),
    ],
    content: value,
    editorProps: {
      attributes: {
        'data-placeholder': placeholder ?? '',
      },
    },
    onUpdate({ editor: currentEditor }) {
      const markdown = currentEditor.getMarkdown();
      lastEmittedValue.current = markdown;
      onChange(markdown);
    },
  }, []);

  useEffect(() => {
    if (!editor) return;
    if (value !== lastEmittedValue.current) {
      lastEmittedValue.current = value;
      // setContent uses flushSync internally; defer outside React's commit phase
      // to avoid "flushSync called from inside a lifecycle method".
      queueMicrotask(() => {
        if (!editor.isDestroyed) editor.commands.setContent(value, { contentType: 'markdown', emitUpdate: false });
      });
    }
  }, [editor, value]);

  if (!editor) return null;

  const handleImageFile = (file: File | null) => {
    if (!file || !editor.commands.uploadAttachmentImage) return;
    editor.chain().focus().uploadAttachmentImage(file).run();
  };

  const handleFileAttachment = (file: File | null) => {
    if (!file || !onUploadFile) return;
    onUploadFile(file)
      .then((ref) => {
        editor
          .chain()
          .focus()
          .insertContent({
            type: 'text',
            text: ref.fileName,
            marks: [{ type: 'link', attrs: { href: ref.url } }],
          })
          .run();
      })
      .catch((err) => onError(err));
  };

  return (
    <div>
      {label && (
        <label
          style={{
            display: 'block',
            fontSize: 'var(--mantine-font-size-sm)',
            fontWeight: 500,
            marginBottom: 4,
            color: error ? 'var(--mantine-color-red-7)' : undefined,
          }}
        >
          {label}
          {required && <span style={{ color: 'var(--mantine-color-red-7)', marginLeft: 4 }}>*</span>}
        </label>
      )}
      {description && (
        <p
          style={{
            fontSize: 'var(--mantine-font-size-xs)',
            color: 'var(--mantine-color-dimmed)',
            marginBottom: 6,
            marginTop: 0,
          }}
        >
          {description}
        </p>
      )}

      <Paper
        withBorder
        style={{ borderColor: error ? 'var(--mantine-color-red-7)' : undefined }}
      >
        <Group gap={2} p={4} style={{ borderBottom: '1px solid var(--mantine-color-default-border)' }}>
          <Tooltip label="Bold" withArrow>
            <ActionIcon variant="subtle" onClick={() => editor.chain().focus().toggleBold().run()}>
              <IconBold size={16} />
            </ActionIcon>
          </Tooltip>
          <Tooltip label="Italic" withArrow>
            <ActionIcon variant="subtle" onClick={() => editor.chain().focus().toggleItalic().run()}>
              <IconItalic size={16} />
            </ActionIcon>
          </Tooltip>
          <Tooltip label="Link" withArrow>
            <ActionIcon
              variant="subtle"
              onClick={() => promptForUrl('Insert link', (url) => editor.chain().focus().setLink({ href: url }).run())}
            >
              <IconLink size={16} />
            </ActionIcon>
          </Tooltip>

          <Divider orientation="vertical" />

          <Tooltip label="Insert video (YouTube URL)" withArrow>
            <ActionIcon
              variant="subtle"
              onClick={() => promptForUrl('Insert video', (url) => editor.chain().focus().setVideoEmbed({ url }).run())}
            >
              <IconMovie size={16} />
            </ActionIcon>
          </Tooltip>
          <Tooltip label="Insert audio (URL)" withArrow>
            <ActionIcon
              variant="subtle"
              onClick={() => promptForUrl('Insert audio', (url) => editor.chain().focus().setAudioEmbed({ url }).run())}
            >
              <IconMusic size={16} />
            </ActionIcon>
          </Tooltip>

          {onUploadFile && (
            <>
              <Divider orientation="vertical" />

              {fetchAuthenticated && (
                <FileButton onChange={handleImageFile} accept="image/*">
                  {(props) => (
                    <Tooltip label="Upload image" withArrow>
                      <ActionIcon variant="subtle" {...props}>
                        <IconPhoto size={16} />
                      </ActionIcon>
                    </Tooltip>
                  )}
                </FileButton>
              )}
              <FileButton onChange={handleFileAttachment}>
                {(props) => (
                  <Tooltip label="Upload file" withArrow>
                    <ActionIcon variant="subtle" {...props}>
                      <IconFile size={16} />
                    </ActionIcon>
                  </Tooltip>
                )}
              </FileButton>
            </>
          )}
        </Group>

        <div style={{ minHeight, padding: 8 }}>
          <EditorContent editor={editor} />
        </div>
      </Paper>

      <style>{`
        .tiptap.ProseMirror {
          outline: none;
          white-space: pre-wrap;
          word-wrap: break-word;
          min-height: ${minHeight - 16}px;
        }
        .tiptap.ProseMirror p {
          margin: 0 0 0.75em 0;
        }
        .tiptap.ProseMirror p:last-child {
          margin-bottom: 0;
        }
        .tiptap.ProseMirror img {
          max-width: 100%;
        }
      `}</style>

      {error && (
        <p
          style={{
            fontSize: 'var(--mantine-font-size-xs)',
            color: 'var(--mantine-color-red-7)',
            marginTop: 4,
            marginBottom: 0,
          }}
        >
          {error}
        </p>
      )}
    </div>
  );
}
