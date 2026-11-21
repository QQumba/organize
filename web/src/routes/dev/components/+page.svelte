<script lang="ts">
	import { resolve } from '$app/paths';

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
		<h2 class="font-semibold">Ukrainian text</h2>
		<p>Щасливий їжак з Києва з'їв гарбуз; є ґанок, Їжа, Є, ї, ґ, ʼ.</p>
	</section>
</main>
