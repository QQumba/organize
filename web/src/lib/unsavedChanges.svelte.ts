// Shared flag: a form with unsaved changes sets it, so "Refresh on focus" does not reload the
// page under it. A form clears it when it is saved, reset or destroyed.
export const unsavedChanges = $state({ dirty: false });

export function setUnsaved(dirty: boolean) {
	unsavedChanges.dirty = dirty;
}
