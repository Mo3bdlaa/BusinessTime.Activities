# Built packages

The activity package, built and checked in so it can be installed into Studio without building anything.

| | |
| --- | --- |
| File | `BusinessTime.Activities.1.0.1.nupkg` |
| Version | 1.0.1 |
| Built from | `9588fa8442e71691bcaeb02bae5e4247e6e0d92c` |
| SHA-256 | `5389af01b87eb7f66fca0c7321c84a2b04e3e38740a3c202067f0235d0847c84` |

## Installing it into Studio

1. Copy `BusinessTime.Activities.1.0.1.nupkg` into a folder. A network share works well for a team.
2. In Studio, open **Manage Packages → Settings** and add that folder as a user-defined package source.
3. Find **BusinessTime.Activities** under that source and install it.

To publish it to Orchestrator instead, upload the same file to a tenant feed.

## Why there are no dependencies on the cross-platform side

Studio and the Robot supply the workflow runtime themselves. A package that also depended on
`UiPath.Workflow` would install a second copy of `System.Activities` beside the host's, and the loader would
fail to bind it — so the workflow runtime is referenced at compile time only, and the `net6.0` assets declare
no dependencies at all.

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
