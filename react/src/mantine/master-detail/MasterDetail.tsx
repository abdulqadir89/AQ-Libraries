'use client';

import { useEffect, useRef, useState, type ChangeEvent } from 'react';
import {
  ActionIcon, Button, Grid, Group, Paper, Select, Stack, Text, TextInput,
} from '@mantine/core';
import { useDebouncedValue } from '@mantine/hooks';
import { IconRefresh, IconSearch, IconX } from '@tabler/icons-react';
import type { MasterDetailProps } from './MasterDetail.types';
import { useAQLocale } from '../locale';

/**
 * Master-detail split view: a scrollable, flexbox-rendered list panel on the left
 * (row content fully delegated to `renderRow` — no DataGrid-style fixed columns)
 * paired with a persistent detail panel on the right, driven by a single selected key.
 *
 * Search/sort/filter follow the same uncontrolled + callback conventions as DataGrid/
 * CardDataGrid: search is internally debounced and reported via `onSearch`, sort is a
 * flat dropdown (no column headers to click) reporting a ready-to-send expression via
 * `onSortChange`, and filter is a caller-supplied component whose results are reported
 * via `onFilterChange` using DataGrid's `FilterCondition`/`LogicalOperator` shape.
 *
 * Two usage modes:
 *  - Self-contained: pass `searchable`/`sortOptions`/`filterConfig` and let MasterDetail
 *    render its own toolbar.
 *  - Externally driven: pass `searchable={false}` and drive `data` from your own search/
 *    filter UI above the component (e.g. reusing an existing search bar component).
 */
export function MasterDetail<T = Record<string, unknown>>({
  data,
  loading,
  rowKey,
  selectedKey,
  onSelectionChange,
  renderRow,
  renderDetail,
  emptyListText,
  searchable,
  searchPlaceholder,
  onSearch,
  sortOptions,
  sortValue,
  onSortChange,
  filterConfig,
  hasMore,
  onLoadMore,
  loadingMore,
  totalCount,
  refreshable,
  onRefresh,
  toolbarRightSection,
  listSpan = 4,
  listHeight = 'calc(100vh - 260px)',
}: MasterDetailProps<T>) {
  const { messages: aqMessages } = useAQLocale();
  const m = aqMessages.masterDetail;
  const resolvedEmptyListText = emptyListText ?? m.noItemsFound;
  const resolvedSortPlaceholder = m.sortBy;
  const getKey = (record: T): string => (
    typeof rowKey === 'function' ? rowKey(record) : String(record[rowKey])
  );

  const [searchText, setSearchText] = useState('');
  const [debouncedSearchText] = useDebouncedValue(searchText, 500);

  const onSearchRef = useRef(onSearch);
  useEffect(() => { onSearchRef.current = onSearch; });

  const isFirstSearchRun = useRef(true);
  useEffect(() => {
    if (isFirstSearchRun.current) {
      isFirstSearchRun.current = false;
      return;
    }
    onSearchRef.current?.(debouncedSearchText);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [debouncedSearchText]);

  const handleClearSearch = () => setSearchText('');

  const showToolbar = searchable || refreshable || !!sortOptions?.length || !!filterConfig || !!toolbarRightSection;
  const selectedRecord = data.find((record) => getKey(record) === selectedKey);

  return (
    <Stack gap="sm">
      {showToolbar && (
        <Group justify="space-between" wrap="wrap">
          <Group>
            {searchable && (
              <TextInput
                placeholder={searchPlaceholder}
                leftSection={<IconSearch size={16} />}
                rightSection={searchText ? (
                  <ActionIcon size="sm" variant="subtle" color="gray" onClick={handleClearSearch} title="Clear search">
                    <IconX size={14} />
                  </ActionIcon>
                ) : null}
                value={searchText}
                onChange={(event: ChangeEvent<HTMLInputElement>) => setSearchText(event.currentTarget.value)}
                style={{ width: 300, maxWidth: '100%' }}
              />
            )}
            {sortOptions && sortOptions.length > 0 && (
              <Select
                placeholder={resolvedSortPlaceholder}
                data={sortOptions}
                value={sortValue ?? null}
                onChange={(value) => onSortChange?.(value ?? '')}
                clearable
                style={{ width: 200 }}
              />
            )}
            {filterConfig?.component}
            {refreshable && (
              <ActionIcon variant="light" onClick={onRefresh} title="Refresh">
                <IconRefresh size={16} />
              </ActionIcon>
            )}
          </Group>
          {toolbarRightSection}
        </Group>
      )}

      <Grid gap="md">
        <Grid.Col span={{ base: 12, md: listSpan }}>
          <Paper withBorder radius="md" style={{ height: listHeight, overflowY: 'auto' }}>
            <Stack gap={0}>
              {data.map((record, index) => {
                const key = getKey(record);
                return (
                  <div key={key} onClick={() => onSelectionChange?.(key, record)} style={{ cursor: 'pointer' }}>
                    {renderRow(record, key === selectedKey, index)}
                  </div>
                );
              })}
              {!loading && data.length === 0 && (
                <Text c="dimmed" ta="center" p="lg">{resolvedEmptyListText}</Text>
              )}
              {hasMore && (
                <Button variant="subtle" loading={loadingMore} onClick={onLoadMore} m="sm">
                  {m.loadMore(data.length, totalCount)}
                </Button>
              )}
            </Stack>
          </Paper>
        </Grid.Col>
        <Grid.Col span={{ base: 12, md: 12 - listSpan }}>
          {renderDetail(selectedRecord, selectedKey ?? null)}
        </Grid.Col>
      </Grid>
    </Stack>
  );
}
