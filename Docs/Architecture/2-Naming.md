# 2 — Naming and API design

<!--
Purpose: How things are named in this codebase, and how to read a bad name as a design diagnosis rather than a cosmetic problem.
Status: working
Authority: MANDATORY — deviations need a reason in writing.
Update trigger: when a casing rule, a banned name shape, or the comment policy changes.
Last updated: 2026-08-09
-->

## 1. Casing

Microsoft's rules, with Unity's documented divergences noted. Checked against InfinityQube's
`03-file-standards.md` on 2026-08-09: the two agree on every point here. The one place they
diverge is class naming — see the note at the end of §2.

| Kind | Casing | Note |
|---|---|---|
| Namespace, type, method, property, event, enum value | `PascalCase` | |
| Public / protected field | `PascalCase` | |
| Parameter, local | `camelCase` | |
| Private instance field | `_camelCase` | Unity's guide also permits `m_`; pick one, and this project already uses `_` |
| Private static field | `s_camelCase` | |
| Private constant | `PascalCase` | Unity's guide says `k_MaxHealth`; **rejected** — it conflicts with Microsoft's no-nonalphanumeric rule and buys nothing in an IDE with semantic highlighting |
| Interface | `I` prefix, adjective phrase | |
| Boolean | affirmative, verb-prefixed: `IsDead`, `HasRig`, `CanBuy` | never `NotReady`, never `CantSeek` |

