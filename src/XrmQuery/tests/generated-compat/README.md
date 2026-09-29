# XrmQueryTyped compatibility project

`typings/account.d.ts` and `typings/contact.d.ts` are `XrmQueryTyped.Core` output, copied verbatim
from `test/XrmQueryTyped.Core.Tests/Fixtures/`. `typings/enums.d.ts` and `typings/OptionSets.ts` are what the option-set generator
emits for `account_accountcategorycode`: an ambient numeric union for generated declarations and an
importable `as const` value object for named values at runtime. `typings/stubs.d.ts` stands in for the two entities that
`account.d.ts` reaches through its polymorphic `ownerid` lookup — a real run generates those the same
way.

`WebEntity` and the `WebEntities*` merge interfaces are deliberately absent from the typings: they
come from this package's `declare global` block, which is what this project verifies.

`consumer.ts` never runs; it type-checks. Run it with:

```bash
npm run typecheck:generated
```

It lives in its own `tsconfig.json` because it declares `accounts` on the global merge interfaces,
which collides with `tests/fixtures/entities.d.ts` in the root project — hence the root
`"exclude": ["tests/generated-compat"]`.

## Known differences from the legacy generator

- **Polymorphic lookups cannot be expanded.** `ownerid` targets both `Team` and `SystemUser`, so it
  is typed as an intersection of one mapping per target. TypeScript cannot infer a child select from
  an intersection of generic mappings, so `.expand((x) => x.ownerid, …)` does not compile. The legacy
  generator has the same limitation.
- **Expanded children are non-optional.** `_Result` members are `T | null` rather than
  `T?: T | null`, so accessing a member of an expanded child no longer includes `undefined`.
- **No `_Base`, `_Relationships` or `"@odata.etag"`.** Base members are inlined into `_Result`; the
  runtime never referenced the other two.
