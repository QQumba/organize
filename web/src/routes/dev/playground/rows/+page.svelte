<!--
	Question: how should an Inbox item row look? Starting point: the row from the `look` prototype,
	in the chosen charcoal-blue theme (variant G there).
	Date: 2026-11-14
	Ticket: ATL-55 follow-up
	Reference only: no API calls, no state. Not imported from or into anything else.
-->
<script lang="ts">
	import { resolve } from '$app/paths';

	const self = resolve('/dev/playground/rows');
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

<svelte:head><title>Rows prototype</title></svelte:head>

<div class="root">
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

	.rows {
		margin: 0;
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
</style>
