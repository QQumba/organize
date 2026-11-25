<script lang="ts">
	import { coreStatus } from '#lib/coreStatus.svelte.ts';
	import { unreachableText } from '#lib/errorText.ts';
	import { retry } from '#lib/session.ts';

	// `visible` and `onretry` exist so the gallery can show the banner; the app uses the defaults.
	let {
		visible = coreStatus.unreachable,
		onretry = retry
	}: { visible?: boolean; onretry?: () => Promise<unknown> | void } = $props();
	let busy = $state(false);

	async function run() {
		busy = true;
		try {
			await onretry();
		} finally {
			busy = false;
		}
	}
</script>

{#if visible}
	<div
		role="alert"
		class="fixed inset-x-0 top-0 z-50 flex items-center justify-center gap-3 border-b border-danger bg-surface-raised px-3 py-2 text-danger"
	>
		<span>{unreachableText}</span>
		<button class="btn btn-secondary" disabled={busy} onclick={run}>Retry</button>
	</div>
{/if}
