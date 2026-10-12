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
    with:
      resources-ref: "2026.923.0"   # match G0V0ResourcesVersion in osu.Game.csproj
```

The reusable workflow checks out the calling repository as the osu! side, the resources repository
(public, pinned by `resources-ref`), and the checker itself. It emits inline annotations, appends a
summary table to the job summary, and fails the job when the threshold is exceeded. Inputs:
`osu-repository`, `osu-ref`, `resources-repository`, `resources-ref`, `extra-resx`, `baseline`,
`fail-on`.

To drive it by hand from an existing `CheckSanity.sh` style step instead, use the annotation format
directly:

```yaml
- name: Check localisation
  run: |
    dotnet run --project checker/src/g0v0.LocalisationChecker -c Release -- \
      --osu osu --resources resources --format github --color never --fail-on error
```

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
  Output/     human / GitHub annotations / JSON reporters
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
