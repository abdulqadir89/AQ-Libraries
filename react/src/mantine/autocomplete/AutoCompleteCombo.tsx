import { useState, forwardRef, useMemo } from 'react';
import { useQuery } from '@tanstack/react-query';
import {
  Select,
  MultiSelect,
} from '@mantine/core';
import type { SelectProps, MultiSelectProps } from '@mantine/core';
import { useDebouncedValue } from '@mantine/hooks';

export interface AutoCompleteItem {
  value: string;
  label: string;
  disabled?: boolean;
}

export interface AutoCompleteComboProps {
  // Core functionality
  fetchData: (searchTerm: string) => Promise<AutoCompleteItem[]>;
  placeholder?: string;
  label?: string;
  description?: string;
  error?: string;
  required?: boolean;
  disabled?: boolean;
  
  // Selection configuration
  maxValues?: number | null; // null = single select, number > 1 = multi select
  
  // Value handling
  value?: string | string[] | null;
  onChange?: (value: string | string[] | null) => void;
  
  // Preloaded data for form editing
  preloadedData?: AutoCompleteItem[];
  
  // Search configuration
  debounceMs?: number; // Debounce delay for API calls (default: 300)
  minSearchLength?: number; // Minimum characters to trigger search (default: 0)
  
  // UI configuration
  withAsterisk?: boolean;
  size?: 'xs' | 'sm' | 'md' | 'lg' | 'xl';
  clearable?: boolean;
  searchable?: boolean;
  
  // Limit rendering performance
  maxDropdownHeight?: number;
  limit?: number; // Max items to show in dropdown (default: 10)
  
  // Caching
  cacheKey?: string; // Key for React Query caching
}

export const AutoCompleteCombo = forwardRef<HTMLInputElement, AutoCompleteComboProps>(
  (
    {
      fetchData,
      placeholder = 'Search...',
      label,
      description,
      error,
      required = false,
      disabled = false,
      maxValues = null,
      value,
      onChange,
      preloadedData = [],
      debounceMs = 300,
      minSearchLength = 0,
      withAsterisk,
      size = 'sm',
      clearable = true,
      searchable = true,
      maxDropdownHeight = 200,
      limit = 10,
      cacheKey, // Add cache key prop for React Query
      ...props
    },
    ref
  ) => {
    // State management
    const [searchValue, setSearchValue] = useState('');
    const [debouncedSearch] = useDebouncedValue(searchValue, debounceMs);

    // Determine if this should be multi-select
    const isMultiSelect = maxValues === null ? false : maxValues > 1;

    // Use React Query for caching search results
    const searchQuery = useQuery({
      queryKey: [cacheKey || 'autocomplete-search', debouncedSearch, minSearchLength],
      queryFn: async () => {
        if (debouncedSearch.length < minSearchLength) {
          if (minSearchLength === 0) {
            // If minSearchLength is 0, fetch initial data
            return await fetchData('');
          }
          return [];
        }
        return await fetchData(debouncedSearch);
      },
      enabled: searchable && (debouncedSearch.length >= minSearchLength || minSearchLength === 0),
      staleTime: 1 * 60 * 1000, // 1 minute
      gcTime: 3 * 60 * 1000, // 3 minutes
      retry: 1,
    });

    // Combine preloaded data with fetched data, avoiding duplicates
    const allData = useMemo(() => {
      const fetchedData = searchQuery.data || [];
      const fetchedItems = fetchedData.filter((item: AutoCompleteItem) => 
        !preloadedData.some(preloaded => preloaded.value === item.value)
      );
      return [...preloadedData, ...fetchedItems].slice(0, limit);
    }, [searchQuery.data, preloadedData, limit]);

    // Handle value changes
    const handleChange = (newValue: string | string[] | null) => {
      onChange?.(newValue);
    };

    // Handle search value changes
    const handleSearchChange = (search: string) => {
      setSearchValue(search);
    };

    // Common props for both Select and MultiSelect
    const commonProps = {
      ref,
      label,
      description,
      error,
      placeholder,
      required,
      disabled,
      withAsterisk,
      size,
      clearable,
      searchable,
      data: allData,
      value: value,
      onChange: handleChange,
      onSearchChange: searchable ? handleSearchChange : undefined,
      searchValue: searchable ? searchValue : undefined,
      maxDropdownHeight,
      limit,
      nothingFoundMessage: searchQuery.isLoading ? 'Loading...' : 'No options found',
      ...props,
    };

    if (isMultiSelect) {
      const multiSelectProps: MultiSelectProps = {
        ...commonProps,
        value: (value as string[]) || [],
        onChange: handleChange as (value: string[]) => void,
        maxValues: maxValues || undefined,
      };

      return <MultiSelect {...multiSelectProps} />;
    } else {
      const selectProps: SelectProps = {
        ...commonProps,
        value: (value as string) || null,
        onChange: handleChange as (value: string | null) => void,
      };

      return <Select {...selectProps} />;
    }
  }
);

AutoCompleteCombo.displayName = 'AutoCompleteCombo';

export default AutoCompleteCombo;
