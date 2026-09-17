# Shaker.BusinessTime.Activities

[![Build](https://github.com/Mo3bdlaa/Shaker.BusinessTime.Activities/actions/workflows/build.yml/badge.svg)](https://github.com/Mo3bdlaa/Shaker.BusinessTime.Activities/actions/workflows/build.yml)
[![Licence](https://img.shields.io/badge/licence-MIT-blue)](LICENSE)
[![Version](https://img.shields.io/badge/nuget-1.0.0-blue)](packages/Shaker.BusinessTime.Activities.1.0.0.nupkg)
[![Targets](https://img.shields.io/badge/UiPath-Windows%20%C2%B7%20Cross--platform%20%C2%B7%20Legacy-blue)](#designers-and-platforms)

UiPath activities for date arithmetic that respects working hours.

`DateTime.AddHours(8)` on a Friday afternoon lands on Saturday, and a service level measured with
`DateTime.Subtract` bills a team for the weekend. This package counts only the hours the office is actually
open:

On a Monday-Friday, 09:00-17:00 week:

| Question | Plain .NET | This package |
| --- | --- | --- |
| Friday 16:30 plus 4 hours | Friday 20:30 | **Monday 12:30** |
| Friday 16:30 → Monday 10:15 | 2 days 17:45 | **1h 45m** of working time |
| Is Sunday 10:00 open? | it has no idea | **False**, and it names the holiday if there is one |
| Christmas Eve | an ordinary day | **a half day**, because you said so once |

The working week, its holidays and its exceptions live in one calendar, and every activity reads that same
definition — from Studio, or from a JSON file several processes share.

## What is in the box

- **Twelve activities** — add and subtract business time, measure the working time between two moments,
  count business days, ask whether you are open, describe a day, snap an out-of-hours moment onto the
  calendar, find the next working day, and list the working windows in a period.
- **One calendar** covering several shifts a day, night shifts across midnight, holidays that repeat every
  year, shutdown ranges, half days, time zones and daylight saving.
- **A Business Calendar editor** in Studio's ribbon, for maintaining that calendar without editing JSON.
- **Every Studio** — `net461` for Windows-legacy projects, `net6.0` for Windows and cross-platform ones,
  with the engine and the designers inside the one package.
- **266 tests**, including every figure quoted on this page.

---

## Install

1. Download
   [`Shaker.BusinessTime.Activities.1.0.0.nupkg`](packages/Shaker.BusinessTime.Activities.1.0.0.nupkg)
   and put it in a folder — a network share works well for a team.
2. In Studio, open **Manage Packages → Settings** and add that folder as a user-defined package source.
3. Find **Shaker BusinessTime Activities** under that source and install it.

That is the name Studio shows; `Shaker.BusinessTime.Activities` is the package id a feed lists it under.
The activities appear under **Business Time** in the panel. To publish to Orchestrator instead, upload the
same file to a tenant feed.

## Quick start

Three activities and you have a working answer.

**1. Say what your working week is.** Drop **Create Business Calendar** at the start of the process:

| Property | Value |
| --- | --- |
| Working week | `Mon-Fri 09:00-17:00` |
| Time zone | `UTC_plus_01_Berlin`, or `MachineLocal` to follow each robot |
| Result | `calendar` |

**2. Ask it something.** Drop **Add Business Time** and point it at that variable:

| Property | Value |
| --- | --- |
| Calendar | `calendar` |
| Date | `ticket.Created` |
| Hours | `4` |
| Result | `dueAt` |

**3. That is it.** A ticket raised Friday 16:30 is due **Monday 12:30**, not Friday 20:30.

Nothing above is required, by the way: an activity with no calendar at all assumes Monday to Friday,
09:00-17:00, in the robot's own time zone, so it does something sensible before it is configured.

---

## Contents

- [Describing working time](#describing-working-time) — the calendar, in depth
  - [The schedule string](#the-schedule-string) · [Time zones](#time-zones) · [Holidays and half days](#holidays-shutdowns-and-half-days) · [Calendar files](#calendar-files)
- [Activities](#activities) — every property of every activity
  - [At a glance](#at-a-glance)
  - [Create](#create-business-calendar) · [Load](#load-business-calendar) · [Save](#save-business-calendar)
  - [Add](#add-business-time) · [Subtract](#subtract-business-time) · [Between](#get-business-time-between) · [Count](#count-business-days)
  - [Is Business Time](#is-business-time) · [Day Info](#get-business-day-info) · [Snap](#snap-to-business-time) · [Next Day](#get-next-business-day) · [Intervals](#get-working-intervals)
- [How an activity finds its calendar](#how-an-activity-finds-its-calendar)
- [Worked examples](#worked-examples) — service levels, countdowns, retries
- [Rules the engine follows](#rules-the-engine-follows) — the cases implementations usually disagree on
- [Designers and platforms](#designers-and-platforms)
- [The calendar editor in Studio's ribbon](#the-calendar-editor-in-studios-ribbon)
- [Building it yourself](#building-and-installing)
- [Using the engine outside UiPath](#using-the-engine-outside-uipath)
- [Repository layout](#repository-layout) · [Licence](#licence)

---

## Describing working time

Everything starts with a **business calendar**: a working week, a list of exceptions to it, a time zone, and
the length of a nominal business day.

### The schedule string

The working week is written as one line, which is what the activities, the JSON files and the API all accept:

```
Mon-Fri 09:00-17:00
Mon-Thu 08:00-16:30; Fri 08:00-14:00
Mon-Fri 09:00-12:00,13:00-17:00; Sat 09:00-13:00; Sun off
Weekdays 9am-5pm
Daily 00:00-24:00
Mon-Fri 22:00-06:00
```

The rules are deliberately few:

| Part | Accepted forms |
| --- | --- |
| Entry separator | `;`, `|`, or a line break |
| Days | `Mon`, `Monday`, `Mon-Fri`, `Mon,Wed,Fri`, `Weekdays`, `Weekend`, `Daily` / `All` |
| Day / hours separator | a space, or `:` |
| Hours | `09:00-17:00`, `9-17`, `9am-5:30pm`, `00:00-24:00` |
| Several windows in a day | comma-separated: `09:00-12:00,13:00-17:00` |
| A day off | `off`, `closed`, `none`, or simply never mentioning the day |

Two conveniences worth knowing:

- **Night shifts.** A window that runs backwards crosses midnight, so `22:00-06:00` is eight hours ending the
  next morning. Say `Mon-Fri 22:00-06:00` and Saturday morning is correctly counted as Friday's shift.
- **Last entry wins.** `Mon-Fri 09:00-17:00; Wed 09:00-12:00` gives short Wednesdays, which keeps a schedule
  readable instead of having to spell out every day.

### Time zones

A calendar either belongs to a place or follows the machine. `MachineLocal` in the activities, and
**System default** in the editor, mean the hours are local time wherever the process runs: the file records
`"timeZone": "Local"` and each robot resolves its own zone on loading. Naming a city pins the hours to that
place instead, which is what a support desk in one country wants.

The **Time zone** property is a drop-down naming each zone by its offset and a city in it, so it can be read
either way and the list runs west to east: `UTC_minus_05_NewYork`, `UTC_00_London`, `UTC_plus_01_Berlin`,
`UTC_plus_05_30_Mumbai`, `UTC_plus_09_Tokyo` and around forty more, plus `MachineLocal` and `UTC`.

The offsets are standard time, so a zone that keeps summer time is an hour further ahead for part of the
year. The calculations account for that on their own; the offset in the name is only there to find the zone
you mean.

Anything not in the list is still reachable: pick `Custom` and type the identifier into **Time zone id**,
which takes either spelling — `Europe/Oslo` or `Central Asia Standard Time`.

### Holidays, shutdowns and half days

Four kinds of exception cover what businesses actually do:

| Kind | Meaning |
| --- | --- |
| Holiday | A single date nobody works |
| Annual holiday | A date that repeats every year, such as 1 January |
| Shutdown | A run of dates, such as the week between Christmas and New Year |
| Special hours | A date that *is* worked, but not on the usual hours — a half day, or an exceptional working Saturday |

Where two exceptions cover the same date, the one added last wins, so a skeleton-crew day can be carved out
of a shutdown.

### Naming them in Studio

**Create Business Calendar** takes plain dates in **Holidays**, which is enough when nobody needs to know
why a day is closed. Its **Special days** input covers the rest, and keeps the names:

```vb
New SpecialDay() {
    SpecialDay.AnnualHoliday(1, 1, "New Year's Day"),
    SpecialDay.Holiday(New DateTime(2026, 12, 25), "Christmas Day"),
    SpecialDay.CustomHours(New DateTime(2026, 12, 24), "09:00-13:00", "Christmas Eve"),
    SpecialDay.Shutdown(New DateTime(2026, 12, 27), New DateTime(2026, 12, 31), "Winter shutdown")
}
```

Those names come back out of **Is Business Time**, **Get Business Day Info** and **Get Next Business Day** as
`SpecialDayName`, and they survive **Save Business Calendar**, so a calendar built in Studio says everything
a hand-written calendar file can.

### Calendar files

A calendar can live in a JSON file that several processes share and that the business can edit without anyone
republishing a package:

```json
{
  "name": "Germany - support desk",
  "timeZone": "Europe/Berlin",
  "hoursPerBusinessDay": 8,
  "week": "Mon-Fri 09:00-12:00,13:00-17:00; Sat 09:00-13:00",
  "specialDays": [
    { "date": "01-01", "name": "New Year's Day", "annual": true },
    { "date": "2026-12-24", "name": "Christmas Eve", "hours": "09:00-13:00" },
    { "from": "2026-12-27", "to": "2026-12-31", "name": "Winter shutdown" }
  ]
}
```

`week` also accepts an object keyed by day name, when that reads better for a hand-maintained file:

```json
"week": {
  "monday": "09:00-17:00",
  "tuesday": ["09:00-12:00", "13:00-17:00"],
  "saturday": "off"
}
```

A full example is in [`samples/support-desk-calendar.json`](samples/support-desk-calendar.json).

---

## Activities

Twelve activities. The five used in almost every process sit directly under **Business Time** in the
panel; the rest are grouped so the list stays short:

| Group | Activities |
| --- | --- |
| **Business Time** | Create Business Calendar · Add Business Time · Subtract Business Time · Get Business Time Between · Is Business Time |
| **Business Time › Calendar** | Load Business Calendar · Save Business Calendar |
| **Business Time › Days** | Count Business Days · Get Next Business Day · Get Business Day Info |
| **Business Time › Windows** | Snap To Business Time · Get Working Intervals |
 Every calculating one takes the same two
calendar properties, described once here rather than repeated in each table below:

| Property | In/Out | Example | What it does |
| --- | --- | --- | --- |
| `Calendar` | In | `calendar` | The calendar to calculate with, usually from **Create Business Calendar**. |
| `Working week` | In | `"Mon-Fri 09:00-17:00"` | Used only when `Calendar` is empty. No holidays. |

Leave both empty and you get Monday-Friday 09:00-17:00 in the robot's own zone, so an activity does
something sensible the moment it is dropped.

### At a glance

| Activity | Answers | Result |
| --- | --- | --- |
| Create Business Calendar | What counts as working time here? | `BusinessCalendar` |
| Load Business Calendar | …the same, read from a file | `BusinessCalendar` |
| Save Business Calendar | Write the calendar out | *(writes a file)* |
| Add Business Time | When will this be done? | `DateTime` |
| Subtract Business Time | When did it have to start? | `DateTime` |
| Get Business Time Between | How long did we actually have? | `TimeSpan` |
| Count Business Days | How many working days is that? | `Int32` |
| Is Business Time | Are we open right now? | `Boolean` |
| Get Business Day Info | What do this day's hours look like? | `Boolean` |
| Snap To Business Time | Treat this as arriving when we open | `DateTime` |
| Get Next Business Day | When do we next open? | `DateTime` |
| Get Working Intervals | Which windows are available? | `IList(Of BusinessTimeInterval)` |

---

### Create Business Calendar

Describes the working week once. Keep the result in a variable and hand it to everything that follows.

| Property | In/Out | Example |
| --- | --- | --- |
| `Working week` | In | `"Mon-Fri 09:00-12:00,13:00-17:00; Sat 09:00-13:00"` |
| `Time zone` | In | `UTC_plus_01_Berlin`, or `MachineLocal` to follow each robot |
| `Time zone id` | In | `"Europe/Oslo"` — only when Time zone is `Custom` |
| `Holidays` | In | `New DateTime() {New DateTime(2026,12,25)}` |
| `Holidays every year` | In | `New DateTime() {New DateTime(2000,1,1)}` — month and day only |
| `Special days` | In | `New SpecialDay() {SpecialDay.CustomHours(New DateTime(2026,12,24), "09:00-13:00", "Christmas Eve")}` |
| `Hours per business day` | In | `7.5` — leave `0` to take it from the week |
| `Name` | In | `"Support desk"` |
| `Result` | **Out** | `calendar` |

### Load Business Calendar

The same calendar, read from a file several processes can share.

| Property | In/Out | Example |
| --- | --- | --- |
| `File path` | In | `"Data\BusinessCalendar.json"` |
| `Json` | In | JSON text, for an Orchestrator asset instead of a file |
| `Time zone override` | In | `UTC_plus_09_Tokyo` — leave `MachineLocal` to keep what the file says |
| `Result` | **Out** | `calendar` |

### Save Business Calendar

| Property | In/Out | Example |
| --- | --- | --- |
| `Calendar` | In *(required)* | `calendar` |
| `File path` | In *(required)* | `"Data\BusinessCalendar.json"` — missing folders are created |

### Add Business Time

Moves a moment forward by working time. `Days`, `Hours`, `Minutes` and `Duration` are added together.

| Property | In/Out | Example |
| --- | --- | --- |
| `Date` | In *(required)* | `ticket.Created` |
| `Days` | In | `3` |
| `Hours` | In | `4` |
| `Minutes` | In | `30` |
| `Duration` | In | `TimeSpan.FromMinutes(15)` |
| `Day handling` | In | `AsWorkingHours` (a day is the calendar's hours) or `AsWholeDays` (a whole day, clock time kept) |
| `Result` | **Out** | `dueAt` |
| `Elapsed time` | Out | `20:00:00` — real time crossed, closed hours included |

> Friday 16:30 + 4 business hours on a 09:00-17:00 week → **Monday 12:30**.

### Subtract Business Time

The exact inverse: when did this have to start? Same properties as **Add Business Time**.

> Due Wednesday 11:00, needs 6 business hours → started **Tuesday 13:00**.

### Get Business Time Between

The working time two moments are apart — the service-level measure.

| Property | In/Out | Example |
| --- | --- | --- |
| `From` | In *(required)* | `ticket.Created` |
| `To` | In *(required)* | `ticket.Answered` |
| `Result` | **Out** | `05:45:00` |
| `Business days` | Out | `0.766` — using the calendar's hours per business day |
| `Business hours` | Out | `5.75` |
| `Working days` | Out | `3` — working days the period touches |

> A weekend in the middle costs nothing, so a Friday ticket is not penalised for it.

### Count Business Days

| Property | In/Out | Example |
| --- | --- | --- |
| `From` | In *(required)* | `DateTime.Today` |
| `To` | In *(required)* | `DateTime.Today.AddMonths(1)` |
| `Result` | **Out** | `22` — both end dates counted |

### Is Business Time

| Property | In/Out | Example |
| --- | --- | --- |
| `Date` | In *(required)* | `DateTime.Now` |
| `Result` | **Out** | `True` when that exact moment is inside working hours |
| `Is working day` | Out | `False` on a holiday, whatever the time |
| `Special day name` | Out | `"Christmas Day"`, or empty on an ordinary day |

### Get Business Day Info

| Property | In/Out | Example |
| --- | --- | --- |
| `Date` | In *(required)* | `New DateTime(2026,12,24)` |
| `Result` | **Out** | `True` when the date is worked at all |
| `Day start` | Out | `09:00` |
| `Day end` | Out | `13:00` |
| `Working time` | Out | `04:00:00` |
| `Shifts` | Out | `"09:00-13:00"`, or `"off"` |
| `Special day name` | Out | `"Christmas Eve"` |

### Snap To Business Time

Moves an out-of-hours moment onto the calendar, and leaves a working moment alone.

| Property | In/Out | Example |
| --- | --- | --- |
| `Date` | In *(required)* | `request.Received` |
| `Direction` | In | `Forward` to the next opening, `Backward` to when work last stopped |
| `Result` | **Out** | Sunday 10:00 → **Monday 09:00** |
| `Was adjusted` | Out | `True` when it had to move — worth logging |

### Get Next Business Day

| Property | In/Out | Example |
| --- | --- | --- |
| `Date` | In *(required)* | `DateTime.Today` — never itself the answer |
| `Direction` | In | `Next` or `Previous` |
| `Result` | **Out** | The moment work starts that day, never midnight |
| `Day end` | Out | `17:00` |
| `Working time` | Out | `08:00:00` |
| `Special day name` | Out | `"Christmas Eve"` on a half day |

### Get Working Intervals

Every open window in a period — what a scheduler needs to place work.

| Property | In/Out | Example |
| --- | --- | --- |
| `From` | In *(required)* | `DateTime.Now` |
| `To` | In *(required)* | `DateTime.Now.AddDays(7)` |
| `Result` | **Out** | `IList(Of BusinessTimeInterval)`, each with `Start`, `End`, `Duration` |
| `Total working time` | Out | `05:45:00` — agrees with **Get Business Time Between** over the same period |

---

## How an activity finds its calendar

Every calculating activity has a **Calendar** property. Build the calendar once at the start of the process,
keep it in a variable, and hand that variable to each activity.

If you leave **Calendar** empty, the activity falls back to its own **Working week** property — a schedule
string such as `Mon-Fri 09:00-17:00`, enough for a quick calculation that needs no holidays. Leave that empty
too and you get Monday to Friday, 09:00-17:00, in the robot's own time zone, so an activity dropped on the
canvas does something sensible before it is configured at all.

## Worked examples

### A service level that ignores the weekend

A ticket arrives Friday at 16:30 and must be answered within four business hours, on a Monday-Friday
09:00-17:00 calendar.

1. **Load Business Calendar** → `FilePath: "Data\calendar.json"` → `calendar`
2. **Add Business Time** → `Date: ticket.Created`, `Hours: 4` → `dueAt`

The office closes at 17:00, so half an hour is spent on Friday and the remaining three and a half on Monday
morning: `dueAt` is Monday 12:30, not Friday 20:30.

On the [sample calendar](samples/support-desk-calendar.json), which opens on Saturday mornings, the same
ticket is due Saturday at 12:30 instead — which is the point of keeping the working week in one shared file
rather than in the workflow.

### Was the answer late?

**Get Business Time Between** → `From: ticket.Created`, `To: ticket.Answered`

The `Result` is the working time the team actually had. A weekend in the middle costs nothing, and
`BusinessHours` gives the same figure as a number for a report.

### Start a countdown only once the office is open

A request arriving on Sunday should be treated as arriving on Monday morning.

**Snap To Business Time** → `Date: request.Received`, `Direction: Forward`

`WasAdjusted` tells you whether it landed out of hours, which is often worth logging.

### Schedule a retry for the next working day

**Get Next Business Day** → `Date: DateTime.Now` → the moment work next starts.

The answer is when the office actually opens, not midnight, and it follows that day's own hours — so a day
with exceptional hours opens when those hours say it does. `Direction: Previous` looks the other way.

### When did this have to start?

A task needs six business hours and is due Wednesday at 11:00.

**Subtract Business Time** → `Date: dueAt`, `Hours: 6` → the latest possible start.

### One calendar for a whole process

**Create Business Calendar** once at the start, keep the result in a variable, and hand that variable to the
**Calendar** property of everything that follows. Any single activity that needs different hours just gets a
different calendar.

---

## Rules the engine follows

Worth knowing, because these are the cases where implementations usually disagree:

- **Windows are half-open.** The instant a shift ends is not working time, so 17:00 on a 09:00-17:00 day is
  outside hours. Consequently 09:00 + 8 hours reports 17:00 — the moment work stopped — rather than jumping
  to the next morning. One minute more rolls over to 09:01 the next day.
- **Starting out of hours is fine.** The clock simply starts running at the next working moment, so a
  calculation from Sunday behaves the same as one from Monday 09:00.
- **Day navigation lands on the start of business.** **Get Next Business Day** returns the moment work
  begins on that day, never midnight, so it can be used directly as a start time.
- **Adding zero changes nothing**, even out of hours. Use **Snap To Business Time** when you want a moment
  moved onto the calendar.
- **Subtraction is the exact inverse of addition.** Add a duration and subtract it again and you are back
  where you started.
- **Durations are wall clock.** A working day keeps its nominal length across a daylight-saving change: the
  clocks move, the shift does not.
- **Time zones.** A calendar carries the zone its hours are written in. A `DateTime` marked UTC is converted
  into that zone, calculated there, and handed back as UTC; one with no kind is taken to be office-local
  already, which is the usual case in a workflow.
- **Calendars are immutable and thread-safe**, so one calendar can be shared across parallel branches.
- **A calendar with no working time fails loudly** rather than looping forever, and says which calendar it was.

---

## Designers and platforms

The runtime and the designer are separate things, and they run in different places.

| | Targets | Runs on |
| --- | --- | --- |
| `BusinessTime.Core` | `netstandard2.0`, `net461`, `net6.0` | anywhere .NET runs |
| `BusinessTime.Activities` | `net461`, `net6.0` | Windows-legacy, Windows and cross-platform robots alike |
| `BusinessTime.Activities.Design` | `net461` | Windows-legacy Studio, at design time |
| `BusinessTime.Activities.Wizard` | `net6.0-windows` | Modern Studio, at design time |

**The activities are cross-platform.** The `net6.0` assets run on a Linux robot as they do on Windows, and
the runtime assembly deliberately references nothing from WPF — there is a test that fails the build if it
ever starts to. Time zones are resolved by identifier, so `Europe/Berlin` works on Linux and
`W. Europe Standard Time` works on Windows, whichever the calendar was written with.

It is design time only and Windows only, and it is built twice: the .NET Framework assets carry the build a
Windows-legacy Studio loads, the `net6.0` assets the build a modern one loads. The two generations split the
designer across different assemblies — .NET Framework keeps it all in `System.Activities.Presentation`, .NET
moves the metadata half into `System.Activities.Metadata` — so one build cannot serve both, and for a long
while only the legacy one existed here, which is why a modern Studio showed the stock designers.

Neither designer assembly is published by UiPath, so the .NET build compiles against the stubs in
[`stubs/`](stubs/README.md) and Studio supplies the real ones. The stubs never ship.

It is written in code rather than XAML so that the whole solution still builds on any operating system — the
WPF markup compiler only runs on Windows. What it provides:

- **The main inputs and outputs on the face of each activity**, so the common cases can be filled in without
  opening the properties panel. Everything else stays in the panel as usual.
- **An icon for each activity** — a clock or a calendar page, marked in the corner with what the activity
  does to it, so the pack reads as one family and each one is still told apart at a glance.
- **`Result` under Output**, with a description of its own. It arrives from the base class with no category,
  which otherwise leaves it under Misc, away from the outputs it belongs with.

An activity the designer has no layout for falls back to the plain card, and a card that cannot be drawn at
all leaves every property reachable from the panel, so nothing is ever stranded.

Designers are attached through `IRegisterMetadata`, which Studio calls once when it loads the package. If
registration were ever to fail it is swallowed and the stock designers apply, so a designer problem can
never stop the activities themselves from loading.

## The calendar editor in Studio's ribbon

The package registers a **Business Calendar** wizard, so a calendar file can be built and maintained from
Studio rather than by hand: the working week a day at a time, the time zone, and a grid of holidays, half
days and shutdowns, saved as JSON wherever you point it.

It opens on `Data\BusinessCalendar.json` in the project when there is one, so the usual case needs no
browsing at all. When there is not, it offers that path for a new file and leaves everything else alone:
**Open…** picks up a calendar kept somewhere else, **Save as…** writes one somewhere new, and **Save** asks
before replacing a file the editor did not open, so a calendar somebody else maintains cannot be lost by
pressing Save out of habit.

Its **Time zone** list offers **System default — follows each robot** first. That choice is not the machine
the editor happens to be running on: the file records the instruction rather than a zone, and whichever
robot loads it supplies its own. A calendar written that way means 09:00-17:00 in Berlin on a Berlin robot
and 09:00-17:00 in Tokyo on a Tokyo robot. Pick a city instead when the hours belong to one place — a
support desk in Berlin keeps Berlin hours whichever robot is asking.

Dates are chosen from a date picker rather than typed, so no spelling of a date can reach the file; ticking
**Every year** keeps only the day and month. Time zones are listed by offset as well as name —
`(UTC+01:00) W. Europe Standard Time` — with the machine's own zone offered first.

It is design time only and Windows only, and it ships with the `net6.0` assets because modern Studio runs on
.NET 8 and never loads .NET Framework ones. Every date and shift it accepts is read by the same engine the
activities use, so the editor has no parsing rules of its own to disagree with them.

## Building and installing

The workflow runtime comes from UiPath's official feed, which `NuGet.config` already points at. It is a
compile-time reference only: the activities ask the host for `System.Activities 6.0.0.0`, the version Studio
ships, and a build against anything higher fails to load in a real project.


A built package is checked in at
[`packages/Shaker.BusinessTime.Activities.1.0.0.nupkg`](packages/Shaker.BusinessTime.Activities.1.0.0.nupkg), so Studio can
install it without building anything first — see [`packages/README.md`](packages/README.md) for the steps.
Every push also builds it on CI and attaches it to the run.

To build it yourself:

```bash
dotnet build BusinessTime.Activities.sln -c Release
dotnet test  BusinessTime.Activities.sln -c Release
dotnet pack  src/BusinessTime.Activities/BusinessTime.Activities.csproj -c Release -o artifacts
```

`artifacts/Shaker.BusinessTime.Activities.1.0.0.nupkg` is the activity package. It targets `net461` for Windows-legacy
projects and `net6.0` for Windows and cross-platform ones, and both the engine and the designers travel
inside it, so this one file is all Studio needs.

Build the solution before packing: the designers are picked up from their build output, and packing without
them raises a warning and produces a package that works but looks unbranded on the canvas.

To install it:

1. Copy the `.nupkg` into a folder — a network share works well for a team.
2. In Studio, **Manage Packages → Settings**, add that folder as a user-defined package source.
3. Find **Shaker BusinessTime Activities** under that source and install it.

To publish it to Orchestrator instead, upload the same file to a tenant feed.

---

## Using the engine outside UiPath

`BusinessTime.Core` has no dependency on the workflow runtime, so the same rules can be used from any .NET
code — a test harness, an API, a console tool:

```csharp
BusinessCalendar calendar = BusinessCalendar.Create()
    .WithName("Support desk")
    .WithSchedule("Mon-Fri 09:00-12:00,13:00-17:00; Sat 09:00-13:00")
    .WithTimeZone("Europe/Berlin")
    .AddAnnualHoliday(1, 1, "New Year's Day")
    .AddShutdown(new DateTime(2026, 12, 27), new DateTime(2026, 12, 31), "Winter shutdown")
    .Build();

DateTime dueAt   = calendar.Add(ticketCreated, TimeSpan.FromHours(4));
TimeSpan handled = calendar.GetBusinessTimeBetween(ticketCreated, ticketAnswered);
bool openNow     = calendar.IsWorkingTime(DateTime.Now);
```

The main entry points are `Add`, `Subtract`, `AddBusinessDays`, `AddBusinessHours`, `AddWorkingDays`,
`GetBusinessTimeBetween`, `GetBusinessDaysBetween`, `CountWorkingDays`, `SnapForward`, `SnapBackward`,
`GetWorkingIntervals`, `IsWorkingTime`, `IsWorkingDay` and `GetShifts`, plus `BusinessCalendarSerializer`
for the JSON format.

---

## Repository layout

```
packages/                      the built activity package, ready to install
src/BusinessTime.Core          the calendar model and the calculation engine
src/BusinessTime.Activities    the UiPath activities
tests/BusinessTime.Core.Tests  engine tests
src/BusinessTime.Activities.Design
                               Studio designers, design time and Windows only
tests/BusinessTime.Activities.Tests
                               activity tests, run through the real workflow runtime
samples/                       an example calendar file
```

---

## Licence

[MIT](LICENSE) — Copyright (c) 2026 Mohammed Shaker. Use it, change it, ship it; keep the notice.
