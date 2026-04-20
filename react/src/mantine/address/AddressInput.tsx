import React, { useState, useEffect, useMemo } from 'react';
import {
  Stack, Group, Collapse, Button, Combobox, InputBase, useCombobox,
  TextInput, Text, ActionIcon,
} from '@mantine/core';
import { IconChevronDown, IconChevronUp, IconX } from '@tabler/icons-react';
import { COUNTRIES_SORTED, findCountry, findState } from './data';

// ─── Value / Field Types ───────────────────────────────────────────────────────

export interface AddressValue {
  country?: string;   // ISO alpha-2 code  e.g. "US"
  state?: string;     // state/province code e.g. "CA"
  city?: string;      // city name
  street?: string;    // street address
  postalCode?: string;
}

export type AddressField = 'country' | 'state' | 'city' | 'street' | 'postalCode';

export interface AddressFieldConfig {
  field: AddressField;
  label?: string;
  placeholder?: string;
  required?: boolean;
}

export interface AddressInputProps {
  value?: AddressValue;
  onChange?: (value: AddressValue) => void;

  /**
   * Ordered list of fields to display in the main (always-visible) section.
   * Defaults to: ['country', 'state', 'city', 'street', 'postalCode']
   */
  fields?: AddressFieldConfig[];

  /**
   * Fields placed in the collapsible "Optional" section.
   * User can expand to reveal and fill these.
   */
  optionalFields?: AddressFieldConfig[];

  /** Label shown on the expand button. Default: "More address fields" */
  optionalSectionLabel?: string;

  /** Error map: field -> error string */
  errors?: Partial<Record<AddressField, string>>;

  disabled?: boolean;
}

// ─── CreatableSelect ──────────────────────────────────────────────────────────

interface CreatableSelectProps {
  label?: string;
  placeholder?: string;
  required?: boolean;
  disabled?: boolean;
  error?: string;
  data: string[];
  value: string;
  onChange: (value: string) => void;
  onClear?: () => void;
}

/**
 * A Mantine Combobox-based select that allows the user to type a custom value
 * if their option doesn't appear in the list.
 */
