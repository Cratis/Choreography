---
title: Grain compatibility tool
description: Run the GrainCompatibility tool to compare two OrleansContracts.txt files, read its exit codes and break kinds, and wire it into a CI workflow.
---

`GrainCompatibility` answers one question: can silos of the previous release and silos of this build share a cluster? It compares the `OrleansContracts.txt` of a baseline (normally the latest release) with the one of the build under test, and fails on anything that is not purely additive. The rules behind it are in [Upgrading with two releases in one cluster](index.md).

The tool is a build tool, not a package. It lives in `Source/Tools/GrainCompatibility` of the Choreography repository and targets `net10.0`, so build it from a checkout.

## Run it

```bash
dotnet run --project Source/Tools/GrainCompatibility -c Release -- \
  --baseline path/to/previous/OrleansContracts.txt \
  --current Source/Choreography/OrleansContracts.txt \
  --baseline-name v2.0.0
```

| Argument | Meaning |
| --- | --- |
| `--baseline <path>` | The manifest of the previous release. Required. |
| `--current <path>` | The manifest being checked. Required. |
| `--baseline-name <name>` | What the report calls the baseline, typically its tag. Defaults to `baseline`. |
| `--github` | Also print one GitHub workflow error annotation per break. |
| `--self-test` | Check that every kind of break is still detected, then exit. Needs no other argument. |

Run it once per contract project. The two files in this repository are `Source/Choreography/OrleansContracts.txt` and `Source/Choreography.Chronicle/OrleansContracts.txt`.

## Read the result

When nothing broke, the report counts what it compared and says so:

```text
Baseline (v1.0.0): 17 grain interfaces, 56 methods, 8 grain classes.
Current: 17 grain interfaces, 56 methods, 8 grain classes.

Every grain contract of v1.0.0 is still served, so silos of both can run in one cluster.
```

When something broke, each change is listed with its kind and the interface it affects. This output comes from a copy of the manifest with a method removed, another replaced by an overload, and a version lowered:

```text
4 change(s) mean silos of v1.0.0 and of this build cannot run in one cluster:
  MethodRemoved in Cratis.Choreography.Jobs.IJob: '1553DC89: Remove() -> ...' is gone. Changing a parameter type changes the wire identity, so keep the method and add a new one instead.
  MethodsAddedWithoutVersionIncrease in Cratis.Choreography.Jobs.IJob: Added '7777AAAA: Remove(bool) -> ...' but kept [Version(1)]. Raise it to [Version(2)].
  MethodRemoved in Cratis.Choreography.Jobs.IJobsManager: 'AC625506: Delete(Cratis.Choreography.Jobs.JobId) -> Task' is gone. ...
  VersionDecreased in Cratis.Choreography.Jobs.IJobsManager: [Version(1)] became [Version(0)].
```

The signatures are shortened here. Note the first two lines: giving `Remove()` a parameter is reported as a removal plus an unversioned addition, because the new parameter list is a different wire identity.

### Exit codes

| Code | Meaning |
| --- | --- |
| `0` | The current contracts serve the baseline, or `--self-test` found every planted break. |
| `1` | At least one break, or `--self-test` missed one. |
| `2` | The tool could not tell: bad or missing arguments, an unreadable file, a line that is not a valid declaration, or a manifest with no active grain interfaces. |

A `2` is never a pass. A manifest with no active interfaces is refused because comparing nothing would pass everything.

### Break kinds

| Kind | Raised when |
| --- | --- |
| `InterfaceRemoved` | A baseline interface is absent from the current manifest, or retired. |
| `MethodRemoved` | A baseline method identity is gone. This includes changing a parameter type or a method name. |
| `MethodSignatureChanged` | A method kept its identity but its parameter or return types changed. |
| `MethodsAddedWithoutVersionIncrease` | An interface gained methods and kept its `[Version]`. |
| `VersionDecreased` | An interface's `[Version]` went down. |
| `GrainClassRemoved` | A baseline grain class is gone, retired, or its grain type changed. |

Adding a new interface, adding a method with a higher `[Version]`, and a different CLR name under an unchanged wire identity are not breaks.

## Regenerate a manifest

The manifest is produced by the Orleans contract analyzer, never edited by hand. After changing a grain interface, run this from the repository root, once per contract project, and review the diff:

```bash
dotnet format Source/Choreography/Choreography.csproj analyzers --severity info --diagnostics ORLEANS0016 ORLEANS0017 ORLEANS0018 ORLEANS0019 ORLEANS0020 ORLEANS0022 ORLEANS0023 ORLEANS0024
```

Use the `.csproj` of the project, not a solution path. The command edits the manifest only and does not touch source attributes. A normal `dotnet build` then fails if the source and the manifest still disagree.

## Wire it into CI

`.github/workflows/grain-compatibility.yml` is a reusable workflow that the build and publish workflows call. It runs these steps:

1. Pick the baseline: the newest `v*` tag, excluding the version being released. It skips the check, with a notice, when the pull request is labeled `major`, when the release being cut starts a new major, or when no tag exists yet.
2. Build the tool and run `--self-test`, so a comparison that stopped detecting breaks cannot pass changes silently.
3. For each contract project, take its `OrleansContracts.txt` from the baseline tag with `git show`. A tag that predates the manifest has it generated from the tagged source with the same analyzer.
4. Refuse an empty baseline, run the tool with `--github`, and keep the worst exit code across projects.

To check your own product's grains the same way, keep the manifest in each release and compare against the last tag:

```yaml
- name: Compare grain contracts with the last release
  run: |
    TAG="$(git tag --list 'v*' --sort=-v:refname | sed -n 1p)"
    git show "$TAG:Source/MyProduct/OrleansContracts.txt" > baseline.txt
    dotnet run --project path/to/GrainCompatibility -c Release -- \
      --baseline baseline.txt \
      --current Source/MyProduct/OrleansContracts.txt \
      --baseline-name "$TAG" \
      --github
```

The checkout needs `fetch-depth: 0` so tags exist. This excerpt skips the self-test and the handling of a missing baseline that the shipped workflow has; keep both in a real gate.

:::caution[Passing against the last release is transitive]
Because nothing is ever removed and versions only rise, a build that passes against the latest release also stays upgradable from every earlier release since the gate began. Skipping the gate for a release breaks that chain.
:::