Sources: [Capitalization Conventions](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/capitalization-conventions),
[Identifier names](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/identifier-names),
[Unity naming and code style](https://unity.com/how-to/naming-and-code-style-tips-c-scripting-unity).

**Two rules that matter more than casing here:**

**Abbreviations: none, except mathematics and this project's own vocabulary.** Microsoft says
never abbreviate; Unity carves out math (`Lerp`, `Slerp`). This project has a third carve-out
that outranks both: `_meta/_definitions/language.md` is the ubiquitous language and its terms
are the names. `Hp`, `Dps`, `Tier`, `Ram` stay. Expanding a table term to satisfy a style
guide is a language violation, and the language file wins.

**Redundant context: strip it.** Inside `Player`, the field is `Score`, not `PlayerScore`.
Unity states this one explicitly and it is the rule most often broken by generated code.

**Two Unity-specific hazards Microsoft does not cover, and both apply here:**

1. **MonoBehaviour messages bind by name, not by override.** `Awake`, `Update`, `OnEnable`,
   `OnValidate`, `OnTriggerEnter` are matched as strings. Renaming one silently stops it
   being called — no compiler error. Declared `private`, they also break inheritance: Unity
   calls only the derived one. Any MonoBehaviour intended to be subclassed declares its
   messages `protected virtual`. This is live for `EnemyGatekeeper` → `HunterGatekeeper` /
   `RamGatekeeper`. ([source](https://kybernetik.com.au/cs-unity/docs/introduction/methods/messages))
2. **`OnX` already means three things**: a Unity message (`OnEnable`), an event *raiser*
   (`OnDoorOpened()` invoking `DoorOpened`), and a hand-wired *handler* (`OnWaveStarted`).
   This codebase uses all three. **Rule: `OnX` is reserved for Unity messages and for
   subscribed handlers. An event raiser is `RaiseX`.**

## 2. Method names as a diagnosis

A bad method name is not a cosmetic problem. It is the method admitting something the
signature will not say, and the admission is usually accurate.

**The worked example is this codebase's own.** `PlayerGun.Firing.RegisterPinHitIfNeeded` was
the name that prompted this folder. Audited 2026-08-09: it is the **only** `*IfNeeded` in
`Assets/Scripts`, and there is no `*Manager`, `*Helper` or `*Utils` class anywhere in the
tree. So the name was not evidence of a pattern — but it was evidence of a bug. `IfNeeded`
means *the method has two behaviours and the caller cannot see which it gets*, and here the
hidden branch was that it fired once per damaged body in a pierce line, resetting a build
counter that needed three hits on one target. **The name predicted the defect. That is the
argument for the table below.**

| Shape | What it is admitting | Refactor | Where it is correct |
|---|---|---|---|
| `DoXIfNeeded` | Two behaviours, guard on state the caller cannot see | Hoist the predicate to the caller, or make it idempotent and drop the suffix. .NET's name for the legitimate version is `EnsureX` — `Ensure` states a postcondition, `IfNeeded` states an implementation detail | Check-and-act must be atomic (lazy init under concurrency) |
| `HandleX` / `ProcessX` | The verb carries no information; usually several abstraction levels at once | [Extract Function](https://refactoring.com/catalog/extractFunction.html) per step, name each after its observable effect | Event handlers, where it is the convention; or where "process" is the domain's own verb |
| `XManager` | A noun that names no responsibility; residue of splitting a class without redistributing behaviour | Name the responsibility — `WaveScheduler`, `SalvageLedger` | Established platform terms with defined contracts (`SceneManager`) |
| `XHelper` | The class it helps is not doing its job — low cohesion plus feature envy | Merge into the class it helps | Extension-method containers |
| `XUtils` | A static bag discoverable only by tribal knowledge | Move each method to the type it operates on. Note `Math`, `Path` and `Mathf` are named after their **subject**, not after "utils" | Genuinely stateless pure functions with no owning type — and still name them after the subject |
| `UpdateX` | Says state changed, not to what or why; becomes a recompute-everything call. Also collides with Unity's reserved `Update` | Name the transition: `ApplyDamage`, `AdvanceWave`, `SetHealth` | CRUD layers; Unity's own messages |
| `TryX` | **Not a smell when complete.** See below | | |
| `DoX(bool flag)` | The name cannot describe the behaviour because it branches on an argument. `Save(true)` is unreadable at the call site | Two named methods, or an enum ([Remove Flag Argument](https://refactoring.com/catalog/removeFlagArgument.html)) | **When the bool is the value, not a mode.** `SetActive(bool)` is correct design |

**InfinityQube mandates `XManager` / `XUtils` / `XHelper` and this project does not.**
That is a genuine fork, not a settled question — the name-smell argument is practitioner
opinion rather than documented guidance, and .NET ships `SceneManager`. The evidence that
decided it here is InfinityQube's own: the two files its `TechnicalCritiques.md` names as
god classes are `WaveManager` (2,083 lines against a 600 limit) and `PlayerActionManager`
(1,936). A class named after managing has no boundary to stop growing at. Recorded in
`6-InfinityQube-Precedence.md`; **Zo's to overturn.**

**`TryX` is normative, not a smell — when it has all three parts.** Microsoft's Try-Parse
Pattern requires the `Try` prefix, a `bool` return, and **a paired exception-throwing member**;
failure for any reason other than the well-defined try condition must still throw
([Exceptions and Performance](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/exceptions-and-performance)).
`TryX` returning `void`, or with no throwing sibling, or where the bool means "was the input
valid", is an error code wearing the pattern's costume.

## 3. Command-Query Separation

**Queries return a result and change nothing. Commands change state and return nothing.**
The payoff is that queries can be called freely — in logs, asserts, conditionals, the
debugger — without reasoning about consequences ([Fowler](https://martinfowler.com/bliki/CommandQuerySeparation.html)).

Fowler breaks it himself for `Pop`, and .NET breaks it for `Stack.Pop`, `Queue.Dequeue`,
`Set.Add` returning `bool`, and the concurrent `TryX` family. The discriminator for a method
that mutates **and** returns `bool`:

- **The bool reports whether the mutation happened** (`Set.Add` → "was it new?"). Legitimate.
  The information exists only because the mutation was attempted, and splitting it costs a
  second lookup or opens a race.
- **The bool reports whether the operation succeeded.** That is an error code. Throw, or use
  the real Try pattern.

`PlayerAbility.TryActivate`'s internal `ActivateConcussive` / `TryManualPin` /
`ActivateRegenSurge` all return `bool` in the first sense and are fine. What is not fine is
what `TryActivate` does with the result — see §4.

**A name needing "And" is failing CQS.** `getTotalOutstandingAndSendBill` is Fowler's own
example; the refactor is [Separate Query from Modifier](https://refactoring.com/catalog/separateQueryFromModifier.html).

## 4. Least astonishment, and the rule that follows from it

**The verb in the name must cover every observable effect.** If a caller reading only the
name would be surprised by something the method does, either the name is wrong or the method
is doing too much. There is no third option.
([Principle of least astonishment](https://en.wikipedia.org/wiki/Principle_of_least_astonishment))

**`Register` records; it does not fire.** A `RegisterX` that also invokes the thing being
registered for breaks in compounding ways: callers registering during initialization get a
callback before their own construction finishes, registration order becomes semantically
significant, and `Unregister` is no longer its inverse. If immediate invocation is wanted it
belongs to the caller, or to a method that advertises it.

**Rule, new and specific to this codebase: a refusal is never silent.** Audited 2026-08-09,
`PlayerAbility.TryActivate` returns without a word on two of its three failure paths —
`if (_cooldown > 0f) return;` and `if (!fired) return;`. Only the no-ability-on-this-weapon
case logs. The consequence is that pressing `F` is indistinguishable between on cooldown,
not armed, and broken, and the log afterwards is equally silent. **Every path that declines
to do the thing the player asked for says why, in the log at minimum.** This is the same
class of defect M22 was written for.

**Unity-specific astonishment this project should honour:** Unity's own API splits `Get*`
(cheap) from `Find*` (a scene search). A method that walks the hierarchy is `Find*`, never
`Get*`, regardless of what it returns.

## 5. Ubiquitous language

`_meta/_definitions/language.md` is the authority and this file does not restate it. Two
points where it interacts with the rules above:

**It outranks the naming guidelines.** When a domain term is an abbreviation, a keyword, or
otherwise against Microsoft's rules, the domain wins for internal code. The escape hatch is
the presentation boundary: code speaks the design vocabulary, and anything the player reads
can diverge without a rename.

**The cost is a standing rename tax**, and this project has now paid it twice — M11's slots
and the 2026-08-08 Boarder/Gatekeeper reversal. Renames that reach serialized data are
migrations, not refactors: `SaveData`'s `"EquippedMelee"` JSON key changed with the Drone
rename, and compatibility shims are banned; `language.md` trap 3 records why.

## 6. Comments

`Docs/Architecture.md` already states the project rule — comments carry contracts,
invariants, units and traps, never design rationale or conclusions from play data. It was
written after `WaveDirector.cs` reached 203 comment lines against 97 of code. That rule
stands and this file adds three things.

**What a doc comment must carry to earn its place** — information the signature cannot:
exceptions and their triggers, **units and coordinate space** (degrees vs radians, world vs
local, seconds vs frames — the highest-value comment in game code), valid ranges and null
semantics, ownership and lifetime, and in Unity specifically **cost**: does this allocate,
does it walk the hierarchy, does it block.

**A comment that restates the signature is a second copy of the truth that will drift.**
`/// <summary>Gets or sets the health.</summary>` above `public int Health { get; set; }` is
pure maintenance cost and it makes real doc comments harder to find.

**A comment recording a conclusion is worse than one restating code.** `// Safe to skip the
null check here` states a verdict without the premises. When a caller is added the verdict is
silently wrong and nothing detects it. Record the reasoning, which is falsifiable — `// every
caller is in this assembly and constructs _target in Awake` — or better, encode it as a test.

**Do not enable `GenerateDocumentationFile`.** Nothing outside this project consumes its
IntelliSense, so CS1591 would only pressure someone into writing the filler above. Document
by judgement, and use `<inheritdoc/>` on overrides rather than copying text.
