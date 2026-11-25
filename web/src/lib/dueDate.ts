// Display of an Item's due date (a `YYYY-MM-DD` string from the Core). "Today" is the browser's
// local date. Pure display: the Core has no overdue rule.

const months = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
const longMonths = [
	'January',
	'February',
	'March',
	'April',
	'May',
	'June',
	'July',
	'August',
	'September',
	'October',
	'November',
	'December'
];
const weekdays = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];

const dayMs = 86_400_000;

function parts(due: string): { y: number; m: number; d: number } {
	const [y, m, d] = due.split('-').map(Number);
	return { y, m, d };
}

/** The local calendar date of `date` as `YYYY-MM-DD`. */
export function localIsoDate(date: Date): string {
	const p = (n: number, width = 2) => String(n).padStart(width, '0');
	return `${p(date.getFullYear(), 4)}-${p(date.getMonth() + 1)}-${p(date.getDate())}`;
}

/** Whole days from `today` to `due` (negative: in the past). */
function daysFromToday(due: string, today: Date): number {
	const a = parts(due);
	const b = parts(localIsoDate(today));
	return Math.round((Date.UTC(a.y, a.m - 1, a.d) - Date.UTC(b.y, b.m - 1, b.d)) / dayMs);
}

/** True when the due date is before today. */
export function isOverdue(due: string | null, today: Date): boolean {
	return due !== null && due < localIsoDate(today);
}

/** "Today", "Tomorrow", "Yesterday", otherwise "12 Nov", with the year when not the current one. */
export function dueLabel(due: string, today: Date): string {
	const days = daysFromToday(due, today);
	if (days === 0) return 'Today';
	if (days === 1) return 'Tomorrow';
	if (days === -1) return 'Yesterday';
	const { y, m, d } = parts(due);
	const short = `${d} ${months[m - 1]}`;
	return y === today.getFullYear() ? short : `${short} ${y}`;
}

/** The full date for a tooltip: "Thursday, 12 November 2026". */
export function dueTooltip(due: string): string {
	const { y, m, d } = parts(due);
	const weekday = weekdays[new Date(Date.UTC(y, m - 1, d)).getUTCDay()];
	return `${weekday}, ${d} ${longMonths[m - 1]} ${y}`;
}
