import { Fragment } from 'react';
import { CardDataGrid } from './CardDataGrid';
import { DataGrid } from './DataGrid';
import { DataGridSwitch } from './DataGridSwitch';
import type {
  CardDataGridProps,
  DataGridProps,
  DataGridViewSwitcherProps,
} from './DataGrid.types';

function toCardGridProps<T extends Record<string, unknown>>(
  tableProps: DataGridProps<T>,
  cardOverrides?: Partial<CardDataGridProps<T>>
): CardDataGridProps<T> {
  return {
    data: tableProps.data,
    loading: tableProps.loading,
    columns: tableProps.columns,
    pagination: tableProps.pagination,
    onPageChange: tableProps.onPageChange,
    searchable: tableProps.searchable,
    searchPlaceholder: tableProps.searchPlaceholder,
    onSearch: tableProps.onSearch,
    toolbarRightSection: tableProps.toolbarRightSection,
    refreshable: tableProps.refreshable,
    onRefresh: tableProps.onRefresh,
    sortable: tableProps.sortable,
    onSortChange: tableProps.onSortChange,
    onCreate: tableProps.onCreate,
    createButtonText: tableProps.createButtonText,
    createButtonIcon: tableProps.createButtonIcon,
    selectable: tableProps.selectable,
    selectedRows: tableProps.selectedRows,
    onSelectionChange: tableProps.onSelectionChange,
    rowKey: tableProps.rowKey,
    onFilterChange: tableProps.onFilterChange,
    bulkActions: tableProps.bulkActions,
    ...cardOverrides,
  };
}

export function DataGridViewSwitcher<T extends Record<string, unknown>>({
  viewMode,
  onViewModeChange,
  showSwitch = true,
  switchTableLabel,
  switchCardLabel,
  tableProps,
  cardProps,
}: DataGridViewSwitcherProps<T>) {
  const switchControl = showSwitch ? (
    <DataGridSwitch
      value={viewMode}
      onChange={onViewModeChange}
      tableLabel={switchTableLabel}
      cardLabel={switchCardLabel}
    />
  ) : null;

  const resolvedTableProps: DataGridProps<T> = {
    ...tableProps,
    toolbarRightSection: switchControl ? (
      <Fragment>
        {tableProps.toolbarRightSection}
        {switchControl}
      </Fragment>
    ) : tableProps.toolbarRightSection,
  };

  const resolvedCardProps = toCardGridProps(tableProps, cardProps);
  const resolvedCardPropsWithToolbar: CardDataGridProps<T> = {
    ...resolvedCardProps,
    toolbarRightSection: switchControl ? (
      <Fragment>
        {resolvedCardProps.toolbarRightSection}
        {switchControl}
      </Fragment>
    ) : resolvedCardProps.toolbarRightSection,
  };

  return (
    viewMode === 'table' ? (
      <DataGrid {...resolvedTableProps} />
    ) : (
      <CardDataGrid {...resolvedCardPropsWithToolbar} />
    )
  );
}
