import { useRef, useState } from 'react';
import {
  ActionIcon, Badge, Box, Button, Group, Stack, Text, Tooltip,
} from '@mantine/core';
import { IconTrash, IconUpload } from '@tabler/icons-react';
import type { AttachmentLimits } from './types';

export interface AttachmentUploadProps {
  entityType: string;
  entityId: string;
  category: string;
  onUploaded?: () => void;
  limits?: AttachmentLimits;
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
  entityType, entityId, category, onUploaded, limits, onUpload, onError,
}: AttachmentUploadProps) {
  const maxFiles = limits?.maxFiles ?? 10;
  const accept = limits?.allowedContentTypes.join(',');
  const [pending, setPending] = useState<PendingFile[]>([]);
  const [uploading, setUploading] = useState(false);
  const [dragging, setDragging] = useState(false);
  const inputRef = useRef<HTMLInputElement>(null);

  const addFiles = (files: FileList | File[]) => {
    const arr = Array.from(files);
    setPending((prev) => {
      const remaining = maxFiles - prev.length;
      if (remaining <= 0) return prev;
      const toAdd = arr.slice(0, remaining).map((f) => ({ id: crypto.randomUUID(), file: f }));
      const oversized = toAdd.filter((f) => limits && f.file.size > limits.maxFileSizeBytes);
      if (oversized.length > 0) {
        onError(new Error(`File too large (max ${formatSize(limits!.maxFileSizeBytes)}): ${oversized.map((f) => f.file.name).join(', ')}`));
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
    const errors: string[] = [];
    for (const { file } of pending) {
      try {
        await onUpload(entityType, entityId, category, file);
      } catch {
        errors.push(file.name);
      }
    }
    setPending([]);
    setUploading(false);
    if (errors.length > 0) {
      onError(new Error(`Failed to upload: ${errors.join(', ')}`));
    }
    onUploaded?.();
  };

  const atLimit = pending.length >= maxFiles;

  return (
    <Stack gap="xs">
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
            ? `Maximum ${maxFiles} file${maxFiles === 1 ? '' : 's'} reached`
            : 'Drag & drop files here or click to browse'}
        </Text>
        <Text size="xs" c="dimmed">
          {pending.length}/{maxFiles} files
          {limits && ` · ${formatContentTypes(limits.allowedContentTypes)} · up to ${formatSize(limits.maxFileSizeBytes)} each`}
        </Text>
        <input
          ref={inputRef}
          type="file"
          multiple={maxFiles > 1}
          accept={accept}
          onChange={handleInputChange}
          style={{ display: 'none' }}
        />
      </Box>

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
              <Tooltip label="Remove" withArrow>
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
              Clear all
            </Button>
            <Button
              size="xs"
              leftSection={<IconUpload size={14} />}
              loading={uploading}
              onClick={() => void handleUpload()}
            >
              Upload {pending.length} file{pending.length === 1 ? '' : 's'}
            </Button>
          </Group>
        </Stack>
      )}
    </Stack>
  );
}
