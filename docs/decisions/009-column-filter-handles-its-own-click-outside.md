# 009. A column filter handles its own click-outside

Status: Accepted
Date: 2026-11-16

## Context
The column filter popover is controlled by the filter (`opened`), and it applies what was entered when the user dismisses it. It was wired to the popover's `onClose`, with a flag that ignored the one `onClose` caused by the filter's own close. Under Mantine 9 a controlled popover reports `onClose` only after `opened` has already gone false, and reports a click outside or Escape through `onDismiss` instead, so the filter could not be dismissed by an outside click or Escape, and what was entered was never applied that way. Moving to `onDismiss` exposed a second problem: a dropdown opened from inside the popover (the calendar of a date filter, the list of operators, the picker of a lookup column) is drawn in a portal outside the popover's element, so the popover's own click-outside counts a click in it as outside and closes the popover while the user is choosing.

## Options considered
1. **`onDismiss` and the popover's own click-outside**: the smallest change, but a choice in any nested dropdown closes the popover.
2. **Put every nested dropdown inside the popover** (no portal): works for the controls the grid draws, not for a lookup picker an app supplies.
3. **`onDismiss` for Escape, and the filter's own click-outside that ignores a click inside another dropdown** (chosen).

## Decision
The popover is told `closeOnClickOutside={false}`; Escape arrives through `onDismiss`, and an outside click is detected by the filter itself with Mantine's click-outside hook over the popover's target and dropdown. A click whose path runs through another Mantine dropdown (a popover dropdown, a dates dropdown or a listbox) is not an outside click. The flag that ignored the next `onClose` is gone, because the filter no longer uses `onClose`.

## Why
It keeps what the popover was meant to do (close and apply on a click outside) for every control that can appear inside it, including ones an app supplies, without the grid knowing about them.

## Consequences
- The check is by kind of element, not by owner: a click inside an unrelated open dropdown elsewhere on the page also leaves the filter open.
- The filter relies on `onDismiss` and the click-outside hook's list of nodes, which Mantine 9 provides.
- `closeOnEscape` keeps working through `onDismiss`.
