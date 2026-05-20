import { useCallback, useEffect, useState } from 'react';
import {
  ActionIcon, Badge, Group, Image, Skeleton, Stack, Text, Tooltip,
} from '@mantine/core';
import { modals } from '@mantine/modals';
import { IconDownload, IconFile, IconTrash } from '@tabler/icons-react';
import { DateTimeOffsetDisplay } from '../datetime';
import type { AttachmentDto } from './types';

function formatSize(bytes?: number): string {
  if (bytes == null) return '—';
  if (bytes >= 1024 * 1024) return `${(bytes / 1024 / 1024).toFixed(1)} MB`;
  if (bytes >= 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${bytes} B`;
}

async function triggerDownload(
  attachment: AttachmentDto,
  fetchAuthenticated: (url: string) => Promise<Response>,
) {
  if (!attachment.downloadUrl || !attachment.fileName) return;
  try {
    const res = await fetchAuthenticated(attachment.downloadUrl);
    if (!res.ok) throw new Error(`Download failed: ${res.status}`);
    const blob = await res.blob();
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = attachment.fileName;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
  } catch (err) {
    throw err;
  }
}

export interface AttachmentListProps {
  entityType: string;
  entityId: string;
  category?: string;
  canDelete?: boolean;
  refreshKey?: number;
  onFetchList: (entityType: string, entityId: string, category?: string) => Promise<AttachmentDto[]>;
  onDelete: (entityType: string, entityId: string, category: string, fileName: string) => Promise<void>;
  fetchAuthenticated: (url: string) => Promise<Response>;
  onError: (err: unknown) => void;
}

interface AttachmentRowProps {
  attachment: AttachmentDto;
  canDelete?: boolean;
  onDelete: () => void;
  fetchAuthenticated: (url: string) => Promise<Response>;
  onError: (err: unknown) => void;
}

function AttachmentRow({ attachment, canDelete, onDelete, fetchAuthenticated, onError }: AttachmentRowProps) {
  const isImage = attachment.contentType?.startsWith('image/');
  const [imageSrc, setImageSrc] = useState<string | null>(null);

  useEffect(() => {
    if (!isImage || !attachment.downloadUrl) return;
    let objectUrl: string;
    fetchAuthenticated(attachment.downloadUrl)
      .then((res) => (res.ok ? res.blob() : Promise.reject(res.status)))
      .then((blob) => {
        objectUrl = URL.createObjectURL(blob);
        setImageSrc(objectUrl);
      })
      .catch(() => { /* silently show file icon instead */ });
    return () => { if (objectUrl) URL.revokeObjectURL(objectUrl); };
  }, [isImage, attachment.downloadUrl, fetchAuthenticated]);

  return (
    <Group justify="space-between" wrap="nowrap" p="xs" style={{ border: '1px solid var(--mantine-color-default-border)', borderRadius: 'var(--mantine-radius-sm)' }}>
      <Group gap="sm" wrap="nowrap" style={{ minWidth: 0 }}>
        {isImage && imageSrc ? (
          <Image src={imageSrc} alt={attachment.fileName ?? ''} mah={48} maw={64} fit="contain" radius="xs" />
        ) : (
          <IconFile size={32} stroke={1.5} style={{ flexShrink: 0, color: 'var(--mantine-color-dimmed)' }} />
        )}
        <Stack gap={2} style={{ minWidth: 0 }}>
          <Text size="sm" fw={500} truncate>{attachment.fileName ?? '—'}</Text>
          <Group gap="xs">
            {attachment.contentType && (
              <Badge size="xs" variant="light" color="gray">{attachment.contentType}</Badge>
            )}
            <Text size="xs" c="dimmed">{formatSize(attachment.size)}</Text>
            {attachment.createdAt && (
              <DateTimeOffsetDisplay value={attachment.createdAt} textProps={{ size: 'xs', c: 'dimmed' }} />
            )}
          </Group>
        </Stack>
      </Group>

      <Group gap="xs" wrap="nowrap" style={{ flexShrink: 0 }}>
        {attachment.downloadUrl && (
          <Tooltip label="Download" withArrow>
            <ActionIcon variant="subtle" color="blue" onClick={() => {
              void triggerDownload(attachment, fetchAuthenticated).catch(onError);
            }}>
              <IconDownload size={16} />
            </ActionIcon>
          </Tooltip>
        )}
        {canDelete && (
          <Tooltip label="Delete" withArrow>
            <ActionIcon variant="subtle" color="red" onClick={onDelete}>
              <IconTrash size={16} />
            </ActionIcon>
          </Tooltip>
        )}
      </Group>
    </Group>
  );
}

export function AttachmentList({
  entityType, entityId, category, canDelete, refreshKey,
  onFetchList, onDelete, fetchAuthenticated, onError,
}: AttachmentListProps) {
  const [attachments, setAttachments] = useState<AttachmentDto[]>([]);
  const [loading, setLoading] = useState(true);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const data = await onFetchList(entityType, entityId, category);
      setAttachments(data);
    } catch (err) {
      onError(err);
    } finally {
      setLoading(false);
    }
  }, [entityType, entityId, category, onFetchList, onError]);

  useEffect(() => { void load(); }, [load, refreshKey]);

  const handleDelete = (attachment: AttachmentDto) => {
    modals.openConfirmModal({
      title: 'Delete Attachment',
      children: <Text size="sm">Delete &quot;{attachment.fileName}&quot;? This cannot be undone.</Text>,
      labels: { confirm: 'Delete', cancel: 'Cancel' },
      confirmProps: { color: 'red' },
      onConfirm: async () => {
        try {
          await onDelete(
            attachment.entityType!,
            attachment.entityId!,
            attachment.category!,
            attachment.fileName!,
          );
          void load();
        } catch (err) {
          onError(err);
        }
      },
    });
  };

  if (loading) {
    return (
      <Stack gap="xs">
        <Skeleton height={40} />
        <Skeleton height={40} />
      </Stack>
    );
  }

  if (attachments.length === 0) {
    return <Text size="sm" c="dimmed">No attachments.</Text>;
  }

  return (
    <Stack gap="xs">
      {attachments.map((attachment) => (
        <AttachmentRow
          key={attachment.id}
          attachment={attachment}
          canDelete={canDelete}
          fetchAuthenticated={fetchAuthenticated}
          onError={onError}
          onDelete={() => handleDelete(attachment)}
        />
      ))}
    </Stack>
  );
}
