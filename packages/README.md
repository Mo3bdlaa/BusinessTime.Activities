# Built packages

The activity package, built and checked in so it can be installed into Studio without building anything.

| | |
| --- | --- |
| Name in Studio | Shaker BusinessTime Activities |
| File | `Shaker.BusinessTime.Activities.1.0.0.nupkg` |
| Version | 1.0.0 |
| Built from | `3d7ddd7cf61246068aea19e7f740b94c6505181f` |
| SHA-256 | `aee76f1fa56f1a2badd383cc9a080cf783f7bf3320ffebfd268b94bd79284ad7` |

## Installing it into Studio

1. Copy `Shaker.BusinessTime.Activities.1.0.0.nupkg` into a folder. A network share works well for a team.
2. In Studio, open **Manage Packages → Settings** and add that folder as a user-defined package source.
3. Find **Shaker BusinessTime Activities** under that source and install it — that is the name Studio
   shows for the package id `Shaker.BusinessTime.Activities`.

To publish it to Orchestrator instead, upload the same file to a tenant feed.

## Why there are no dependencies on the cross-platform side

Studio and the Robot supply the workflow runtime themselves, so it is referenced at compile time only and
the `net6.0` assets declare no dependencies at all.

The version they are compiled against matters just as much. The activities ask the host for
`System.Activities 6.0.0.0`, which is what Studio ships; the runtime resolves an assembly forward but never
backward, so building against a higher version makes every activity fail to load with *Could not load file
or assembly 'System.Activities'*. That is why the workflow runtime is taken from UiPath's official feed
rather than from nuget.org, whose `UiPath.Workflow` builds carry `6.0.3.0` and are not what any Studio has.

## What is inside

```
lib/net461/    BusinessTime.Activities.dll         the activities
               BusinessTime.Activities.Design.dll  the Studio designers, design time only
               BusinessTime.Core.dll               the calendar engine
lib/net6.0/    BusinessTime.Activities.dll         the activities
               BusinessTime.Activities.Design.dll  the Studio designers, design time only
               BusinessTime.Activities.Wizard.dll  the Business Calendar editor in the ribbon
               BusinessTime.Core.dll               the calendar engine
               calendar.png                        the ribbon icon, named by path
```

`net461` covers Windows-legacy projects, `net6.0` covers Windows and cross-platform ones. Each set carries
the designers for the Studio that reads it — a .NET Framework Studio the `net461` ones, a modern Studio the
`net6.0` ones. Both are design time only; a robot loads neither.

## Rebuilding it

```bash
dotnet build BusinessTime.Activities.sln -c Release
dotnet pack  src/BusinessTime.Activities/BusinessTime.Activities.csproj -c Release -o packages
```

Build the solution first: the designers are collected from their build output, and packing without them
warns and produces a package that works but looks unbranded on the canvas.

Every push also builds this package on CI and attaches it to the run, which is the copy to prefer if this
one is ever behind the source.
