<script lang="ts">
	import { navigating } from '$app/state';

	// Shows only when a page load takes longer than `delay` ms, so quick loads do not flash.
	// `loading` exists so the gallery can force it; the app uses the navigation state.
	let { loading, delay = 300 }: { loading?: boolean; delay?: number } = $props();
	let late = $state(false);
	const active = $derived(loading ?? navigating.to !== null);

	$effect(() => {
		if (!active) return;
		const timer = setTimeout(() => (late = true), delay);
		return () => {
			clearTimeout(timer);
			late = false;
		};
	});
</script>

{#if late}
	<div
		role="progressbar"
		aria-label="Loading"
		class="fixed inset-x-0 top-0 z-40 h-0.5 bg-accent"
	></div>
{/if}
