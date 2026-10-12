# g0v0.LocalisationChecker

A read-only cross-checker for the osu! localisation pipeline: it compares the `*Strings.cs`
classes in an osu! checkout against the `.resx` files in a matching osu-resources checkout, in both
directions, and reports the drift.

It is deliberately **not** a generator. `dotnet localisation to-resx` already writes resx files from
the classes; this tool only tells you when the two sides disagree.

## Why it exists

The two halves of a localised string live in different repositories:

| Half | Location | Example |
| --- | --- | --- |
| Key + English fallback | `osu.Game/Localisation/<X>Strings.cs` | `getKey(@"back")`, `@"Back"` |
| Translations | `osu.Game.Resources/Localisation/<X>.resx` | `<data name="back">` |

Nothing in either repository's CI checks that they agree. A missing key or a mistyped prefix fails
**silently at runtime**: `ResourceManagerLocalisationStore` catches `MissingManifestResourceException`
and returns null, so the class quietly serves English fallbacks forever while looking perfectly
healthy.

Real examples this tool finds in the current checkouts:

* `HUD/SpectatorListStrings.cs` declares `osu.Game.Resources.Localisation.SpectatorList`, but the
  resource is `...Localisation.HUD.SpectatorList` — the class is permanently untranslatable.
* `SkinComponents/SkinnableModDisplayStrings.cs` has the same defect one directory up.
* `Taiko/TaikoEditorStrings.cs` points at `...Localisation.Osu.OsuEditor`, so its three tool names are
  read from the osu! ruleset editor's resource file.
* `RulesetSettings.resx` defines 56 keys while the class declares 28 — the rest are dead weight.

## Requirements

* .NET SDK **10.0** or later (the repositories this targets build against `net10.0`).

## Quick start

```bash
git clone https://github.com/GooGuTeam/g0v0-l10n-checker
cd g0v0-l10n-checker

# against the default local layout (D:/Projects/osu and D:/Projects/osu-resources)
dotnet run --project src/g0v0.LocalisationChecker -- --osu /path/to/osu --resources /path/to/osu-resources
```

```text
osu.Game/Localisation/HUD/SpectatorListStrings.cs:10: error L10N003: prefix '...SpectatorList' does not match any resource; expected '...HUD.SpectatorList'
...

Checked 101 localisation classes and 167 resource files.
  error:   26
  warning: 59
  info:    0
  suppressed by baseline: 85
```

Exit codes: `0` clean, `1` diagnostics at or above `--fail-on`, `2` bad command line, `3` input
problem (missing directory, unreadable resx, malformed baseline).

## Rules

| ID | Default | What it catches |
| --- | --- | --- |
| `L10N001` | error | A member's key is not defined in its resource file. |
| `L10N002` | warning | A resource key that no member declares (reverse direction). |
| `L10N003` | error | The `prefix` constant does not name the right resource. |
| `L10N004` | error | Two members of one class share a key. |
| `L10N005` | error | A member's XML doc text and its fallback text disagree. |
| `L10N006` | warning | A base resource has no paired `.zh.resx`. |
| `L10N007` | warning | A resource key is not lowercase snake_case (dotted groups allowed). |
| `L10N008` | warning | A class whose prefix names no resource at all. |
| `L10N009` | warning | A resource file that no class claims. |
| `L10N010` | warning | Two scanned files derive the same resource name. |

`L10N008` is a warning rather than an error because the runtime explicitly tolerates a
"code only" namespace. It is still worth knowing about: every string in such a class is permanently
untranslatable.

The `L10N` prefix is the standard shorthand for localisation (the same family as `i18n`), and these
IDs are a stable contract: they appear in baselines, in CI annotations and in `--severity` /
`--ignore-rule` arguments, so they are never renumbered.

Change a rule's severity with `--severity L10N008=error`, or silence one with `--ignore-rule L10N008`.

## How the matching works

