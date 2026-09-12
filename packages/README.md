# Built packages

The activity package, built and checked in so it can be installed into Studio without building anything.

| | |
| --- | --- |
| File | `BusinessTime.Activities.1.4.0.nupkg` |
| Version | 1.4.0 |
| Built from | `b98fdfc3ce030a2a95762afdcef4d5d510fce493` |
| SHA-256 | `1428a159239334a8a4265b6c0074bb4e037a439a08f445381c299bcedace17b4` |

## Installing it into Studio

1. Copy `BusinessTime.Activities.1.4.0.nupkg` into a folder. A network share works well for a team.
2. In Studio, open **Manage Packages → Settings** and add that folder as a user-defined package source.
3. Find **BusinessTime.Activities** under that source and install it.

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
               BusinessTime.Core.dll               the calendar engine
```

`net461` covers Windows-legacy projects, `net6.0` covers Windows and cross-platform ones. The designers are
Windows-only and design-time only, so they ship with the `net461` assets and a robot never loads them.

## Rebuilding it

```bash
dotnet build BusinessTime.Activities.sln -c Release
dotnet pack  src/BusinessTime.Activities/BusinessTime.Activities.csproj -c Release -o packages
```

Build the solution first: the designers are collected from their build output, and packing without them
warns and produces a package that works but looks unbranded on the canvas.

Every push also builds this package on CI and attaches it to the run, which is the copy to prefer if this
one is ever behind the source.