function CreatableSelect({
  label, placeholder, required, disabled, error, data, value, onChange, onClear,
}: CreatableSelectProps) {
  const combobox = useCombobox({
    onDropdownClose: () => combobox.resetSelectedOption(),
  });
  const [search, setSearch] = useState(value);

  // Keep search in sync only when the dropdown is closed (i.e. user isn't
  // actively typing). This prevents external value changes from overwriting
  // what the user is currently typing.
  useEffect(() => {
    if (!combobox.dropdownOpened) {
      setSearch(value);
    }
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [value]);

  const filtered = useMemo(() => {
    const q = search.toLowerCase().trim();
    if (!q) return data;
    return data.filter(item => item.toLowerCase().includes(q));
  }, [data, search]);

  const exactMatch = data.some(item => item.toLowerCase() === search.toLowerCase());

  const options = [
    ...filtered.map(item => (
      <Combobox.Option key={item} value={item}>
        {item}
      </Combobox.Option>
    )),
    !exactMatch && search.trim() ? (
      <Combobox.Option key="__custom__" value={search.trim()}>
        <Text size="sm" c="dimmed">Use &ldquo;{search.trim()}&rdquo;</Text>
      </Combobox.Option>
    ) : null,
  ].filter(Boolean);

  return (
    <Combobox
      store={combobox}
      onOptionSubmit={(val: string) => {
        onChange(val);
        setSearch(val);
        combobox.closeDropdown();
      }}
    >
      <Combobox.Target>
        <InputBase
          label={label}
          placeholder={placeholder ?? 'Select or type…'}
          required={required}
          disabled={disabled}
          error={error}
          value={search}
          pointer
          rightSection={
            !disabled && search ? (
              <ActionIcon
                variant="subtle"
                color="gray"
                size="sm"
                aria-label="Clear"
                onClick={(e) => {
                  e.stopPropagation();
                  setSearch('');
                  onClear ? onClear() : onChange('');
                  combobox.closeDropdown();
                }}
              >
                <IconX size={12} />
              </ActionIcon>
            ) : (
              <Combobox.Chevron />
            )
          }
          rightSectionPointerEvents={!disabled && search ? 'all' : 'none'}
          onChange={(e: React.ChangeEvent<HTMLInputElement>) => {
            const v = e.currentTarget.value;
            setSearch(v);
            // Do NOT call onChange here — only filter the dropdown.
            // Value is committed on option select or blur.
            combobox.openDropdown();
            combobox.updateSelectedOptionIndex();
          }}
          onFocus={() => combobox.openDropdown()}
          onBlur={() => {
            combobox.closeDropdown();
            // Commit whatever is in the search box when user leaves
            onChange(search.trim());
          }}
          onClick={() => combobox.openDropdown()}
        />
      </Combobox.Target>
      <Combobox.Dropdown>
        <Combobox.Options mah={200} style={{ overflowY: 'auto' }}>
          {options.length > 0
            ? options
            : <Combobox.Empty>Nothing found</Combobox.Empty>}
        </Combobox.Options>
      </Combobox.Dropdown>
    </Combobox>
  );
}

// ─── Default field configs ─────────────────────────────────────────────────────

const DEFAULT_FIELDS: AddressFieldConfig[] = [
  { field: 'country', label: 'Country', placeholder: 'Select country', required: false },
  { field: 'state', label: 'State / Province', placeholder: 'Select state', required: false },
  { field: 'city', label: 'City', placeholder: 'Select city', required: false },
  { field: 'street', label: 'Street', placeholder: 'Street address', required: false },
  { field: 'postalCode', label: 'Postal Code', placeholder: 'Postal / ZIP code', required: false },
];

function getDefaultConfig(field: AddressField): AddressFieldConfig {
  return DEFAULT_FIELDS.find(f => f.field === field) ?? { field };
}

// ─── Main Component ───────────────────────────────────────────────────────────

export function AddressInput({
  value = {},
  onChange,
  fields = DEFAULT_FIELDS,
  optionalFields = [],
  optionalSectionLabel = 'More address fields',
  errors = {},
  disabled = false,
}: AddressInputProps) {
  const [optionalOpen, setOptionalOpen] = useState(false);

  const update = (patch: Partial<AddressValue>) => {
    onChange?.({ ...value, ...patch });
  };

  const handleCountryChange = (newCountry: string) => {
    // Find country by name or code
    const match = COUNTRIES_SORTED.find(
      c => c.name.toLowerCase() === newCountry.toLowerCase() ||
           c.code.toLowerCase() === newCountry.toLowerCase()
    );
    const code = match?.code ?? newCountry;
    // Reset state and city when country changes
    update({ country: code, state: undefined, city: undefined });
  };

  const handleStateChange = (newState: string) => {
    const country = findCountry(value.country ?? '');
    const match = country?.states.find(
      s => s.name.toLowerCase() === newState.toLowerCase() ||
           s.code.toLowerCase() === newState.toLowerCase()
    );
    const code = match?.code ?? newState;
    // Reset city when state changes
    update({ state: code, city: undefined });
  };

  const handleCityChange = (city: string) => {
    update({ city });
  };

  // Derive display values (names, not codes)
  const countryObj = findCountry(value.country ?? '');
  const countryDisplayValue = countryObj?.name ?? value.country ?? '';

  const stateObj = findState(value.country ?? '', value.state ?? '');
  const stateDisplayValue = stateObj?.name ?? value.state ?? '';

  // Dropdown data
  const countryOptions = COUNTRIES_SORTED.map(c => c.name);
  const stateOptions = countryObj?.states.map(s => s.name) ?? [];
  // Collect all cities from the selected country (all states), deduplicated
  const cityOptions = useMemo(() => {
    if (!countryObj) return [];
    const seen = new Set<string>();
    const all: string[] = [];
    // If a state is selected, put its cities first
    if (stateObj) {
      for (const c of stateObj.cities) {
        if (!seen.has(c.name)) { seen.add(c.name); all.push(c.name); }
      }
    }
    for (const s of countryObj.states) {
      for (const c of s.cities) {
        if (!seen.has(c.name)) { seen.add(c.name); all.push(c.name); }
      }
    }
    return all;
  }, [countryObj, stateObj]);

  const renderField = (cfg: AddressFieldConfig) => {
    const c = { ...getDefaultConfig(cfg.field), ...cfg };
    switch (c.field) {
      case 'country':
        return (
          <CreatableSelect
            key="country"
            label={c.label}
            placeholder={c.placeholder}
            required={c.required}
            disabled={disabled}
            error={errors.country}
            data={countryOptions}
            value={countryDisplayValue}
            onChange={handleCountryChange}
            onClear={() => update({ country: undefined, state: undefined, city: undefined })}
          />
        );
      case 'state':
        return (
          <CreatableSelect
            key="state"
            label={c.label}
            placeholder={!value.country ? 'Select a country first' : c.placeholder}
            required={c.required}
            disabled={disabled || !value.country}
            error={errors.state}
            data={stateOptions}
            value={stateDisplayValue}
            onChange={handleStateChange}
            onClear={() => update({ state: undefined, city: undefined })}
          />
        );
      case 'city':
        return (
          <CreatableSelect
            key="city"
            label={c.label}
            placeholder={c.placeholder}
            required={c.required}
            disabled={disabled}
            error={errors.city}
            data={cityOptions}
            value={value.city ?? ''}
            onChange={handleCityChange}
            onClear={() => update({ city: undefined })}
          />
        );
      case 'street':
        return (
          <TextInput
            key="street"
            label={c.label}
            placeholder={c.placeholder}
            required={c.required}
            disabled={disabled}
            error={errors.street}
            value={value.street ?? ''}
            onChange={(e) => { const v = e.currentTarget.value; update({ street: v }); }}
          />
        );
      case 'postalCode':
        return (
          <TextInput
            key="postalCode"
            label={c.label}
            placeholder={c.placeholder}
            required={c.required}
            disabled={disabled}
            error={errors.postalCode}
            value={value.postalCode ?? ''}
            onChange={(e) => { const v = e.currentTarget.value; update({ postalCode: v }); }}
          />
        );
      default:
        return null;
    }
  };

  return (
    <Stack gap="sm">
      {fields.map(renderField)}

      {optionalFields.length > 0 && (
        <>
          <Group>
            <Button
              variant="subtle"
              size="xs"
              rightSection={optionalOpen ? <IconChevronUp size={14} /> : <IconChevronDown size={14} />}
              onClick={() => setOptionalOpen(o => !o)}
            >
              {optionalSectionLabel}
            </Button>
          </Group>
          <Collapse expanded={optionalOpen}>
            <Stack gap="sm">
              {optionalFields.map(renderField)}
            </Stack>
          </Collapse>
        </>
      )}
    </Stack>
  );
}
