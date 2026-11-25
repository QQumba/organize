/** The search page URL for the header search box text, or null when the text is blank. */
export function searchUrl(text: string): string | null {
	const q = text.trim();
	return q ? `/search?q=${encodeURIComponent(q)}` : null;
}
