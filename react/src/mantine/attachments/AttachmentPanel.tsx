import { useEffect, useState } from 'react';
import { Stack, Tabs } from '@mantine/core';
import { AttachmentList } from './AttachmentList';
import { AttachmentUpload } from './AttachmentUpload';
import type { AttachmentListProps } from './AttachmentList';
import type { AttachmentUploadProps } from './AttachmentUpload';
import type { AttachmentLimits } from './types';

export interface AttachmentPanelProps {
  entityType: string;
  entityId: string;
  categories: string[];
  canUpload?: boolean;
  canDelete?: boolean;
  uploadVariant?: AttachmentUploadProps['variant'];
  uploadButtonPosition?: AttachmentUploadProps['buttonPosition'];
  /** Bump this to force the attachment list to refetch, e.g. after an upload from outside this panel. */
  externalRefreshKey?: number;
  onFetchList: AttachmentListProps['onFetchList'];
  onFetchLimits: (entityType: string) => Promise<AttachmentLimits>;
  onUpload: AttachmentUploadProps['onUpload'];
  onDelete: AttachmentListProps['onDelete'];
  fetchAuthenticated: AttachmentListProps['fetchAuthenticated'];
  onError: (err: unknown) => void;
}

export function AttachmentPanel({
  entityType, entityId, categories, canUpload, canDelete, uploadVariant, uploadButtonPosition,
  externalRefreshKey,
  onFetchList, onFetchLimits, onUpload, onDelete, fetchAuthenticated, onError,
}: AttachmentPanelProps) {
  const [refreshKey, setRefreshKey] = useState(0);
  const [limits, setLimits] = useState<AttachmentLimits | undefined>(undefined);
  const [existingCounts, setExistingCounts] = useState<Record<string, number>>({});

  useEffect(() => {
    onFetchLimits(entityType).then(setLimits).catch(onError);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [entityType]);

  const combinedRefreshKey = refreshKey + (externalRefreshKey ?? 0);
  const handleUploaded = () => setRefreshKey((k) => k + 1);
  const handleCountChange = (cat: string) => (count: number) => {
    setExistingCounts((prev) => (prev[cat] === count ? prev : { ...prev, [cat]: count }));
  };

  if (categories.length === 1) {
    const category = categories[0];
    return (
      <Stack gap="md">
        {canUpload && (
          <AttachmentUpload
            entityType={entityType}
            entityId={entityId}
            category={category}
            onUploaded={handleUploaded}
            limits={limits}
            existingCount={existingCounts[category] ?? 0}
            variant={uploadVariant}
            buttonPosition={uploadButtonPosition}
            onUpload={onUpload}
            onError={onError}
          />
        )}
        <AttachmentList
          entityType={entityType}
          entityId={entityId}
          category={category}
          canDelete={canDelete}
          refreshKey={combinedRefreshKey}
          onFetchList={onFetchList}
          onDelete={onDelete}
          fetchAuthenticated={fetchAuthenticated}
          onError={onError}
          onCountChange={handleCountChange(category)}
        />
      </Stack>
    );
  }

  return (
    <Tabs defaultValue={categories[0]}>
      <Tabs.List mb="md">
        {categories.map((cat) => (
          <Tabs.Tab key={cat} value={cat} style={{ textTransform: 'capitalize' }}>
            {cat}
          </Tabs.Tab>
        ))}
      </Tabs.List>

      {categories.map((cat) => (
        <Tabs.Panel key={cat} value={cat}>
          <Stack gap="md">
            {canUpload && (
              <AttachmentUpload
                entityType={entityType}
                entityId={entityId}
                category={cat}
                onUploaded={handleUploaded}
                limits={limits}
                existingCount={existingCounts[cat] ?? 0}
                variant={uploadVariant}
                buttonPosition={uploadButtonPosition}
                onUpload={onUpload}
                onError={onError}
              />
            )}
            <AttachmentList
              entityType={entityType}
              entityId={entityId}
              category={cat}
              canDelete={canDelete}
              refreshKey={combinedRefreshKey}
              onFetchList={onFetchList}
              onDelete={onDelete}
              fetchAuthenticated={fetchAuthenticated}
              onError={onError}
              onCountChange={handleCountChange(cat)}
            />
          </Stack>
        </Tabs.Panel>
      ))}
    </Tabs>
  );
}
