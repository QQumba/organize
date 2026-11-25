// Shared state of the "Can't reach the Core" banner. Set by the API middleware, shown by the
// root layout. Hides on the next successful API response.
export const coreStatus = $state({ unreachable: false });

export function markUnreachable() {
	coreStatus.unreachable = true;
}

export function markReachable() {
	if (coreStatus.unreachable) coreStatus.unreachable = false;
}
