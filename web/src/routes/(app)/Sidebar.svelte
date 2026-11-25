<script lang="ts">
	import { page } from '$app/state';
	import { refreshOnFocus, setRefreshOnFocus } from '#lib/focusRefresh.svelte.ts';
	import { logout } from '#lib/session.ts';

	// App has no base path, so plain hrefs are fine (the lint rule only knows `resolve`).
	const links = [
		{ href: '/inbox', label: 'Inbox' },
		{ href: '/done', label: 'Done' },
		{ href: '/archive', label: 'Archive' },
		{ href: '/categories', label: 'Categories' }
	];

	const current = (href: string) =>
		page.url.pathname === href || page.url.pathname.startsWith(href + '/');
</script>

<aside
	class="sticky top-0 flex h-screen w-45 shrink-0 flex-col gap-1 border-r border-border bg-surface-raised p-2"
>
	<div class="px-2 pt-1 pb-3 text-base font-semibold">Organizer</div>
	<nav class="flex flex-col gap-0.5" aria-label="Main">
		<!-- eslint-disable svelte/no-navigation-without-resolve -- app has no base path -->
		{#each links as link (link.href)}
			<a
				href={link.href}
				class="h-control rounded-control px-2 leading-7 hover:bg-surface"
				class:bg-surface={current(link.href)}
				class:font-semibold={current(link.href)}
				aria-current={current(link.href) ? 'page' : undefined}>{link.label}</a
			>
		{/each}
		<!-- eslint-enable svelte/no-navigation-without-resolve -->
	</nav>
	<div class="mt-auto flex flex-col gap-2">
		<label class="flex items-center gap-2 px-2 text-xs text-text-muted">
			<input
				type="checkbox"
				class="accent-accent"
				checked={refreshOnFocus.enabled}
				onchange={(e) => setRefreshOnFocus(e.currentTarget.checked)}
			/>
			Refresh on focus
		</label>
		<button type="button" class="btn btn-secondary" onclick={logout}>Log out</button>
	</div>
</aside>
