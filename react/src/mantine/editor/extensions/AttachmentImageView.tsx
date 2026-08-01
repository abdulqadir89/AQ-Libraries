import { useEffect, useRef, useState } from 'react';
import { NodeViewWrapper } from '@tiptap/react';
import type { NodeViewProps } from '@tiptap/react';
import { Image, Loader, Stack, Text } from '@mantine/core';
import type { AttachmentImageOptions } from './attachmentImage';

export function AttachmentImageView({ node, updateAttributes, extension }: NodeViewProps) {
  const options = extension.options as AttachmentImageOptions;
  const { src, alt, uploadId } = node.attrs as { src: string | null; alt: string; uploadId: string | null };
  const [previewSrc, setPreviewSrc] = useState<string | null>(null);
  const startedUpload = useRef(false);

  useEffect(() => {
    if (!uploadId || src || startedUpload.current) return;
    const file = options.pendingFiles.get(uploadId);
    if (!file) return;
    startedUpload.current = true;
    options.pendingFiles.delete(uploadId);

    options
      .onUploadFile(file)
      .then((ref) => {
        updateAttributes({ src: ref.url, uploadId: null });
      })
      .catch((err) => {
        options.onError(err);
        updateAttributes({ src: null, uploadId: null, alt: `${alt} (upload failed)` });
      });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [uploadId, src]);

  const loadedSrc = useRef<string | null>(null);

  useEffect(() => {
    if (!src || loadedSrc.current === src) return;
    loadedSrc.current = src;
    let objectUrl: string | undefined;
    let cancelled = false;

    options
      .fetchAuthenticated(src)
      .then((res) => (res.ok ? res.blob() : Promise.reject(new Error(`Failed to load image: ${res.status}`))))
      .then((blob) => {
        if (cancelled) return;
        objectUrl = URL.createObjectURL(blob);
        setPreviewSrc(objectUrl);
      })
      .catch(() => {
        if (!cancelled) setPreviewSrc(src);
      });

    return () => {
      cancelled = true;
      if (objectUrl) URL.revokeObjectURL(objectUrl);
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [src]);

  return (
    <NodeViewWrapper as="div" style={{ display: 'inline-block', maxWidth: '100%' }}>
      {uploadId && !src ? (
        <Stack align="center" gap={4} p="md" style={{ border: '1px dashed var(--mantine-color-default-border)', borderRadius: 8 }}>
          <Loader size="sm" />
          <Text size="xs" c="dimmed">Uploading {alt}...</Text>
        </Stack>
      ) : (
        <Image src={previewSrc ?? undefined} alt={alt} radius="sm" fit="contain" mah={400} />
      )}
    </NodeViewWrapper>
  );
}
