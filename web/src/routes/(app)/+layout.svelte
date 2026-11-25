<script lang="ts">
	import { invalidateAll } from '$app/navigation';
	import { coreStatus } from '#lib/coreStatus.svelte.ts';
	import { refreshOnFocus } from '#lib/focusRefresh.svelte.ts';
	import { shouldRefresh } from '#lib/focusRefresh.ts';
	import { unsavedChanges } from '#lib/unsavedChanges.svelte.ts';
	import CaptureLine from './CaptureLine.svelte';
	import SearchBox from './SearchBox.svelte';
	import Sidebar from './Sidebar.svelte';

	let { children } = $props();

	// Refresh on focus: reload the current page's data when the user comes back to the tab.
	let last = Date.now();

	function refreshIfDue() {
		if (document.visibilityState !== 'visible') return;
		const now = Date.now();
		const due = shouldRefresh({
			enabled: refreshOnFocus.enabled,
			unreachable: coreStatus.unreachable,
			unsaved: unsavedChanges.dirty,
			now,
			last
		});
		if (!due) return;
		last = now;
		void invalidateAll();
	}
</script>

<svelte:window onfocus={refreshIfDue} />
<svelte:document onvisibilitychange={refreshIfDue} />

<div class="flex min-h-screen">
	<Sidebar />
	<div class="min-w-0 flex-1">
		<header class="flex items-start gap-3 border-b border-border p-2">
			<CaptureLine />
			<SearchBox />
		</header>
		<main class="mx-auto max-w-190 p-4 pt-6">
			{@render children()}
		</main>
	</div>
</div>
