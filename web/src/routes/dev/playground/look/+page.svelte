<!--
	Question: which dark palette and accent should the web UI use?
	Date: 2026-11-14
	Ticket: ATL-55
	Six variants of one fake Inbox page, chosen with ?v=a to ?v=f. Reference only: copy the winner's
	token values into the real @theme, not this code. No API calls, no state.
-->
<script lang="ts">
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { page } from '$app/state';

	const variants = ['a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i'] as const;
	const names: Record<string, string> = {
		a: 'A · slate, blue',
		b: 'B · near-black, green',
		c: 'C · warm brown, amber',
		d: 'D · blue-green, teal',
		e: 'E · charcoal, orange',
		f: 'F · violet, rose',
		g: 'G · charcoal, blue',
		h: 'H · white, blue (light)',
		i: 'I · cool gray, indigo-blue (light)'
	};

	const v = $derived.by(() => {
		const q = page.url.searchParams.get('v');
		return q && (variants as readonly string[]).includes(q) ? q : 'a';
	});

	function pick(event: MouseEvent, id: string) {
		event.preventDefault();
		goto(resolve(`/dev/playground/look?v=${id}`), { replace: true, reset: false });
	}

	const self = resolve('/dev/playground/look');
	const day = 86_400_000;
	function due(offset: number | null) {
		if (offset === null) return null;
		const d = new Date(Date.now() + offset * day);
		const p = (n: number) => String(n).padStart(2, '0');
		return { text: `${p(d.getDate())}.${p(d.getMonth() + 1)}`, overdue: offset < 0 };
	}

	const rows = [
		{ text: 'Купити молоко та хліб', cat: 'Дім', tags: [], imp: false, urg: false, due: due(1) },
		{
			text: 'Підготувати звіт для п’ятничної зустрічі',
			cat: 'Робота',
			tags: ['звіт', 'команда'],
			imp: true,
			urg: false,
			due: due(3)
		},
		{ text: 'Зателефонувати лікарю', cat: '', tags: [], imp: false, urg: true, due: due(-2) },
		{
			text: 'Оплатити інтернет і комунальні послуги',
			cat: 'Дім',
			tags: ['рахунки'],
			imp: true,
			urg: true,
			due: due(0)
		},
		{
			text: 'Переглянути запит на злиття від Єгора',
			cat: 'Робота',
			tags: ['код', 'огляд', 'команда'],
			imp: false,
			urg: false,
			due: due(5)
		},
		{
			text: 'Записати ідеї про подорож до Карпат',
			cat: '',
			tags: ['ідеї'],
			imp: false,
			urg: false,
			due: due(null)
		},
		{
			text: 'Придбати квитки на «Лебедине озеро» для всієї сім’ї, а також замовити столик у ресторані біля театру та попередити бабусю про час зустрічі',
			cat: 'Родина',
			tags: ['події'],
			imp: true,
			urg: false,
			due: due(10)
		},
		{ text: 'Прочитати розділ про їжаків', cat: '', tags: [], imp: false, urg: false, due: due(7) }
	];
</script>

<svelte:head><title>Look prototype</title></svelte:head>

