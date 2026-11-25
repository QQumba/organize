<script lang="ts">
	import { resolve } from '$app/paths';
	import CoreBanner from '#lib/components/CoreBanner.svelte';
	import ItemRow from '#lib/components/ItemRow.svelte';
	import LoadingBar from '#lib/components/LoadingBar.svelte';
	import { localIsoDate } from '#lib/dueDate.ts';

	let showBanner = $state(false);
	let showBar = $state(false);

	// Component gallery: one section per component, with its variants and states.
	// Rules: web/CLAUDE.md. Read this page before making a component; a component is not done
	// until it is here (`npm run check` fails for a file in src/lib/components/ not mentioned here).
	const swatches = [
		{ name: 'surface', cls: 'bg-surface' },
		{ name: 'surface-raised', cls: 'bg-surface-raised' },
		{ name: 'border', cls: 'bg-border' },
		{ name: 'text', cls: 'bg-text' },
		{ name: 'text-muted', cls: 'bg-text-muted' },
		{ name: 'accent', cls: 'bg-accent' },
		{ name: 'accent-contrast', cls: 'bg-accent-contrast' },
		{ name: 'focus-ring', cls: 'bg-focus-ring' },
		{ name: 'danger', cls: 'bg-danger' },
		{ name: 'mark-important', cls: 'bg-mark-important' },
		{ name: 'mark-urgent', cls: 'bg-mark-urgent' }
	];
	const today = new Date();
	const dayOffset = (n: number) => localIsoDate(new Date(today.getTime() + n * 86_400_000));
	let nextId = 0;
	function sample(
		fields: Partial<{
			text: string;
			notes: string | null;
			category: string | null;
			tags: string[];
			dueDate: string | null;
			important: boolean;
			urgent: boolean;
		}>,
		extra: { overdue?: boolean; action?: boolean; error?: string } = {}
	) {
		return {
			item: {
				id: ++nextId,
				text: 'Item',
				notes: null,
				category: null,
				tags: [],
				dueDate: null,
				important: false,
				urgent: false,
				state: 'open' as const,
				createdAt: today.toISOString(),
				doneAt: null,
				archivedAt: null,
				...fields
			},
			overdue: extra.overdue ?? false,
			action: extra.action ?? false,
			error: extra.error ?? ''
		};
	}
	const rows = [
		sample({ text: 'Plain Item' }),
		sample({ text: 'Important and urgent', important: true, urgent: true }),
		sample({ text: 'Overdue, highlight on', dueDate: dayOffset(-3) }, { overdue: true }),
		sample({ text: 'Same date, highlight off (Done, Archive, Search)', dueDate: dayOffset(-3) }),
		sample({ text: 'Due today', dueDate: dayOffset(0) }),
		sample({ text: 'Due tomorrow, with Category', dueDate: dayOffset(1), category: 'Work' }),
		sample({ text: 'Due in another year', dueDate: '2031-03-09', notes: 'Has notes' }),
		sample({ text: 'Tags', tags: ['urgent-call', 'home', 'errands'], category: 'Personal' }),
		sample({
			text: 'A very long Item text that does not fit on one line and is cut off with an ellipsis, with the full text in the tooltip and on the edit page',
			category: 'Work',
			tags: ['long']
		}),
		sample({
			text: 'Підготувати звіт для п’ятничної зустрічі, їжак',
			category: 'Робота',
			tags: ['звіт', 'команда'],
			important: true,
			dueDate: dayOffset(2)
		}),
		sample({ text: 'With a done checkbox in the action slot' }, { action: true }),
		sample({ text: 'The action failed' }, { action: true, error: "Can't reach the Core" })
	];
	const buttons = [
		{ name: 'Primary', cls: 'btn btn-primary' },
		{ name: 'Secondary', cls: 'btn btn-secondary' },
		{ name: 'Danger', cls: 'btn btn-danger' }
	];
</script>

<svelte:head><title>Component gallery</title></svelte:head>

