import { forwardRef, useImperativeHandle, useRef, useState } from 'react';
import {
  ActionIcon, Badge, Box, Group, Select, Stack, Text, Title, Tooltip,
} from '@mantine/core';
import { IconTrash, IconUpload } from '@tabler/icons-react';

interface StagedFile {
  id: string;
  file: File;
  category: string;
}

export interface StagedAttachmentPickerHandle {
  uploadAll: (entityType: string, entityId: string) => Promise<void>;
  hasStagedFiles: boolean;
}

export interface StagedAttachmentPickerProps {
  categories: string[];
  defaultCategory?: string;
  maxFiles?: number;
  onUpload: (entityType: string, entityId: string, category: string, file: File) => Promise<void>;
  onError: (err: unknown) => void;
}

export const StagedAttachmentPicker = forwardRef<StagedAttachmentPickerHandle, StagedAttachmentPickerProps>(
  function StagedAttachmentPicker({ categories, defaultCategory, maxFiles = 10, onUpload, onError }, ref) {
    const [stagedFiles, setStagedFiles] = useState<StagedFile[]>([]);
    const [pendingCategory, setPendingCategory] = useState<string>(defaultCategory ?? categories[0] ?? 'general');
    const [dragging, setDragging] = useState(false);
    const inputRef = useRef<HTMLInputElement>(null);

    const categoryOptions = categories.map((c) => ({ value: c, label: c.charAt(0).toUpperCase() + c.slice(1) }));

    const addFiles = (files: FileList | File[]) => {
      const arr = Array.from(files);
      setStagedFiles((prev) => {
        const remaining = maxFiles - prev.length;
        if (remaining <= 0) return prev;
        const toAdd = arr.slice(0, remaining).map((f) => ({
          id: crypto.randomUUID(),
          file: f,
          category: pendingCategory,
        }));
        return [...prev, ...toAdd];
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

    const handleRemove = (id: string) => setStagedFiles((prev) => prev.filter((f) => f.id !== id));

    const atLimit = stagedFiles.length >= maxFiles;

    useImperativeHandle(ref, () => ({
      get hasStagedFiles() {
        return stagedFiles.length > 0;
      },
      async uploadAll(entityType: string, entityId: string) {
        const errors: string[] = [];
        for (const staged of stagedFiles) {
          try {
            await onUpload(entityType, entityId, staged.category, staged.file);
          } catch {
            errors.push(staged.file.name);
          }
        }
        setStagedFiles([]);
        if (errors.length > 0) {
          onError(new Error(`Failed to upload: ${errors.join(', ')}`));
        }
      },
    }));

    return (
      <Stack gap="sm">
        <Title order={6} c="dimmed">Attachments</Title>

        {categories.length > 1 && (
          <Select
            label="Category"
            data={categoryOptions}
            value={pendingCategory}
            onChange={(v: string | null) => v && setPendingCategory(v)}
            style={{ width: 200 }}
          />
        )}

        <Box
          onClick={() => !atLimit && inputRef.current?.click()}
          onDragOver={handleDragOver}
          onDragLeave={handleDragLeave}
          onDrop={handleDrop}
          style={{
            border: `2px dashed var(--mantine-color-${dragging ? 'blue-5' : 'default-border'})`,
            borderRadius: 'var(--mantine-radius-sm)',
            padding: '16px',
            textAlign: 'center',
            cursor: atLimit ? 'not-allowed' : 'pointer',
            background: dragging ? 'var(--mantine-color-blue-light)' : undefined,
            transition: 'border-color 120ms, background 120ms',
          }}
        >
          <IconUpload size={20} stroke={1.5} style={{ color: 'var(--mantine-color-dimmed)', marginBottom: 4 }} />
          <Text size="sm" c="dimmed">
            {atLimit
              ? `Maximum ${maxFiles} file${maxFiles === 1 ? '' : 's'} reached`
              : 'Drag & drop or click to browse'}
          </Text>
          {!atLimit && maxFiles > 1 && (
            <Text size="xs" c="dimmed">{maxFiles - stagedFiles.length} remaining</Text>
          )}
          <input
            ref={inputRef}
            type="file"
            multiple={maxFiles > 1}
            onChange={handleInputChange}
            style={{ display: 'none' }}
          />
        </Box>

        {stagedFiles.length > 0 && (
          <Stack gap="xs">
            {stagedFiles.map((staged) => (
              <Group key={staged.id} justify="space-between" wrap="nowrap" p="xs" style={{ border: '1px solid var(--mantine-color-default-border)', borderRadius: 'var(--mantine-radius-sm)' }}>
                <Group gap="sm" wrap="nowrap" style={{ minWidth: 0 }}>
                  <Text size="sm" truncate style={{ flex: 1 }}>{staged.file.name}</Text>
                  {categories.length > 1 && (
                    <Badge size="xs" variant="light" color="blue" style={{ textTransform: 'capitalize' }}>
                      {staged.category}
                    </Badge>
                  )}
                </Group>
                <Tooltip label="Remove" withArrow>
                  <ActionIcon variant="subtle" color="red" onClick={() => handleRemove(staged.id)}>
                    <IconTrash size={14} />
                  </ActionIcon>
                </Tooltip>
              </Group>
            ))}
          </Stack>
        )}
      </Stack>
    );
  },
);
