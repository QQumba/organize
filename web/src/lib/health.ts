export type HealthOutcome = { status: number } | { error: unknown };

/** Maps the result of `GET /api/health` to the one line the placeholder page shows. */
export function coreStatusText(outcome: HealthOutcome): string {
	if ('error' in outcome) return 'Core: unreachable';
	if (outcome.status === 200) return 'Core: ok';
	if (outcome.status === 401) return 'Core: up, login required';
	return 'Core: unreachable';
}
