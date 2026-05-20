import { useState } from 'react';
import { Stack, Tabs } from '@mantine/core';
import { AttachmentList } from './AttachmentList';
import { AttachmentUpload } from './AttachmentUpload';
import type { AttachmentListProps } from './AttachmentList';
import type { AttachmentUploadProps } from './AttachmentUpload';

export interface AttachmentPanelProps {
  entityType: string;
  entityId: string;
  categories: string[];
  canUpload?: boolean;
  canDelete?: boolean;
  maxFiles?: number;
  onFetchList: AttachmentListProps['onFetchList'];
  onUpload: AttachmentUploadProps['onUpload'];
  onDelete: AttachmentListProps['onDelete'];
  fetchAuthenticated: AttachmentListProps['fetchAuthenticated'];
  onError: (err: unknown) => void;
}

export function AttachmentPanel({
  entityType, entityId, categories, canUpload, canDelete, maxFiles = 10,
  onFetchList, onUpload, onDelete, fetchAuthenticated, onError,
}: AttachmentPanelProps) {
  const [refreshKey, setRefreshKey] = useState(0);

  const handleUploaded = () => setRefreshKey((k) => k + 1);

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
            maxFiles={maxFiles}
            onUpload={onUpload}
            onError={onError}
          />
        )}
        <AttachmentList
          entityType={entityType}
          entityId={entityId}
          category={category}
          canDelete={canDelete}
          refreshKey={refreshKey}
          onFetchList={onFetchList}
          onDelete={onDelete}
          fetchAuthenticated={fetchAuthenticated}
          onError={onError}
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
                maxFiles={maxFiles}
                onUpload={onUpload}
                onError={onError}
              />
            )}
            <AttachmentList
              entityType={entityType}
              entityId={entityId}
              category={cat}
              canDelete={canDelete}
              refreshKey={refreshKey}
              onFetchList={onFetchList}
              onDelete={onDelete}
              fetchAuthenticated={fetchAuthenticated}
              onError={onError}
            />
          </Stack>
        </Tabs.Panel>
      ))}
    </Tabs>
  );
}
