# BusinessTime.Activities

Custom UiPath activities for arithmetic that respects working hours.

Adding "8 hours" to a timestamp with `DateTime.AddHours` gives you Saturday. This package gives you Monday
afternoon, because it knows when the office is open:

> On a Monday-Friday 09:00-17:00 calendar, **Friday 14:00 + 8 business hours = Monday 14:00** — three hours
> are spent on Friday afternoon and the remaining five on Monday morning.

The working week, its holidays and its exceptions are described once, in one place, and every activity reads
that same definition.

---

## Contents

- [Describing working time](#describing-working-time)
  - [The schedule string](#the-schedule-string)
  - [Holidays, shutdowns and half days](#holidays-shutdowns-and-half-days)
  - [Calendar files](#calendar-files)
- [Activities](#activities)
- [How an activity finds its calendar](#how-an-activity-finds-its-calendar)
- [Worked examples](#worked-examples)
- [Rules the engine follows](#rules-the-engine-follows)
- [Building and installing](#building-and-installing)
- [Using the engine outside UiPath](#using-the-engine-outside-uipath)

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

The **Time zone** property is a drop-down, named by a city so it can be recognised at a glance: `London`,
`Berlin`, `NewYork`, `Mumbai`, `Tokyo`, `Sydney` and around forty more, plus `MachineLocal` to follow the
robot's own clock and `UTC`.

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

All of them appear in the Studio panel under **Business Time**.

### Building calendars

| Activity | What it does |
| --- | --- |
| **Create Business Calendar** | Builds a calendar from a schedule string, a time zone and lists of holidays. |
| **Load Business Calendar** | Reads a calendar from a JSON file or from JSON text (an Orchestrator asset, say). |
| **Save Business Calendar** | Writes a calendar back out to JSON. |

### Calculating

| Activity | Result | Also reports |
| --- | --- | --- |
| **Add Business Time** | `DateTime` | `ElapsedTime` — the wall clock time that passed, closed hours included |
| **Subtract Business Time** | `DateTime` | `ElapsedTime` |
| **Get Business Time Between** | `TimeSpan` | `BusinessDays`, `BusinessHours`, `WorkingDays` |
| **Count Business Days** | `Int32` | |
| **Is Business Time** | `Boolean` | `IsWorkingDay`, `SpecialDayName` |
| **Get Business Day Info** | `Boolean` (is it worked) | `DayStart`, `DayEnd`, `WorkingTime`, `Shifts`, `SpecialDayName` |
| **Snap To Business Time** | `DateTime` | `WasAdjusted` |
| **Get Next Business Day** | `DateTime` | `DayEnd`, `WorkingTime`, `SpecialDayName` |
| **Get Working Intervals** | `IList<BusinessTimeInterval>` | `TotalWorkingTime` |

**Add Business Time** and **Subtract Business Time** take `Days`, `Hours`, `Minutes` and `Duration` together
and add them up, so "one day and a half" needs no arithmetic beforehand. The `Day handling` property decides
what a *day* means:

- `AsWorkingHours` (the default) — a day is the calendar's hours per business day, and fractions are allowed.
  Friday 14:00 + 1 day is Monday 14:00.
- `AsWholeDays` — a day is a whole day on the calendar. The clock time is carried over untouched and only
  non-working days are skipped, which is what a deadline of "three business days" usually means.

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
| `BusinessTime.Activities.Design` | `net461` | Studio only, at design time |

**The activities are cross-platform.** The `net6.0` assets run on a Linux robot as they do on Windows, and
the runtime assembly deliberately references nothing from WPF — there is a test that fails the build if it
ever starts to. Time zones are resolved by identifier, so `Europe/Berlin` works on Linux and
`W. Europe Standard Time` works on Windows, whichever the calendar was written with.

**The designer is Windows-only, and cannot be otherwise.** Studio is a WPF application, so anything that
draws on its canvas is Windows-only however the runtime is targeted. The design assembly ships alongside the
`net461` assets, is never loaded by a robot, and is absent from the cross-platform assets, where Studio falls
back to its stock designers.

It is written in code rather than XAML so that the whole solution still builds on any operating system — the
WPF markup compiler only runs on Windows. What it provides:

- **The main inputs and outputs on the face of each activity**, so the common cases can be filled in without
  opening the properties panel. Everything else stays in the panel as usual.
- **An icon** on each activity, so the pack reads as one set on the canvas.

An activity the designer has no layout for falls back to the plain card, and a card that cannot be drawn at
all leaves every property reachable from the panel, so nothing is ever stranded.

Designers are attached through `IRegisterMetadata`, which Studio calls once when it loads the package. If
registration were ever to fail it is swallowed and the stock designers apply, so a designer problem can
never stop the activities themselves from loading.

## Building and installing

The workflow runtime comes from UiPath's official feed, which `NuGet.config` already points at. It is a
compile-time reference only: the activities ask the host for `System.Activities 6.0.0.0`, the version Studio
ships, and a build against anything higher fails to load in a real project.


A built package is checked in at
[`packages/BusinessTime.Activities.1.1.0.nupkg`](packages/BusinessTime.Activities.1.1.0.nupkg), so Studio can
install it without building anything first — see [`packages/README.md`](packages/README.md) for the steps.
Every push also builds it on CI and attaches it to the run.

To build it yourself:

```bash
dotnet build BusinessTime.Activities.sln -c Release
dotnet test  BusinessTime.Activities.sln -c Release
dotnet pack  src/BusinessTime.Activities/BusinessTime.Activities.csproj -c Release -o artifacts
```

`artifacts/BusinessTime.Activities.1.1.0.nupkg` is the activity package. It targets `net461` for Windows-legacy
projects and `net6.0` for Windows and cross-platform ones, and both the engine and the designers travel
inside it, so this one file is all Studio needs.

Build the solution before packing: the designers are picked up from their build output, and packing without
them raises a warning and produces a package that works but looks unbranded on the canvas.

To install it:

1. Copy the `.nupkg` into a folder — a network share works well for a team.
2. In Studio, **Manage Packages → Settings**, add that folder as a user-defined package source.
3. Find **BusinessTime.Activities** under that source and install it.

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
