# Built packages

The activity package, built and checked in so it can be installed into Studio without building anything.

| | |
| --- | --- |
| File | `BusinessTime.Activities.1.0.0.nupkg` |
| Version | 1.0.0 |
| Built from | `d9e8c890cfe9b6e6902ce34d6884c6319ec06ad4` |
| SHA-256 | `22344638772b7d8d3d6a880d8504a9f938af2501c0ea916545420346e924f451` |

## Installing it into Studio

1. Copy `BusinessTime.Activities.1.0.0.nupkg` into a folder. A network share works well for a team.
2. In Studio, open **Manage Packages → Settings** and add that folder as a user-defined package source.
3. Find **BusinessTime.Activities** under that source and install it.

To publish it to Orchestrator instead, upload the same file to a tenant feed.

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