<div class="root" data-variant={v}>
	<header class="topbar">
		<nav class="nav">
			<a href={self} class="active">Inbox</a>
			<a href={self}>Done</a>
			<a href={self}>Archive</a>
			<a href={self}>Categories</a>
		</nav>
		<input class="field search" type="search" placeholder="Search" aria-label="Search" />
		<input class="field capture" type="text" placeholder="Capture…" aria-label="Capture" />
	</header>

	<main class="column">
		<nav class="switcher" aria-label="Variant">
			{#each variants as id (id)}
				<a
					href={resolve(`/dev/playground/look?v=${id}`)}
					class:current={id === v}
					onclick={(e) => pick(e, id)}>{names[id]}</a
				>
			{/each}
		</nav>
		{#if v === 'f'}
			<p class="note">
				F: violet-tinted surfaces with a rose accent. It is the only variant with a purple surface
				hue and a pink accent.
			</p>
		{/if}

		<ul class="rows">
			{#each rows as row (row.text)}
				<li class="row">
					<input type="checkbox" aria-label="Done" />
					{#if row.imp}<span class="mark imp" title="Important" aria-label="Important">■</span>{/if}
					{#if row.urg}<span class="mark urg" title="Urgent" aria-label="Urgent">▲</span>{/if}
					<span class="text">{row.text}</span>
					{#if row.cat}<span class="cat">{row.cat}</span>{/if}
					{#each row.tags as tag (tag)}<span class="chip">{tag}</span>{/each}
					<span class="actions">
						<button class="link" type="button">Archive</button>
						<button class="link" type="button">Edit</button>
					</span>
					<span class="due" class:overdue={row.due?.overdue}>{row.due?.text ?? ''}</span>
				</li>
			{/each}
		</ul>

		<section class="samples">
			<span class="chip">Tag chip</span>
			<button class="btn primary" type="button">Primary</button>
			<button class="btn secondary" type="button">Secondary</button>
			<button class="btn danger" type="button">Danger</button>
			<input class="field" type="text" value="Text input" aria-label="Text input" />
			<input class="field" type="date" value="2026-11-14" aria-label="Date input" />
			<a class="accent-link" href={self}>Accent link</a>
		</section>
	</main>
</div>

<style>
	.root {
		color-scheme: dark;
		min-height: 100vh;
		background: var(--surface);
		color: var(--text);
		font:
			14px/1.4 system-ui,
			-apple-system,
			'Segoe UI',
			sans-serif;
	}
	.root[data-variant='a'] {
		--surface: #0f1520;
		--surface-raised: #19212f;
		--border: #2c3748;
		--text: #e6eaf2;
		--text-muted: #9aa6ba;
		--accent: #5b9bff;
		--accent-contrast: #08111f;
		--danger: #ff6b6b;
		--focus-ring: #8bb8ff;
		--mark-important: #e5c04a;
		--mark-urgent: #c49bff;
	}
	.root[data-variant='b'] {
		--surface: #0e0e0e;
		--surface-raised: #181818;
		--border: #2e2e2e;
		--text: #ececec;
		--text-muted: #a0a0a0;
		--accent: #4ade80;
		--accent-contrast: #04140a;
		--danger: #ff6b6b;
		--focus-ring: #86efac;
		--mark-important: #60a5fa;
		--mark-urgent: #fb923c;
	}
	.root[data-variant='c'] {
		--surface: #1a1612;
		--surface-raised: #251f19;
		--border: #3d342a;
		--text: #efe6da;
		--text-muted: #b0a290;
		--accent: #f5b942;
		--accent-contrast: #1d1405;
		--danger: #ef6a5a;
		--focus-ring: #fcd27a;
		--mark-important: #7cc4f0;
		--mark-urgent: #c79bf2;
	}
	.root[data-variant='d'] {
		--surface: #0b1819;
		--surface-raised: #122427;
		--border: #23393c;
		--text: #e0f0ef;
		--text-muted: #8fb0ae;
		--accent: #2dd4bf;
		--accent-contrast: #04201c;
		--danger: #ff7a7a;
		--focus-ring: #7eead9;
		--mark-important: #e9d36a;
		--mark-urgent: #b79cff;
	}
	.root[data-variant='e'] {
		--surface: #1c1c1e;
		--surface-raised: #262628;
		--border: #3a3a3d;
		--text: #eeeeee;
		--text-muted: #a5a5a8;
		--accent: #ff8a3d;
		--accent-contrast: #1f0d02;
		--danger: #ff5c6c;
		--focus-ring: #ffb27a;
		--mark-important: #5eead4;
		--mark-urgent: #a78bfa;
	}
	.root[data-variant='f'] {
		--surface: #16111f;
		--surface-raised: #201829;
		--border: #352a45;
		--text: #ece6f5;
		--text-muted: #a89bbd;
		--accent: #f472a6;
		--accent-contrast: #2a0615;
		--danger: #ff7b5c;
		--focus-ring: #f9a8c9;
		--mark-important: #7dd3a8;
		--mark-urgent: #f5d76e;
	}
	/* G (added later, 2026-11-14, ATL-55 follow-up): E's charcoal surfaces with A's blue accent. */
	.root[data-variant='g'] {
		--surface: #1c1c1e;
		--surface-raised: #262628;
		--border: #3a3a3d;
		--text: #eeeeee;
		--text-muted: #a5a5a8;
		--accent: #6aa5ff;
		--accent-contrast: #07142b;
		--danger: #ff6b6b;
		--focus-ring: #9bc2ff;
		--mark-important: #e5c04a;
		--mark-urgent: #c49bff;
	}
	/* H and I (added later, 2026-11-14, ATL-55 follow-up): light variants; they override color-scheme. */
	.root[data-variant='h'] {
		color-scheme: light;
		--surface: #ffffff;
		--surface-raised: #f4f5f7;
		--border: #d5d9e0;
		--text: #1a1d23;
		--text-muted: #5b6472;
		--accent: #1f5fd6;
		--accent-contrast: #ffffff;
		--danger: #c62828;
		--focus-ring: #1f5fd6;
		--mark-important: #a16207;
		--mark-urgent: #7c3aed;
	}
	.root[data-variant='i'] {
		color-scheme: light;
		--surface: #e8eaee;
		--surface-raised: #f3f4f6;
		--border: #c4c9d2;
		--text: #181b21;
		--text-muted: #505a68;
		--accent: #1d4fc4;
		--accent-contrast: #ffffff;
		--danger: #b3261e;
		--focus-ring: #1d4fc4;
		--mark-important: #8a5a00;
		--mark-urgent: #6d28d9;
	}

	a {
		color: inherit;
		text-decoration: none;
	}
	:where(a, button, input):focus-visible {
		outline: 2px solid var(--focus-ring);
		outline-offset: 1px;
	}

	.topbar {
		display: flex;
		align-items: center;
		gap: 12px;
		height: 44px;
		padding: 0 16px;
		background: var(--surface-raised);
		border-bottom: 1px solid var(--border);
	}
	.nav {
		display: flex;
		gap: 4px;
	}
	.nav a {
		padding: 4px 8px;
		border-radius: 4px;
		color: var(--text-muted);
	}
	.nav a:hover,
	.nav a.active {
		color: var(--text);
	}
	.nav a.active {
		background: var(--surface);
	}
	.search {
		width: 180px;
	}
	.capture {
		flex: 1;
		min-width: 0;
	}

	.field {
		height: 28px;
		padding: 0 8px;
		background: var(--surface);
		color: var(--text);
		border: 1px solid var(--border);
		border-radius: 4px;
		font: inherit;
	}
	.field::placeholder {
		color: var(--text-muted);
	}

	.column {
		max-width: 760px;
		margin: 0 auto;
		padding: 16px;
	}
	.switcher {
		display: flex;
		flex-wrap: wrap;
		gap: 6px;
		margin-bottom: 12px;
	}
	.switcher a {
		padding: 3px 10px;
		border: 1px solid var(--border);
		border-radius: 999px;
		color: var(--text-muted);
	}
	.switcher a.current {
		background: var(--accent);
		border-color: var(--accent);
		color: var(--accent-contrast);
	}
	.note {
		margin: 0 0 12px;
		color: var(--text-muted);
		font-size: 13px;
	}

	.rows {
		margin: 0 0 24px;
		padding: 0;
		list-style: none;
		background: var(--surface-raised);
		border: 1px solid var(--border);
		border-radius: 6px;
	}
	.row {
		display: flex;
		align-items: center;
		gap: 8px;
		height: 34px;
		padding: 0 10px;
		border-bottom: 1px solid var(--border);
	}
	.row:last-child {
		border-bottom: 0;
	}
	.row:hover,
	.row:focus-within {
		background: var(--surface);
	}
	.row input[type='checkbox'] {
		accent-color: var(--accent);
		margin: 0;
	}
	.text {
		flex: 1;
		min-width: 0;
		overflow: hidden;
		white-space: nowrap;
		text-overflow: ellipsis;
	}
	.mark {
		font-size: 11px;
		line-height: 1;
	}
	.imp {
		color: var(--mark-important);
	}
	.urg {
		color: var(--mark-urgent);
	}
	.cat {
		color: var(--text-muted);
		font-size: 12px;
		white-space: nowrap;
	}
	.chip {
		padding: 0 7px;
		border: 1px solid var(--border);
		border-radius: 999px;
		color: var(--text-muted);
		font-size: 12px;
		white-space: nowrap;
	}
	.actions {
		display: none;
		gap: 8px;
	}
	.row:hover .actions,
	.row:focus-within .actions {
		display: flex;
	}
	.link {
		padding: 0;
		background: none;
		border: 0;
		color: var(--text-muted);
		font: inherit;
		font-size: 12px;
		cursor: pointer;
	}
	.link:hover {
		color: var(--text);
	}
	.due {
		min-width: 40px;
		text-align: right;
		color: var(--text-muted);
		font-size: 12px;
		font-variant-numeric: tabular-nums;
	}
	.due.overdue {
		color: var(--danger);
	}

	.samples {
		display: flex;
		flex-wrap: wrap;
		align-items: center;
		gap: 10px;
	}
	.btn {
		height: 28px;
		padding: 0 12px;
		border: 1px solid transparent;
		border-radius: 4px;
		font: inherit;
		cursor: pointer;
	}
	.primary {
		background: var(--accent);
		color: var(--accent-contrast);
	}
	.secondary {
		background: var(--surface-raised);
		border-color: var(--border);
		color: var(--text);
	}
	.btn.danger {
		background: transparent;
		border-color: var(--danger);
		color: var(--danger);
	}
	.accent-link {
		color: var(--accent);
		text-decoration: underline;
	}
</style>
