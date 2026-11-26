import { describe, expect, it } from 'vitest';
import { dueLabel, dueTooltip, isOverdue, localDateOf, localIsoDate, stampOf } from './dueDate';

// Thursday, 12 November 2026, local time, late in the day.
const today = new Date(2026, 10, 12, 23, 30);

describe('localIsoDate', () => {
	it('uses the local date', () => {
		expect(localIsoDate(today)).toBe('2026-11-12');
		expect(localIsoDate(new Date(2026, 0, 5, 0, 5))).toBe('2026-01-05');
	});
});

describe('isOverdue', () => {
	it('is true only before today', () => {
		expect(isOverdue('2026-11-11', today)).toBe(true);
		expect(isOverdue('2026-11-12', today)).toBe(false);
		expect(isOverdue('2026-11-13', today)).toBe(false);
		expect(isOverdue(null, today)).toBe(false);
	});
});

describe('dueLabel', () => {
	it('names today, tomorrow and yesterday', () => {
		expect(dueLabel('2026-11-12', today)).toBe('Today');
		expect(dueLabel('2026-11-13', today)).toBe('Tomorrow');
		expect(dueLabel('2026-11-11', today)).toBe('Yesterday');
	});

	it('is a short date in the current year', () => {
		expect(dueLabel('2026-11-20', today)).toBe('20 Nov');
		expect(dueLabel('2026-01-05', today)).toBe('5 Jan');
	});

	it('adds the year in another year', () => {
		expect(dueLabel('2027-01-05', today)).toBe('5 Jan 2027');
		expect(dueLabel('2025-12-31', today)).toBe('31 Dec 2025');
	});

	it('names tomorrow across a month and year end', () => {
		expect(dueLabel('2027-01-01', new Date(2026, 11, 31))).toBe('Tomorrow');
		expect(dueLabel('2026-12-31', new Date(2027, 0, 1))).toBe('Yesterday');
	});
});

describe('dueTooltip', () => {
	it('gives the weekday and the full date', () => {
		expect(dueTooltip('2026-11-12')).toBe('Thursday, 12 November 2026');
		expect(dueTooltip('2027-01-01')).toBe('Friday, 1 January 2027');
	});
});

describe('localDateOf', () => {
	it('maps a UTC date-time to the local calendar date', () => {
		// Built from a local time, so the result is the same in any time zone.
		const late = new Date(2026, 10, 12, 23, 59).toISOString();
		const early = new Date(2026, 10, 13, 0, 1).toISOString();
		expect(localDateOf(late)).toBe('2026-11-12');
		expect(localDateOf(early)).toBe('2026-11-13');
	});

	it('reads a value without a zone marker as UTC, like the Core sends it', () => {
		const utc = new Date(Date.UTC(2026, 10, 25, 23, 48, 14));
		expect(localDateOf('2026-11-25T23:48:14.0788039')).toBe(localIsoDate(utc));
		expect(localDateOf('2026-11-25T23:48:14.0788039Z')).toBe(localIsoDate(utc));
		expect(localDateOf('2026-11-26T01:48:14+02:00')).toBe(localIsoDate(utc));
	});
});

describe('stampOf', () => {
	it('prefixes the label and gives the full date as the tooltip', () => {
		const at = (y: number, m: number, d: number) => new Date(y, m, d, 12).toISOString();
		expect(stampOf('Done', at(2026, 10, 12), today)).toEqual({
			label: 'Done Today',
			title: 'Thursday, 12 November 2026'
		});
		expect(stampOf('Archived', at(2026, 10, 11), today).label).toBe('Archived Yesterday');
	});

	it('shows a short date in the current year and adds the year in another', () => {
		const at = (y: number, m: number, d: number) => new Date(y, m, d, 12).toISOString();
		expect(stampOf('Done', at(2026, 0, 5), today).label).toBe('Done 5 Jan');
		expect(stampOf('Done', at(2025, 11, 31), today).label).toBe('Done 31 Dec 2025');
	});
});