`prefix` is the only authoritative source of a class's resource namespace, so an exact match against
the derived resource name is always tried first. Resource names are computed from the file path
(`osu.Game.Resources.Localisation.HUD.SpectatorList`), which is exactly what the SDK embeds — so the
tool needs no build step and no `osu.Game.Resources.dll`.

A heuristic only runs when the exact match fails, because the repository contains real examples of
both traps:

* `HUD/ColourHitError.resx` belongs to `ColourHitErrorMeterStrings.cs` — the **file names disagree
  but the prefix is right**, so matching on names would be wrong.
* `HUD/SpectatorListStrings.cs` — the **prefix is simply wrong**, so matching on names is the only
  way to work out what it probably meant.

The heuristic collects candidates by name and directory, and disambiguates by how much of the
class's key set each candidate explains. It never guesses when two candidates score equally.

## Command line

Run `--help` for the full list. The parts worth calling out:

```text
-o, --osu <dir>              osu! repository root          (default: $G0V0_OSU_PATH, else D:/Projects/osu)
-r, --resources <dir>        osu-resources repository root (default: $G0V0_RESOURCES_PATH, else D:/Projects/osu-resources)
-x, --extra-resx <DIR[=NS]>  additional resx folder, repeatable
-c, --color <mode>           auto | always | never        (default: auto)
-f, --format <mode>          auto | human | github | json (default: auto)
    --json-report <file>     also write JSON, whatever --format is
    --sarif-report <file>    also write SARIF 2.1.0, for GitHub code scanning
    --sarif-source <side>    osu | resources              (default: osu)
    --min-severity <level>   info | warning | error       (default: info)
    --fail-on <level>        never | error | warning      (default: error)
    --severity <R=LEVEL>     override a rule's severity, repeatable
    --ignore-rule <RULE>     disable a rule, repeatable
    --baseline <file>        read a baseline (default: ./.localisation-baseline.json when present)
    --no-baseline            ignore baselines entirely
    --write-baseline <file>  write the current diagnostics and exit
    --version                print the version from the assembly metadata
```

`--color auto` respects `FORCE_COLOR`, then `NO_COLOR`, then disables itself under GitHub Actions,
and only then checks whether stdout is a terminal.

`--format auto` picks `github` when `GITHUB_ACTIONS=true` and `human` otherwise. The `human` format
uses `path:line: severity RULE: message`, which is the same shape the repository's existing
`CheckSanity` scripts emit.

Pin `--format` in any CI step whose output is asserted on: `auto` changes the output shape the moment
the same command runs under GitHub Actions, and the `github` format also renders findings as
annotations on the check run.

### Versioning

There is no version constant in the source. The single source is `VersionPrefix` in
`Directory.Build.props`; the SDK turns it into the assembly attributes and the tool reads them back
at run time, so `--version` cannot disagree with the binary that printed it:

```text
$ g0v0.LocalisationChecker --version
g0v0.LocalisationChecker 0.1.0
```

Override it for a single build without editing anything:

```bash
dotnet build -p:Version=1.2.3        # -> g0v0.LocalisationChecker 1.2.3
dotnet build -p:VersionSuffix=rc.1   # -> g0v0.LocalisationChecker 0.1.0-rc.1
```

When the build runs inside a git checkout the SDK appends the revision to the informational version,
and the tool splits it back out — `--version` then prints `g0v0.LocalisationChecker 0.1.0+9f3c1ab`.
The same values are echoed into the JSON report under `tool`, so an archived report can be traced back
to the build that produced it:

```json
"tool": { "name": "g0v0.LocalisationChecker", "version": "0.1.0", "revision": "9f3c1ab" }
```

The product name comes from `<Product>` in the same file, which is also what `--help` and every error
message are prefixed with. `AssemblyVersion` and `AssemblyFileVersion` are derived from the same
property, but the tool reports the *informational* version on purpose: the assembly version is a
four-part number (`0.1.0.0`) that carries less information.

### Extra resx folders

`--extra-resx` points at any additional resx directory, optionally with the root namespace it is
compiled under:

```bash
--extra-resx "osu.Game.Resources.Custom/Localisation=osu.Game.Resources.Custom.Localisation"
```

