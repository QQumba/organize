// The per-browser "Refresh on focus" switch (default on), kept in localStorage.
const key = 'organizer.refreshOnFocus';

function read(): boolean {
	try {
		return localStorage.getItem(key) !== 'off';
	} catch {
		return true;
	}
}

export const refreshOnFocus = $state({ enabled: read() });

export function setRefreshOnFocus(enabled: boolean) {
	refreshOnFocus.enabled = enabled;
	try {
		localStorage.setItem(key, enabled ? 'on' : 'off');
	} catch {
		// Private mode or storage disabled: the switch still works until the page reloads.
	}
}
