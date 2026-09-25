import { useRef, useState } from 'react';
import {
  ActionIcon, Badge, Box, Button, Group, Stack, Text, Tooltip,
} from '@mantine/core';
import { IconTrash, IconUpload } from '@tabler/icons-react';
import type { AttachmentLimits } from './types';
import { useAQLocale } from '../locale';

export interface AttachmentUploadProps {
  entityType: string;
  entityId: string;
  category: string;
  onUploaded?: () => void;
  limits?: AttachmentLimits;
  existingCount?: number;
  variant?: 'button' | 'dropzone';
  buttonPosition?: 'left' | 'right';
  onUpload: (entityType: string, entityId: string, category: string, file: File) => Promise<unknown>;
  onError: (err: unknown) => void;
}

interface PendingFile {
  id: string;
  file: File;
}

function formatSize(bytes: number): string {
  return bytes >= 1024 * 1024
    ? `${(bytes / 1024 / 1024).toFixed(1)} MB`
    : `${(bytes / 1024).toFixed(1)} KB`;
}

function formatContentTypes(types: string[]): string {
  const extensions = types.map((t) => t.split('/')[1]?.replace('vnd.openxmlformats-officedocument.wordprocessingml.document', 'docx').toUpperCase());
  return [...new Set(extensions)].join(', ');
}

export function AttachmentUpload({
  entityType, entityId, category, onUploaded, limits, existingCount = 0, variant = 'button', buttonPosition = 'right', onUpload, onError,
}: AttachmentUploadProps) {
  const { messages: aqMessages } = useAQLocale();
  const m = aqMessages.attachments;
  const maxFiles = limits?.maxFiles ?? 10;
  const accept = limits?.allowedContentTypes.join(',');
  const [pending, setPending] = useState<PendingFile[]>([]);
  const [uploading, setUploading] = useState(false);
  const [dragging, setDragging] = useState(false);
  const inputRef = useRef<HTMLInputElement>(null);

  const addFiles = (files: FileList | File[]) => {
    const arr = Array.from(files);
    setPending((prev) => {
      const remaining = maxFiles - existingCount - prev.length;
      if (remaining <= 0) return prev;
      const toAdd = arr.slice(0, remaining).map((f) => ({ id: crypto.randomUUID(), file: f }));
      const oversized = toAdd.filter((f) => limits && f.file.size > limits.maxFileSizeBytes);
      if (oversized.length > 0) {
        onError(new Error(m.fileTooLarge(formatSize(limits!.maxFileSizeBytes), oversized.map((f) => f.file.name).join(', '))));
      }
      const accepted = toAdd.filter((f) => !limits || f.file.size <= limits.maxFileSizeBytes);
      return [...prev, ...accepted];
    });
  };

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files) addFiles(e.target.files);
    e.target.value = '';
  };

  const handleDragOver = (e: React.DragEvent) => { e.preventDefault(); setDragging(true); };
  const handleDragLeave = () => setDragging(false);
  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault();
    setDragging(false);
    if (e.dataTransfer.files) addFiles(e.dataTransfer.files);
  };

  const removeFile = (id: string) => setPending((prev) => prev.filter((f) => f.id !== id));

  const handleUpload = async () => {
    if (pending.length === 0) return;
    setUploading(true);
    const results = await Promise.allSettled(
      pending.map(({ file }) => onUpload(entityType, entityId, category, file)),
    );
    const errors = pending
      .filter((_, i) => results[i].status === 'rejected')
      .map(({ file }) => file.name);
    setPending([]);
    setUploading(false);
    if (errors.length > 0) {
      onError(new Error(m.failedToUpload(errors.join(', '))));
    }
    onUploaded?.();
  };

  const atLimit = existingCount + pending.length >= maxFiles;
  const helperText = (
    <>
      {existingCount + pending.length}/{maxFiles} files
      {limits && ` · ${formatContentTypes(limits.allowedContentTypes)} · up to ${formatSize(limits.maxFileSizeBytes)} each`}
    </>
  );
  const fileInput = (
    <input
      ref={inputRef}
      type="file"
      multiple={maxFiles > 1}
      accept={accept}
      onChange={handleInputChange}
      style={{ display: 'none' }}
    />
  );

  return (
    <Stack gap="xs">
      {variant === 'dropzone' ? (
        <Box
          onClick={() => !atLimit && inputRef.current?.click()}
          onDragOver={handleDragOver}
          onDragLeave={handleDragLeave}
          onDrop={handleDrop}
          style={{
            border: `2px dashed var(--mantine-color-${dragging ? 'blue-5' : 'default-border'})`,
            borderRadius: 'var(--mantine-radius-sm)',
            padding: '20px',
            textAlign: 'center',
            cursor: atLimit ? 'not-allowed' : 'pointer',
            background: dragging ? 'var(--mantine-color-blue-light)' : undefined,
            transition: 'border-color 120ms, background 120ms',
          }}
        >
          <IconUpload size={24} stroke={1.5} style={{ color: 'var(--mantine-color-dimmed)', marginBottom: 6 }} />
          <Text size="sm" c="dimmed">
            {atLimit
              ? m.maxFilesReached(maxFiles)
              : m.dragDropFilesHere}
          </Text>
          <Text size="xs" c="dimmed">{helperText}</Text>
          {fileInput}
        </Box>
      ) : (
        <Group gap="sm" wrap="nowrap" align="center" justify={buttonPosition === 'right' ? 'space-between' : 'flex-start'}>
          {buttonPosition === 'left' && (
            <Button
              size="xs"
              leftSection={<IconUpload size={14} />}
              disabled={atLimit}
              onClick={() => inputRef.current?.click()}
            >
              {m.addFiles}
            </Button>
          )}
          <Text size="xs" c="dimmed">
            {atLimit ? m.maxFilesReached(maxFiles) : helperText}
          </Text>
          {buttonPosition === 'right' && (
            <Button
              size="xs"
              leftSection={<IconUpload size={14} />}
              disabled={atLimit}
              onClick={() => inputRef.current?.click()}
            >
              {m.addFiles}
            </Button>
          )}
          {fileInput}
        </Group>
      )}

      {pending.length > 0 && (
        <Stack gap="xs">
          {pending.map(({ id, file }) => (
            <Group key={id} justify="space-between" wrap="nowrap" p="xs" style={{ border: '1px solid var(--mantine-color-default-border)', borderRadius: 'var(--mantine-radius-sm)' }}>
              <Group gap="sm" wrap="nowrap" style={{ minWidth: 0 }}>
                <Text size="sm" truncate style={{ flex: 1 }}>{file.name}</Text>
                <Badge size="xs" variant="light" color="gray">
                  {file.size >= 1024 * 1024
                    ? `${(file.size / 1024 / 1024).toFixed(1)} MB`
                    : `${(file.size / 1024).toFixed(1)} KB`}
                </Badge>
              </Group>
              <Tooltip label={m.remove} withArrow>
                <ActionIcon variant="subtle" color="red" onClick={() => removeFile(id)} disabled={uploading}>
                  <IconTrash size={14} />
                </ActionIcon>
              </Tooltip>
            </Group>
          ))}

          <Group justify="flex-end">
            <Button
              size="xs"
              variant="subtle"
              color="gray"
              onClick={() => setPending([])}
              disabled={uploading}
            >
              {m.clearAll}
            </Button>
            <Button
              size="xs"
              leftSection={<IconUpload size={14} />}
              loading={uploading}
              onClick={() => void handleUpload()}
            >
              {m.uploadFiles(pending.length)}
            </Button>
          </Group>
        </Stack>
      )}
    </Stack>
  );
}
