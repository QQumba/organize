import { describe, expect, it } from 'vitest';
import { dueLabel, dueTooltip, isOverdue, localIsoDate } from './dueDate';

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
