<script lang="ts">
	import { invalidateAll } from '$app/navigation';
	import { api } from '#lib/api/client.ts';
	import { errorText } from '#lib/errorText.ts';

	const confirmMs = 5000;

	let text = $state('');
	let error = $state('');
	let capturedId = $state<number | null>(null);
	let timer: ReturnType<typeof setTimeout> | undefined;
	// A plain flag, not `disabled`: a disabled input would lose focus.
	let sending = false;

	async function submit(event: SubmitEvent) {
		event.preventDefault();
		if (sending || !text.trim()) return;
		sending = true;
		error = '';
		try {
			const { data, response, error: body } = await api.POST('/api/items', { body: { text } });
			if (data) {
				text = '';
				capturedId = data.id;
				clearTimeout(timer);
				timer = setTimeout(() => (capturedId = null), confirmMs);
				void invalidateAll();
			} else {
				capturedId = null;
				error = errorText({ response, error: body });
			}
		} catch (thrown) {
			capturedId = null;
			error = errorText({ thrown });
		} finally {
			sending = false;
		}
	}
</script>

<form class="relative min-w-0 flex-1" onsubmit={submit}>
	<input
		class="input w-full"
		type="text"
		name="capture"
		placeholder="Capture…"
		aria-label="Capture"
		autocomplete="off"
		aria-invalid={error ? 'true' : undefined}
		bind:value={text}
	/>
	<div class="absolute top-full left-0 z-10 mt-1 text-xs" role="status">
		{#if error}
			<span class="text-danger">{error}</span>
		{:else if capturedId !== null}
			<span class="text-text-muted">
				Captured ·
				<!-- eslint-disable-next-line svelte/no-navigation-without-resolve -- app has no base path; the route is added by the Item edit page ticket -->
				<a class="text-accent underline" href="/items/{capturedId}">open</a>
			</span>
		{/if}
	</div>
</form>
