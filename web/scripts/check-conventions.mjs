// Two small checks run by `npm run check` (rules: web/CLAUDE.md).
// 1. Every file in src/lib/components/ is mentioned in the component gallery page.
// 2. No raw color values in components or app screens (use the theme tokens from src/app.css).
import { existsSync, readdirSync, readFileSync, statSync } from 'node:fs';
import { join, basename, extname } from 'node:path';

const gallery = 'src/routes/dev/components/+page.svelte';
const componentsDir = 'src/lib/components';
const errors = [];

function walk(dir, skip = () => false) {
	if (!existsSync(dir)) return [];
	return readdirSync(dir).flatMap((name) => {
		const path = join(dir, name).replaceAll('\\', '/');
		if (skip(path)) return [];
		return statSync(path).isDirectory() ? walk(path, skip) : [path];
	});
}

const galleryText = existsSync(gallery) ? readFileSync(gallery, 'utf8') : '';
if (!galleryText) errors.push(`${gallery} is missing`);
for (const file of walk(componentsDir)) {
	const name = basename(file, extname(file));
	if (!galleryText.includes(name)) {
		errors.push(`${file}: component "${name}" is not in the gallery (${gallery})`);
	}
}

const palette =
	'slate|gray|zinc|neutral|stone|red|orange|amber|yellow|lime|green|emerald|teal|cyan|sky|blue|indigo|violet|purple|fuchsia|pink|rose';
const raw = [
	/(^|[^\w&])#[0-9a-fA-F]{3,8}\b/, // hex color
	/\b(rgb|rgba|hsl|hsla|hwb|lab|lch|oklab|oklch|color)\(/, // color functions
	new RegExp(
		String.raw`\b(bg|text|border|ring|outline|fill|stroke|from|via|to|divide|accent|caret|shadow|decoration)-(${palette})-\d`
	),
	/\b(bg|text|border|ring|outline|fill|stroke|divide|accent)-(white|black)\b/,
	/\b(bg|text|border|ring|outline|fill|stroke|divide|accent)-\[(#|rgb|hsl|oklch|color)/
];
const scanned = [
	...walk(componentsDir),
	...walk('src/routes', (p) => p.startsWith('src/routes/dev/'))
].filter((f) => /\.(svelte|ts|css)$/.test(f));
for (const file of scanned) {
	readFileSync(file, 'utf8')
		.split('\n')
		.forEach((line, i) => {
			if (raw.some((re) => re.test(line))) {
				errors.push(`${file}:${i + 1}: raw color value, use a theme token: ${line.trim()}`);
			}
		});
}

if (errors.length) {
	console.error(errors.join('\n'));
	process.exit(1);
}
console.log('conventions: ok');
