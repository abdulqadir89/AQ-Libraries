'use client';

import { useEffect, useMemo, useRef } from 'react';
import { useEditor, EditorContent } from '@tiptap/react';
import { StarterKit } from '@tiptap/starter-kit';
import { Audio } from '@tiptap/extension-audio';
import { Youtube } from '@tiptap/extension-youtube';
import { Mathematics } from '@tiptap/extension-mathematics';
import { Underline } from '@tiptap/extension-underline';
import { Subscript } from '@tiptap/extension-subscript';
import { Superscript } from '@tiptap/extension-superscript';
import { Highlight } from '@tiptap/extension-highlight';
import { TableKit } from '@tiptap/extension-table';
import { TaskList, TaskItem } from '@tiptap/extension-list';
import { TextAlign } from '@tiptap/extension-text-align';
import { Markdown } from '@tiptap/markdown';
import {
  ActionIcon, Divider, FileButton, Group, Menu, Paper, Tooltip,
} from '@mantine/core';
import { modals } from '@mantine/modals';
import {
  IconAlignCenter, IconAlignJustified, IconAlignLeft, IconAlignRight,
  IconBold, IconFile, IconH1, IconH2, IconH3, IconH4, IconHeading,
  IconHighlight, IconItalic, IconLink, IconList, IconListCheck,
  IconListNumbers, IconMathFunction, IconMovie, IconMusic, IconPhoto,
  IconSubscript, IconSuperscript, IconTable, IconUnderline,
} from '@tabler/icons-react';
import { AttachmentImage } from './extensions/attachmentImage';
import type { RichTextEditorProps } from './types';
import { useAQLocale } from '../locale';
import 'katex/dist/katex.min.css';

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

function promptForText(title: string, placeholder: string, onSubmit: (text: string) => void) {
  let text = '';
  modals.open({
    title,
    children: (
      <input
        type="text"
        placeholder={placeholder}
        autoFocus
        style={{ width: '100%', padding: 8 }}
        onChange={(e) => { text = e.currentTarget.value; }}
        onKeyDown={(e) => {
          if (e.key === 'Enter' && text) {
            modals.closeAll();
            onSubmit(text);
          }
        }}
      />
    ),
  });
}

