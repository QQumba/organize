<script lang="ts">
	import { goto } from '$app/navigation';
	import { page } from '$app/state';
	import { api } from '#lib/api/client.ts';
	import { errorText } from '#lib/errorText.ts';
	import { landingPath } from '#lib/returnPath.ts';

	let secret = $state('');
	let busy = $state(false);
	let error = $state('');

	async function submit(event: SubmitEvent) {
		event.preventDefault();
		if (busy || !secret) return;
		busy = true;
		error = '';
		try {
			const { response, error: body } = await api.POST('/api/session', { body: { secret } });
			if (response.ok) {
				// eslint-disable-next-line svelte/no-navigation-without-resolve -- app has no base path; the target is a checked same-origin path
				await goto(landingPath(page.url.searchParams.get('return')), { replace: true });
			} else {
				error = errorText({ response, error: body }, { wrongSecret: true });
			}
		} catch (thrown) {
			error = errorText({ thrown });
		} finally {
			busy = false;
		}
	}
</script>

<svelte:head><title>Log in</title></svelte:head>

<main class="mx-auto mt-24 max-w-sm space-y-3 p-4">
	<h1 class="text-xl font-semibold">Organizer</h1>
	<form class="space-y-3" onsubmit={submit}>
		<label class="block space-y-1">
			<span class="text-text-muted">Secret</span>
			<input
				class="input w-full"
				type="password"
				name="secret"
				autocomplete="current-password"
				aria-invalid={error ? 'true' : undefined}
				bind:value={secret}
			/>
		</label>
		{#if error}<p class="text-danger" role="alert">{error}</p>{/if}
		<button class="btn btn-primary" type="submit" disabled={busy}>Log in</button>
	</form>
	<p class="text-xs text-text-muted">
		The secret is the <code>Secret</code> value in <code>core.json</code> (by default
		<code>%APPDATA%\Organizer\core.json</code>).
	</p>
</main>