Extra folders take part in the forward check (are the keys defined?) but never in the reverse check,
so a third-party or experimental folder cannot produce orphan noise.

## CI integration

Add a pre-build check to the osu! repository with four lines:

```yaml
jobs:
  localisation:
    uses: GooGuTeam/g0v0-l10n-checker/.github/workflows/localisation-check.yml@main
    permissions:
      contents: read
      security-events: write   # only for the code scanning upload
    with:
      resources-ref: "v2026.1012.0-g0v0"   # the tag that published G0V0ResourcesVersion
```

The reusable workflow checks out the calling repository as the osu! side, the resources repository
(public, pinned by `resources-ref`), and the checker itself. It emits inline annotations, appends a
summary table to the job summary, publishes the findings to code scanning, and fails the job when the
threshold is exceeded. Inputs: `osu-repository`, `osu-ref`, `resources-repository`, `resources-ref`,
`extra-resx`, `baseline`, `fail-on`, `sarif-source`.

`resources-ref` takes a **git ref, not a version number**. The resources repository tags its releases
`v<version>-g0v0` and its release workflow strips the `v` and the suffix before publishing, so the tag
for `G0V0ResourcesVersion` = `2026.1012.0` is `v2026.1012.0-g0v0` — feeding the version in as-is fails
the checkout, not the check. Pinning is what makes a run reproducible and makes the checker look at
the same resources the app will look up at run time, but it also means the two move together: bumping
`G0V0ResourcesVersion` without bumping this ref reports every key the new version added as missing.
Leaving it empty checks against the resources repository's default branch instead — self-updating,
but no longer the code the build consumes.

### The resources side

`GooGuTeam/g0v0-resources` calls the same workflow, but it is the mirror image: it *is* the resources
repository, so it has to name the osu! side rather than being assumed to be it.

```yaml
jobs:
  localisation:
    uses: GooGuTeam/g0v0-l10n-checker/.github/workflows/localisation-check.yml@main
    permissions:
      contents: read
      security-events: write
    with:
      osu-repository: GooGuTeam/g0v0
      osu-ref: v2
      resources-ref: ${{ github.sha }}   # the commit under test, not the default branch
      sarif-source: resources
```

Three inputs carry the weight here:

* **`osu-repository` is mandatory.** The first checkout reads an empty one as "the caller is the osu!
  side", which for this caller would scan `osu.Game.Resources/Localisation/*.resx` as if they were
  `*Strings.cs` and produce nothing but noise. The workflow now refuses to start in that case instead
  of publishing it — the test is a case-insensitive comparison against `resources-repository`, so a
  differently-cased spelling of the same repository is caught as well.
* **`resources-ref` has to be explicit**, for the same reason the osu! side pins its ref: the default
  branch is not the code under test. `${{ github.sha }}` is the merge commit on a pull request and the
  pushed commit on a push.
* **`sarif-source: resources`** publishes the `.resx` findings — 58 of the 85 at the time of writing —
  instead of the `*Strings.cs` ones. Only the report changes; the cross-check itself always needs both
  sides.

So the two calls run the full pipeline each, and each repository gets the annotations, the summary
table and the alerts whose paths it can actually open. There is no "upload once, both sides see it":
the upload target is always the calling repository.

The `permissions` block must grant `security-events: write` *on the calling job*, not just here: a
called workflow can only be granted what its caller grants, and the token is otherwise read-only for
that scope. Missing it costs the upload (a 403, swallowed) and nothing else — publishing to code
scanning is best effort and never fails the check. Pull requests from forks are skipped for the same
reason, since their token cannot be widened at all.

To drive it by hand from an existing `CheckSanity.sh` style step instead, use the annotation format
directly:

```yaml
- name: Check localisation
  run: |
    dotnet run --project checker/src/g0v0.LocalisationChecker -c Release -- \
      --osu osu --resources resources --format github --color never --fail-on error
```

### Code scanning

