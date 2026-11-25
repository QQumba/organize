/** Minimum time between two refreshes caused by returning to the tab. */
export const focusRefreshInterval = 5000;

export type FocusRefreshState = {
	enabled: boolean;
	/** The Core-unreachable banner is up; its Retry button reloads instead. */
	unreachable: boolean;
	/** A form on the page has unsaved changes. */
	unsaved: boolean;
	now: number;
	/** When the page data was last refreshed (or loaded), in the same unit as `now`. */
	last: number;
};

/** Whether returning to the tab or window should reload the current page's data. */
export function shouldRefresh(s: FocusRefreshState): boolean {
	if (!s.enabled || s.unreachable || s.unsaved) return false;
	return s.now - s.last >= focusRefreshInterval;
}
