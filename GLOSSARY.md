# Glossary

Personal tool for capturing and tracking todos and recurring tasks across work and personal life.

## Item

The single kind of thing the user records. Starts as just a line of text. May optionally have an Area, Tags and, later, a Recurrence. There are no separate types such as todo or reminder.

## Capture

Recording a new Item with minimal friction: one line of text, no required fields. Available from every Client.

## Inbox

The main list of all open Items, ordered by soonest date and then newest, with an Area/Tag filter. More distinct views are designed later.

## Item states

An Item is **open**, **done** or **archived**. There is no deletion: removing an Item archives it. Open Items appear in the Inbox; done and archived Items are hidden from it.

## Views

Each Item state has its own view: Inbox (open), Done and Archive (archived). Each is a filter over the same Items. Items can be restored to open from Done or Archive. Text search works across Items.

## Backup

A way to copy all Core data to separate storage, usable when the Core runs on a remote server.

## Area

A user-defined top-level category an Item can belong to (for example Work, Personal). An Item has at most one Area.

## Important / Urgent

Two manual yes/no flags on an Item, unset by default, set by the user. Not derived from the due date. They exist from v1 for the later Eisenhower matrix.

## Tag

A free-form label on an Item. An Item can have many Tags.

## Recurrence (TBD)

A schedule that makes an Item reappear periodically. Intended behaviour: the next occurrence follows the original schedule (not the completion date), missed occurrences do not pile up, and both done and skipped advance to the next one. Schedule design is deferred to implementation.

## Core

The single service that owns all data and rules. Every Client talks to it. It runs on the user's machine at first, and may later run on a remote server. Clients cannot work without it; there is no offline mode.

## Client

An interface to the Core: the web UI, the CLI, or the desktop app. Clients hold no data or rules of their own.

## Eisenhower matrix (deferred)

A web UI view that sorts Items into four quadrants by how important and how urgent they are. Dragging an Item between quadrants changes those properties. The data model carries the fields it needs from the start; the view itself is not part of v1. There is no general-purpose board.

## Deferred

Reminders and notifications, Activities (habit-style logging with a target frequency), additional views, the Eisenhower matrix, the web UI, phone access, sync, multi-user.
