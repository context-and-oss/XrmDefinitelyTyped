# XrmDefinitelyTyped [![NuGet version](https://badge.fury.io/nu/XrmDefinitelyTyped.svg)](https://badge.fury.io/nu/XrmDefinitelyTyped) [![npm version](https://badge.fury.io/js/@delegateas%2Fxrmquery.svg)](https://www.npmjs.com/package/@delegateas/xrmquery) [![Join the chat at https://gitter.im/delegateas/XrmDefinitelyTyped](https://badges.gitter.im/delegateas/XrmDefinitelyTyped.svg)](https://gitter.im/delegateas/XrmDefinitelyTyped?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

XrmDefinitelyTyped generates [TypeScript](http://www.typescriptlang.org/)
declaration files based on *your* Dynamics 365/CRM/xRM solution.

It is the TypeScript equivalent of [CrmSvcUtil](https://msdn.microsoft.com/en-us/library/gg327844.aspx), but instead of generating early-bound .NET classes for server-side code, it generates TypeScript interfaces for all your client-side coding.

[Read more here](https://github.com/delegateas/XrmDefinitelyTyped/wiki)

## Repository layout

This repository ships two artifacts: the generator and the runtime library that consumes what it
generates.

| Path | Artifact | Registry |
| --- | --- | --- |
| `src/XrmDefinitelyTyped.Tool` | `XrmDefinitelyTyped` — dotnet tool, command `xdt` | NuGet |
| `src/XrmTyped.Shared` | Shared by both generators: Dataverse entity metadata, option sets, TypeScript emission plumbing, output writing | — |
| `src/XrmDefinitelyTyped.Core` | Form generation | — |
| `src/XrmQueryTyped.Core` | Web entity type generation for `@delegateas/xrmquery` | — |
| [`src/XrmQuery`](src/XrmQuery) | `@delegateas/xrmquery` — type-safe Dataverse Web API query library | npm |

XrmQuery used to be emitted onto disk by the tool itself via `-jsLib`. It is now a normal npm
package with ES module exports — see [`src/XrmQuery/MIGRATION.md`](src/XrmQuery/MIGRATION.md) for
moving off the old web-resource setup.

## Releasing

Both artifacts are released by pushing a tag; the version is taken from the tag name.

```bash
git tag tool-v3.0.0     && git push origin tool-v3.0.0        # -> NuGet
git tag xrmquery-v0.1.0 && git push origin xrmquery-v0.1.0    # -> npm
```

## Installation

The generated declarations build on the community-maintained standard Xrm declarations and the
modern XrmQuery package instead of shipping a private copy of the full client API surface:

```bash
npm install --save-dev @types/xrm
npm install @delegateas/xrmquery
```

When form generation is enabled, XDT emits `_internal/XrmDefinitelyTyped.d.ts`. This is a small,
generator-owned augmentation layer over `@types/xrm` that carries the additional precision XDT can
provide: lookup target unions, typed form collections, concrete quick-view forms, and typed option
set controls. It does not redeclare the complete `Xrm` namespace.

## Configuration

## Usage

The tool runs two generators. Pick them with `--generate` (`-g`); the default is both.

```bash
xdt -o typings                      # forms and web entity types
xdt -o typings --generate forms     # form types only
xdt -o typings --generate web       # web entity types for @delegateas/xrmquery only
```

Web entity types land in `typings/Web/<entity>.d.ts` (or a single `typings/Web/WebEntities.d.ts` with
`--single-file`) inside the `XDT` namespace, overridable with `--web-namespace`. Option-set numeric
union declarations are written to `typings/_internal/Enum/` by whichever generator needs them.

Named option-set values are emitted as modern `as const` runtime objects in `typings/OptionSets.ts`:

```typescript
import { ctx_subscription_statuscode } from "./typings/OptionSets";

const status: ctx_subscription_statuscode = ctx_subscription_statuscode.Active;
```

Importing the object provides discoverable named values while its same-named type remains the exact
union of the Dataverse numeric values. Unlike an ambient `const enum`, this works with
`isolatedModules` and module-based transpilers. Because it is a runtime value, the generated module
must be imported by application code that uses its members.

## Configuration Options

## Features
