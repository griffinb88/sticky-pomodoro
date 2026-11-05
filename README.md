# Sticky Pomodoro

A small always-on-top Pomodoro timer window for Windows, opened from Claude Code. It is for you, not
Claude: nothing about it pauses or blocks Claude, so Claude keeps working while you take your break.

## Install

In Claude Code:

```
/plugin marketplace add griffinb88/sticky-pomodoro
/plugin install sticky-pomodoro@sticky-pomodoro
```

## Use

Run `/sticky-pomodoro:start`, or ask Claude in your own words. For example:

- "Start a pomodoro timer"
- "Open my focus timer"
- "Show a break timer on my screen"

The timer appears in the upper right of your screen, paused at 25:00.

- **Buttons:** start/pause, skip to the next timer, restart this timer, settings (gear), close (X).
- **Move it:** drag it anywhere. It remembers the spot.
- **Adjust it:** the gear sets work and break minutes, the chime, and whether the next timer starts
  by itself. A timer already in progress keeps its length; the new length applies next time.
- **When time is up:** it chimes, flashes and waits for you to press start, unless you turned on
  starting by itself.

## Requirements

Windows with .NET Framework 4, which Windows 10 and 11 include. No other operating system is
supported.

## How it works, and what it touches

On first use, `scripts/start.cmd` compiles `src/StickyPomodoro.cs` with the C# compiler included with
Windows and opens the result. The repository contains no compiled programs.

- **Network:** none. It downloads nothing and sends nothing.
- **Data collected:** none.
- **Files written:** only `%USERPROFILE%\.claude\sticky-pomodoro`, which holds the compiled timer and
  your settings. Delete that folder after uninstalling to remove them.

## License

MIT
