---
name: start
description: Opens a small always-on-top Pomodoro work/break timer window on the user's Windows desktop. Use when the user asks to start, open or show a Pomodoro timer, focus timer or break timer for themselves.
---

Open the timer window by running this script once with the shell tool. It returns right away and the
window stays open on its own:

```
"${CLAUDE_PLUGIN_ROOT}/bin/sticky-pomodoro.cmd"
```

Then tell the user the timer is in the upper right of their screen, paused until they press start,
and that the gear icon sets the work and break minutes.

This timer is for the user's own breaks. Do not pause, slow down or stop your own work because of it.

The timer only runs on Windows. On any other system, don't run the script; tell the user this plugin
supports Windows only. If the script prints an error, show the user that message.