<main class="mx-auto max-w-[760px] space-y-8 p-6">
	<h1 class="text-xl font-semibold">Component gallery</h1>

	<section class="space-y-2">
		<h2 class="font-semibold">Theme colors</h2>
		<ul class="grid grid-cols-2 gap-2 sm:grid-cols-3">
			{#each swatches as s (s.name)}
				<li class="flex items-center gap-2 text-xs text-text-muted">
					<span class="size-6 rounded-control border border-border {s.cls}"></span>{s.name}
				</li>
			{/each}
		</ul>
		<p class="text-text-muted">
			Text muted. <span class="text-danger">Danger.</span>
			<span class="text-mark-important">■ Important</span>
			<span class="text-mark-urgent">▲ Urgent</span>
		</p>
	</section>

	<section class="space-y-2">
		<h2 class="font-semibold">Buttons</h2>
		<p class="text-xs text-text-muted">
			Hover and focus: use the mouse and Tab. Classes <code>btn btn-primary|secondary|danger</code>.
		</p>
		{#each buttons as b (b.name)}
			<div class="flex flex-wrap items-center gap-2">
				<span class="w-24 text-text-muted">{b.name}</span>
				<button class={b.cls}>Зберегти</button>
				<button class="{b.cls} outline-2 outline-offset-1 outline-focus-ring">Focus</button>
				<button class={b.cls} disabled>Disabled</button>
			</div>
		{/each}
	</section>

	<section class="space-y-2">
		<h2 class="font-semibold">Inputs</h2>
		<p class="text-xs text-text-muted">
			Class <code>input</code>; invalid with <code>aria-invalid="true"</code>.
		</p>
		<div class="flex flex-wrap items-center gap-2">
			<input class="input" placeholder="Нова задача, п'ятниця, їжак" />
			<input class="input" value="Купити хліб і молоко, їжу для єнота" />
			<input class="input outline-2 outline-offset-1 outline-focus-ring" value="Focus" />
			<input class="input" value="Disabled" disabled />
			<input class="input" value="Error" aria-invalid="true" />
		</div>
		<div class="flex flex-wrap items-center gap-2">
			<input class="input" type="date" value="2026-11-21" />
			<input class="input" type="date" value="2026-11-21" disabled />
			<input class="input" type="date" value="2026-11-21" aria-invalid="true" />
		</div>
	</section>

	<section class="space-y-2">
		<h2 class="font-semibold">Focus ring</h2>
		<p class="text-text-muted">
			Every focusable element shows the <code>focus-ring</code> color on keyboard focus. Tab
			through:
			<a class="text-accent underline" href={resolve('/dev/components')}>a link</a>, the buttons and
			inputs above.
		</p>
	</section>

	<section class="space-y-2">
		<h2 class="font-semibold">CoreBanner</h2>
		<p class="text-xs text-text-muted">
			Fixed at the top of every page when the Core is unreachable; Retry reloads the page's data.
			Shown here by the toggle (it covers the top of the page).
		</p>
		<button class="btn btn-secondary" onclick={() => (showBanner = !showBanner)}>
			{showBanner ? 'Hide' : 'Show'} banner
		</button>
		<CoreBanner visible={showBanner} onretry={() => void (showBanner = false)} />
	</section>

	<section class="space-y-2">
		<h2 class="font-semibold">LoadingBar</h2>
		<p class="text-xs text-text-muted">
			A thin accent bar at the top; appears only after 300 ms of navigation. Shown here by the
			toggle (forced loading, still after the delay).
		</p>
		<button class="btn btn-secondary" onclick={() => (showBar = !showBar)}>
			{showBar ? 'Stop' : 'Start'} loading
		</button>
		<LoadingBar loading={showBar} />
	</section>

	<section class="space-y-2">
		<h2 class="font-semibold">ItemRow</h2>
		<p class="text-xs text-text-muted">
			The shared Item row (Inbox, Done, Archive, Search): 54px, one-line ellipsis title that links
			to the Item, markers, due date, notes marker, Tag chips and Category. The per-view action is
			the
			<code>action</code> snippet (here a done checkbox). <code>overdueHighlight</code> is on only for
			open Items (the Inbox). Rows: plain; Important and Urgent; overdue (highlight on); the same date
			with highlight off; notes and Tags; a long title; Ukrainian text; with an action; with an action
			error. Hover and Tab to see the hover and focus states.
		</p>
		<ul class="rounded-panel border border-border bg-surface-raised">
			{#each rows as row (row.item.id)}
				<ItemRow item={row.item} overdueHighlight={row.overdue} error={row.error} {today}>
					{#snippet action()}
						{#if row.action}
							<input type="checkbox" class="m-0 accent-accent" aria-label="Mark done" />
						{/if}
					{/snippet}
				</ItemRow>
			{/each}
		</ul>
	</section>

	<section class="space-y-2">
		<h2 class="font-semibold">Ukrainian text</h2>
		<p>Щасливий їжак з Києва з'їв гарбуз; є ґанок, Їжа, Є, ї, ґ, ʼ.</p>
	</section>
</main>
