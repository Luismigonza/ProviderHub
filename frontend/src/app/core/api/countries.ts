/**
 * The countries offered in the pickers.
 *
 * Only the codes are kept: the names come from `Intl.DisplayNames`, which the browser already
 * ships and keeps up to date, so there is no list of translated country names to maintain here
 * and no way for a name to drift from the code it belongs to. The server does the same thing
 * with .NET's `RegionInfo`, which is why both sides agree.
 *
 * This is a working subset, not the whole standard. A catalogue that needs all 249 should be
 * served by the API so that both sides read from one source; hard-coding the full list in the
 * client would be the second copy this comment is trying to avoid.
 */
const SUPPORTED_COUNTRY_CODES = [
  'AR', 'BO', 'BR', 'CA', 'CL', 'CO', 'CR', 'CU', 'DE', 'DO',
  'EC', 'ES', 'FR', 'GB', 'GT', 'HN', 'IT', 'MX', 'NI', 'PA',
  'PE', 'PR', 'PT', 'PY', 'SV', 'US', 'UY', 'VE',
] as const;

export interface CountryOption {
  readonly code: string;
  readonly name: string;
}

const displayNames = new Intl.DisplayNames(['en'], { type: 'region' });

/** The pickable countries, already sorted by name. */
export const COUNTRY_OPTIONS: readonly CountryOption[] = SUPPORTED_COUNTRY_CODES.map((code) => ({
  code,
  name: displayNames.of(code) ?? code,
})).sort((left, right) => left.name.localeCompare(right.name));