`--sarif-report` writes a SARIF 2.1.0 file, the only format GitHub accepts for findings produced by
something other than CodeQL. The reusable workflow already uploads it, so the findings become alerts
under **Security → Code scanning**: they keep a state across runs (open, dismissed, fixed), they can
be assigned and filtered, and a ruleset can require them before a merge.

Three of the choices behind it are worth knowing, because none of them is what a first implementation
would do:

* **One report describes one repository.** A SARIF location is a path relative to the root of the
  repository being scanned, so the two sides of the cross-check can never share a file.
  `--sarif-source osu` (the default) publishes the `*Strings.cs` findings to the repository that
  holds them, and `--sarif-source resources` does the same for the `.resx` side from that
  repository's own workflow. Findings from an `--extra-resx` folder belong to neither, so they are
  never reported.
* **The baseline does not apply.** Code scanning tracks alert state itself, so uploading the known
  issues is the point rather than a leak: the Security tab is where they get triaged, while the
  baseline stays what it always was — the CI merge gate. The two lists are allowed to disagree.
* **The alert identity is ours.** GitHub reads only `partialFingerprints.primaryLocationLineHash`,
  and when it is absent the `upload-sarif` action falls back to hashing the file at the reported
  path — which, for this tool, is a file in a *different* checkout that it cannot open. The
  fingerprint is therefore computed from the same `rule|source|file|key` tuple the baseline keys on:
  an alert survives its line moving, and is still new when its key is.

The three reporters coexist on purpose. `--format github` annotates the lines a reviewer is already
looking at, the job summary carries the totals, and SARIF is the list that outlives the run.

## Baselines

Both repositories already carry known issues, so the tool ships a baseline
(`.localisation-baseline.json`) and suppresses anything listed in it. CI then only fails on problems you
introduce. Regenerate it deliberately, after reviewing what it swallows:

```bash
dotnet run --project src/g0v0.LocalisationChecker -- --write-baseline .localisation-baseline.json
```

Only warnings and errors are baselined, so informational notices keep showing up.

## Repository layout

```
src/g0v0.LocalisationChecker/
  Cli/        argument parsing and help text
  Model/      parsed classes, resources and diagnostics
  Parsing/    C# lexer, *Strings.cs parser, resx parser, resource-name derivation
  Workspace/  scanning the two checkouts
  Checking/   matcher, baselines and one file per rule
  Output/     human / GitHub annotations / JSON / SARIF reporters
tests/g0v0.LocalisationChecker.Tests/
  fixtures/   a miniature osu + osu-resources pair used by CI's smoke test
```

## Development

```bash
dotnet build g0v0.LocalisationChecker.sln -c Release
dotnet test  g0v0.LocalisationChecker.sln -c Release
```

The suite includes `EndToEndTests`, which runs the whole pipeline against the real checkouts in
`D:/Projects/osu` and `D:/Projects/osu-resources` and is skipped automatically when they are absent.
It asserts the invariants that must hold (no duplicate keys, every doc matching its fallback, a
correct prefix never flagged even when file names disagree) and acts as a canary for the known
defects listed at the top of this file.

### Conventions

* `net10.0`, `LangVersion 14.0`, nullable enabled, warnings as errors.
* LF line endings and UTF-8 without a BOM for every file, enforced by `.editorconfig` and
  `.gitattributes` so a Windows checkout with `core.autocrlf=true` cannot reintroduce CRLF.
* The .NET project, assembly, root namespace and solution are `g0v0.LocalisationChecker` — the
  organisation prefix stays lowercase, as in `g0v0.osu.Game.Resources`, and nothing is abbreviated.
  The **repository** keeps the hyphenated slug `g0v0-l10n-checker`, which is why the clone URL and
  the `uses:` path above do not read like the project name. That mismatch is deliberate.
* The version and the product name live only in `Directory.Build.props` (`VersionPrefix` and
  `Product`). Nothing in the source restates either: the tool reads its own assembly metadata at run
  time, and `--help` plus every error message are interpolated from the same place.
* Type names match file names, matching the rule the osu! repository enforces in `CheckSanity`.

## Licence

MIT. See `LICENCE`.