export function RichTextEditor({
  value,
  onChange,
  contentFormat = 'html',
  label,
  description,
  placeholder,
  error,
  required,
  minHeight = 300,
  onUploadFile,
  fetchAuthenticated,
  onError,
}: RichTextEditorProps) {
  const { messages: aqMessages } = useAQLocale();
  const m = aqMessages.richTextEditor;
  const pendingImageFiles = useMemo(() => new Map<string, File>(), []);
  const lastEmittedValue = useRef(value);

  const editor = useEditor({
    immediatelyRender: false,
    extensions: [
      StarterKit.configure({ link: { openOnClick: false }, underline: false }),
      Youtube,
      Audio.configure({ addPasteHandler: true }),
      Mathematics,
      Underline,
      Subscript,
      Superscript,
      Highlight.configure({ multicolor: false }),
      TableKit.configure({ table: { resizable: true } }),
      TaskList,
      TaskItem.configure({ nested: true }),
      TextAlign.configure({ types: ['heading', 'paragraph'] }),
      Markdown,
      ...(onUploadFile && fetchAuthenticated
        ? [AttachmentImage.configure({ onUploadFile, fetchAuthenticated, onError, pendingFiles: pendingImageFiles })]
        : []),
    ],
    content: value,
    contentType: contentFormat,
    editorProps: {
      attributes: {
        'data-placeholder': placeholder ?? '',
      },
    },
    onUpdate({ editor: currentEditor }) {
      const output = contentFormat === 'markdown' ? currentEditor.getMarkdown() : currentEditor.getHTML();
      lastEmittedValue.current = output;
      onChange(output);
    },
  }, []);

  useEffect(() => {
    if (!editor) return;
    if (value !== lastEmittedValue.current) {
      lastEmittedValue.current = value;
      // setContent uses flushSync internally; defer outside React's commit phase
      // to avoid "flushSync called from inside a lifecycle method".
      queueMicrotask(() => {
        if (!editor.isDestroyed) editor.commands.setContent(value, { contentType: contentFormat, emitUpdate: false });
      });
    }
  }, [editor, value, contentFormat]);

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
          <Menu withArrow>
            <Menu.Target>
              <Tooltip label={m.heading} withArrow>
                <ActionIcon variant="subtle">
                  <IconHeading size={16} />
                </ActionIcon>
              </Tooltip>
            </Menu.Target>
            <Menu.Dropdown>
              <Menu.Item
                leftSection={<IconH1 size={16} />}
                onClick={() => editor.chain().focus().toggleHeading({ level: 1 }).run()}
              >
                {m.heading1}
              </Menu.Item>
              <Menu.Item
                leftSection={<IconH2 size={16} />}
                onClick={() => editor.chain().focus().toggleHeading({ level: 2 }).run()}
              >
                {m.heading2}
              </Menu.Item>
              <Menu.Item
                leftSection={<IconH3 size={16} />}
                onClick={() => editor.chain().focus().toggleHeading({ level: 3 }).run()}
              >
                {m.heading3}
              </Menu.Item>
              <Menu.Item
                leftSection={<IconH4 size={16} />}
                onClick={() => editor.chain().focus().toggleHeading({ level: 4 }).run()}
              >
                {m.heading4}
              </Menu.Item>
              <Menu.Item onClick={() => editor.chain().focus().setParagraph().run()}>
                {m.paragraph}
              </Menu.Item>
            </Menu.Dropdown>
          </Menu>

          <Divider orientation="vertical" />

          <Tooltip label={m.bold} withArrow>
            <ActionIcon variant="subtle" onClick={() => editor.chain().focus().toggleBold().run()}>
              <IconBold size={16} />
            </ActionIcon>
          </Tooltip>
          <Tooltip label={m.italic} withArrow>
            <ActionIcon variant="subtle" onClick={() => editor.chain().focus().toggleItalic().run()}>
              <IconItalic size={16} />
            </ActionIcon>
          </Tooltip>
          <Tooltip label={m.link} withArrow>
            <ActionIcon
              variant="subtle"
              onClick={() => promptForUrl(m.insertLinkTitle, (url) => editor.chain().focus().setLink({ href: url }).run())}
            >
              <IconLink size={16} />
            </ActionIcon>
          </Tooltip>
          <Tooltip label={m.underline} withArrow>
            <ActionIcon variant="subtle" onClick={() => editor.chain().focus().toggleUnderline().run()}>
              <IconUnderline size={16} />
            </ActionIcon>
          </Tooltip>
          <Tooltip label={m.subscript} withArrow>
            <ActionIcon variant="subtle" onClick={() => editor.chain().focus().toggleSubscript().run()}>
              <IconSubscript size={16} />
            </ActionIcon>
          </Tooltip>
          <Tooltip label={m.superscript} withArrow>
            <ActionIcon variant="subtle" onClick={() => editor.chain().focus().toggleSuperscript().run()}>
              <IconSuperscript size={16} />
            </ActionIcon>
          </Tooltip>
          <Tooltip label={m.highlight} withArrow>
            <ActionIcon variant="subtle" onClick={() => editor.chain().focus().toggleHighlight().run()}>
              <IconHighlight size={16} />
            </ActionIcon>
          </Tooltip>

          <Divider orientation="vertical" />

          <Tooltip label={m.alignLeft} withArrow>
            <ActionIcon variant="subtle" onClick={() => editor.chain().focus().setTextAlign('left').run()}>
              <IconAlignLeft size={16} />
            </ActionIcon>
          </Tooltip>
          <Tooltip label={m.alignCenter} withArrow>
            <ActionIcon variant="subtle" onClick={() => editor.chain().focus().setTextAlign('center').run()}>
              <IconAlignCenter size={16} />
            </ActionIcon>
          </Tooltip>
          <Tooltip label={m.alignRight} withArrow>
            <ActionIcon variant="subtle" onClick={() => editor.chain().focus().setTextAlign('right').run()}>
              <IconAlignRight size={16} />
            </ActionIcon>
          </Tooltip>
          <Tooltip label={m.justify} withArrow>
            <ActionIcon variant="subtle" onClick={() => editor.chain().focus().setTextAlign('justify').run()}>
              <IconAlignJustified size={16} />
            </ActionIcon>
          </Tooltip>

          <Divider orientation="vertical" />

          <Tooltip label={m.bulletList} withArrow>
            <ActionIcon variant="subtle" onClick={() => editor.chain().focus().toggleBulletList().run()}>
              <IconList size={16} />
            </ActionIcon>
          </Tooltip>
          <Tooltip label={m.orderedList} withArrow>
            <ActionIcon variant="subtle" onClick={() => editor.chain().focus().toggleOrderedList().run()}>
              <IconListNumbers size={16} />
            </ActionIcon>
          </Tooltip>
          <Tooltip label={m.taskList} withArrow>
            <ActionIcon variant="subtle" onClick={() => editor.chain().focus().toggleTaskList().run()}>
              <IconListCheck size={16} />
            </ActionIcon>
          </Tooltip>
          <Tooltip label={m.insertTable} withArrow>
            <ActionIcon
              variant="subtle"
              onClick={() => editor.chain().focus().insertTable({ rows: 3, cols: 3, withHeaderRow: true }).run()}
            >
              <IconTable size={16} />
            </ActionIcon>
          </Tooltip>
          <Tooltip label={m.insertMath} withArrow>
            <ActionIcon
              variant="subtle"
              onClick={() => promptForText(
                m.insertLatexTitle,
                m.latexPlaceholder,
                (latex) => editor.chain().focus().insertInlineMath({ latex }).run(),
              )}
            >
              <IconMathFunction size={16} />
            </ActionIcon>
          </Tooltip>

          <Divider orientation="vertical" />

          <Tooltip label={m.insertVideo} withArrow>
            <ActionIcon
              variant="subtle"
              onClick={() => promptForUrl(m.insertVideoTitle, (url) => editor.chain().focus().setYoutubeVideo({ src: url }).run())}
            >
              <IconMovie size={16} />
            </ActionIcon>
          </Tooltip>
          <Tooltip label={m.insertAudio} withArrow>
            <ActionIcon
              variant="subtle"
              onClick={() => promptForUrl(m.insertAudioTitle, (url) => editor.chain().focus().setAudio({ src: url }).run())}
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
                    <Tooltip label={m.uploadImage} withArrow>
                      <ActionIcon variant="subtle" {...props}>
                        <IconPhoto size={16} />
                      </ActionIcon>
                    </Tooltip>
                  )}
                </FileButton>
              )}
              <FileButton onChange={handleFileAttachment}>
                {(props) => (
                  <Tooltip label={m.uploadFile} withArrow>
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
        .tiptap.ProseMirror h1,
        .tiptap.ProseMirror h2,
        .tiptap.ProseMirror h3,
        .tiptap.ProseMirror h4 {
          margin: 0.75em 0 0.5em 0;
          font-weight: 600;
          line-height: 1.25;
        }
        .tiptap.ProseMirror h1 {
          font-size: 1.6em;
        }
        .tiptap.ProseMirror h2 {
          font-size: 1.35em;
        }
        .tiptap.ProseMirror h3 {
          font-size: 1.15em;
        }
        .tiptap.ProseMirror h4 {
          font-size: 1em;
        }
        .tiptap.ProseMirror ul,
        .tiptap.ProseMirror ol {
          padding-left: 1.5em;
          margin: 0 0 0.75em 0;
        }
        .tiptap.ProseMirror li p {
          margin: 0;
        }
        .tiptap.ProseMirror table {
          border-collapse: collapse;
          width: 100%;
          margin: 0.75em 0;
        }
        .tiptap.ProseMirror td,
        .tiptap.ProseMirror th {
          border: 1px solid var(--mantine-color-default-border);
          padding: 6px 8px;
          vertical-align: top;
        }
        .tiptap.ProseMirror th {
          font-weight: 600;
          background-color: var(--mantine-color-default-hover);
        }
        .tiptap.ProseMirror ul[data-type="taskList"] {
          list-style: none;
          padding-left: 0;
        }
        .tiptap.ProseMirror ul[data-type="taskList"] li {
          display: flex;
          align-items: flex-start;
          gap: 6px;
        }
        .tiptap.ProseMirror ul[data-type="taskList"] li > label {
          margin-top: 3px;
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
