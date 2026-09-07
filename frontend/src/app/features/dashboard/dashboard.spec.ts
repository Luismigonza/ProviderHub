import { CountryIndicator } from '../../core/api/models';
import { toRows } from './dashboard';

function indicator(
  countryCode: string,
  serviceCount: number,
  providerCount: number,
): CountryIndicator {
  return { countryCode, countryName: countryCode, serviceCount, providerCount };
}

describe('toRows', () => {
  it('gives the leader of each column a full bar', () => {
    const rows = toRows([indicator('CO', 11, 9), indicator('PE', 5, 5)]);

    expect(rows[0].servicesWidth).toBe(100);
    expect(rows[0].providersWidth).toBe(100);
  });

  it('scales the rest against that leader', () => {
    // Relative to the leader, not to a fixed maximum or to the sum: the shape of the
    // distribution is what the eye is meant to pick up.
    const rows = toRows([indicator('CO', 10, 8), indicator('PE', 5, 2)]);

    expect(rows[1].servicesWidth).toBe(50);
    expect(rows[1].providersWidth).toBe(25);
  });

  it('scales each column on its own', () => {
    // Services and providers are different quantities. Sharing one scale would make a country
    // with many providers and few services look like it had many of both.
    const rows = toRows([indicator('CO', 100, 2), indicator('PE', 50, 1)]);

    expect(rows[1].servicesWidth).toBe(50);
    expect(rows[1].providersWidth).toBe(50);
  });

  it('survives an empty summary without dividing by zero', () => {
    expect(toRows([])).toEqual([]);
  });

  it('survives a country with nothing in it', () => {
    const rows = toRows([indicator('AQ', 0, 0)]);

    expect(rows[0].servicesWidth).toBe(0);
    expect(rows[0].providersWidth).toBe(0);
  });

  it('keeps the order the API sent', () => {
    // The server already ordered by how many services reach each country; re-sorting here
    // would be a second opinion nobody asked for.
    const rows = toRows([indicator('CO', 11, 9), indicator('PE', 5, 5), indicator('MX', 4, 2)]);

    expect(rows.map((row) => row.countryCode)).toEqual(['CO', 'PE', 'MX']);
  });
});
