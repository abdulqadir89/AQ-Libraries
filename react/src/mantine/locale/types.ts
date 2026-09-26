/**
 * Typed message shape for every localizable string in @AQ/react-components.
 *
 * Grouped by component. Strings with counts or interpolated values are plain
 * functions (not ICU) — the host app (ELS web) supplies them, typically backed
 * by next-intl's `t()`.
 */

export interface DataGridMessages {
  searchPlaceholder: string;
  refresh: string;
  resetFilters: string;
  options: string;
  create: string;
  bulkActions: (count: number) => string;
  clearSearch: string;
  noData: string;
  actions: string;
  showHideColumns: string;
  primaryColumnLocked: string;
  confirm: string;
  cancel: string;
  delete: string;
  deleteConfirmTitle: string;
  deleteConfirmContent: string;
  deleteErrorTitle: string;
  deleteErrorMessage: string;
  overview: string;
  details: string;
  edit: string;
  noRecords: string;
  showingEntries: (from: number, to: number, total: number) => string;
  rowsPerPage: string;
  removeFilter: string;
  removeSort: string;
}

export interface ColumnFilterMessages {
  operators: {
    contains: string;
    eq: string;
    ne: string;
    startswith: string;
    endswith: string;
    gt: string;
    gte: string;
    lt: string;
    lte: string;
    between: string;
    isnull: string;
    isnotnull: string;
    in: string;
    notin: string;
    // date operator aliases
    on: string;
    noton: string;
    after: string;
    onOrAfter: string;
    before: string;
    onOrBefore: string;
  };
  filterByLabel: (columnTitle: string) => string;
  operator: string;
  value: string;
  from: string;
  to: string;
  field: string;
  clear: string;
  apply: string;
  true: string;
  false: string;
  selectValue: string;
  selectValues: string;
  search: string;
  noEnumOptions: string;
  noMatches: string;
  noLookupConfig: string;
  limitReached: (max: number) => string;
  enterNumber: string;
  selectDate: string;
  selectStartDate: string;
  selectEndDate: string;
  enterText: string;
  fromDateAfterToDate: string;
  fromValueGreaterThanToValue: string;
}

export interface CardDataGridMessages {
  searchPlaceholder: string;
  refresh: string;
  resetFilters: string;
  options: string;
  create: string;
  bulkActions: (count: number) => string;
  clearSearch: string;
  noData: string;
  confirm: string;
  cancel: string;
  item: string;
  cardImageAlt: string;
  selectAll: string;
  noRecords: string;
  showingEntries: (from: number, to: number, total: number) => string;
  rowsPerPage: string;
  filterAndSort: string;
  filterConditions: string;
  matchAll: string;
  matchAny: string;
  addFilter: string;
  noFiltersConfigured: string;
  sort: string;
  addSort: string;
  noSortConfigured: string;
  resetOptions: string;
  apply: string;
  operator: string;
  selectValue: string;
  selectValues: string;
  pickDate: string;
  enumValue: string;
  operatorEquals: string;
  operatorNotEquals: string;
  operatorGreaterThan: string;
  operatorGreaterThanOrEqual: string;
  operatorLessThan: string;
  operatorLessThanOrEqual: string;
  operatorBetween: string;
  operatorIsNull: string;
  operatorIsNotNull: string;
  operatorOn: string;
  operatorNotOn: string;
  operatorAfter: string;
  operatorOnOrAfter: string;
  operatorBefore: string;
  operatorOnOrBefore: string;
  operatorIsAnyOf: string;
  operatorIsNotAnyOf: string;
  operatorContains: string;
  operatorStartsWith: string;
  operatorEndsWith: string;
  ascending: string;
  descending: string;
  direction: string;
  field: string;
  secondValue: string;
  removeFilter: string;
  removeSort: string;
  true: string;
  false: string;
}

export interface RichTextEditorMessages {
  heading: string;
  heading1: string;
  heading2: string;
  heading3: string;
  heading4: string;
  paragraph: string;
  bold: string;
  italic: string;
  link: string;
  underline: string;
  subscript: string;
  superscript: string;
  highlight: string;
  alignLeft: string;
  alignCenter: string;
  alignRight: string;
  justify: string;
  bulletList: string;
  orderedList: string;
  taskList: string;
  insertTable: string;
  insertMath: string;
  insertVideo: string;
  insertAudio: string;
  uploadImage: string;
  uploadFile: string;
  insertLinkTitle: string;
  insertVideoTitle: string;
  insertAudioTitle: string;
  insertLatexTitle: string;
  latexPlaceholder: string;
}

export interface DateTimeRangeMessages {
  start: string;
  end: string;
  pickStartDateTime: string;
  pickEndDateTime: string;
  lastYear: string;
  lastMonth: string;
  lastWeek: string;
  yesterday: string;
  now: string;
  tomorrow: string;
  nextWeek: string;
  nextMonth: string;
  nextYear: string;
  from: string;
  until: string;
}

export interface AddressMessages {
  country: string;
  state: string;
  city: string;
  street: string;
  postalCode: string;
  selectCountry: string;
  selectState: string;
  selectCity: string;
  streetAddress: string;
  postalCodePlaceholder: string;
  selectCountryFirst: string;
  selectOrType: string;
  useCustomValue: (value: string) => string;
  nothingFound: string;
  moreAddressFields: string;
  clear: string;
}

export interface AttachmentsMessages {
  noAttachments: string;
  deleteAttachmentTitle: string;
  deleteAttachmentConfirm: (fileName: string) => string;
  confirm: string;
  cancel: string;
  delete: string;
  copyLink: string;
  copied: string;
  download: string;
  addFiles: string;
  clearAll: string;
  uploadFiles: (count: number) => string;
  maxFilesReached: (max: number) => string;
  dragDropOrClick: string;
  dragDropFilesHere: string;
  remove: string;
  category: string;
  attachments: string;
  fileTooLarge: (maxSize: string, fileNames: string) => string;
  failedToUpload: (fileNames: string) => string;
}

export interface ThemeSwitcherMessages {
  toggleColorScheme: string;
  changeTheme: string;
  theme: string;
}

export interface MasterDetailMessages {
  noItemsFound: string;
  sortBy: string;
  loadMore: (loaded: number, total?: number) => string;
}

export interface SplitButtonMessages {
  moreOptions: string;
}

export interface AutocompleteMessages {
  searchPlaceholder: string;
  loading: string;
  noOptionsFound: string;
}

export interface AQMessages {
  dataGrid: DataGridMessages;
  columnFilter: ColumnFilterMessages;
  cardDataGrid: CardDataGridMessages;
  richTextEditor: RichTextEditorMessages;
  dateTimeRange: DateTimeRangeMessages;
  address: AddressMessages;
  attachments: AttachmentsMessages;
  themeSwitcher: ThemeSwitcherMessages;
  masterDetail: MasterDetailMessages;
  splitButton: SplitButtonMessages;
  autocomplete: AutocompleteMessages;
}

/** Deep partial helper: allows callers to override any subset of nested message keys/functions. */
export type DeepPartial<T> = T extends (...args: infer A) => infer R
  ? (...args: A) => R
  : T extends object
    ? { [K in keyof T]?: DeepPartial<T[K]> }
    : T;
