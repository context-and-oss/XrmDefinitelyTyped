/**
 * Type-level compatibility check against XrmQueryTyped.Core output.
 *
 * `typings/account.d.ts` and `typings/contact.d.ts` are the generator's golden files, copied
 * verbatim from `test/XrmQueryTyped.Core.Tests/Fixtures/`. Nothing here runs — the project is
 * `noEmit`; the point is that the generated declarations compile against the package's ambient
 * contract and that inference flows through the builder.
 *
 * Like `tests/xdt-compat`, this needs its own tsconfig because it declares `accounts` on the global
 * merge interfaces, which would collide with `tests/fixtures/entities.d.ts`.
 */

import { Filter, SortOrder, XrmQuery } from "@delegateas/xrmquery";
import { account_accountcategorycode } from "./typings/OptionSets";

/** Compile-time assertion that `Actual` is exactly `Expected`. */
type Equals<A, B> =
  (<T>() => T extends A ? 1 : 2) extends <T>() => T extends B ? 1 : 2 ? true : false;
declare function expectType<Expected>(): <Actual>(
  ...args: Equals<Actual, Expected> extends true ? [Actual] : ["TYPE MISMATCH", Actual]
) => void;

const guid = "00000000-0000-0000-0000-000000000000";

/* -------------------------------------------------------------------------- */
/* select, option sets, money, formatted values                               */
/* -------------------------------------------------------------------------- */

async function selecting() {
  const account = await XrmQuery.retrieve((x) => x.accounts, guid)
    .select((x) => [x.name, x.accountcategorycode, x.createdon, x.merged])
    .selectMore((x) => [x.revenue, x.primarycontactid_guid])
    .includeFormattedValues()
    .execute();

  expectType<string>()(account.accountid);
  expectType<string | null>()(account.name);
  expectType<account_accountcategorycode | null>()(account.accountcategorycode);
  expectType<Date | null>()(account.createdon);
  expectType<boolean | null>()(account.merged);
  expectType<number | null>()(account.revenue);
  expectType<string | null>()(account.primarycontactid_guid);
  // A money attribute carries its currency lookup along.
  expectType<string | null>()(account.transactioncurrencyid_guid);

  expectType<string | undefined>()(account.revenue_formatted);
  expectType<string | undefined>()(account.accountcategorycode_formatted);
  expectType<string | undefined>()(account.transactioncurrencyid_formatted);
}

/* -------------------------------------------------------------------------- */
/* filter, ordering, paging                                                   */
/* -------------------------------------------------------------------------- */

async function filtering() {
  const accounts = await XrmQuery.retrieveMultiple((x) => x.accounts)
    .select((x) => [x.name, x.revenue])
    .filter((x) =>
      Filter.ands([
        Filter.startsWith(x.name, "Contoso"),
        Filter.greaterThan(x.revenue, 1000),
        Filter.notEquals(x.accountcategorycode, account_accountcategorycode.PreferredCustomer),
        Filter.equals(x.parentaccountid_guid, Filter.makeGuid(guid)),
        Filter.equals(x.merged, false),
      ]),
    )
    .orderAsc((x) => x.name)
    .orderDesc((x) => x.createdon)
    .maxPageSize(20)
    .execute();

  expectType<string | null>()(accounts[0]!.name);

  const first = await XrmQuery.retrieveMultiple((x) => x.accounts)
    .select((x) => [x.name])
    .getFirst();
  expectType<string | null | undefined>()(first?.name);
}

/* -------------------------------------------------------------------------- */
/* expand — single-valued and collection-valued                               */
/* -------------------------------------------------------------------------- */

async function expanding() {
  const account = await XrmQuery.retrieve((x) => x.accounts, guid)
    .select((x) => [x.name])
    .expand(
      (x) => x.primarycontactid,
      (x) => [x.fullname],
    )
    .expand(
      (x) => x.contact_customer_accounts,
      (x) => [x.fullname, x.creditlimit],
      { top: 10, orderBy: (s) => s.fullname, sortOrder: SortOrder.Descending },
    )
    .expand(
      (x) => x.parentaccountid,
      (x) => [x.name],
    )
    .execute();

  expectType<string | null>()(account.name);
  // Expanded children are typed from `_Result`, whose members are non-optional `T | null` — the
  // legacy generator made them optional, so `| undefined` is gone here.
  expectType<string | null>()(account.primarycontactid.fullname);
  expectType<string | null>()(account.contact_customer_accounts[0]!.fullname);
  expectType<string | null>()(account.parentaccountid.name);
  // `ownerid` is deliberately not expanded here: a polymorphic lookup is typed as an intersection
  // of one mapping per target entity, and TypeScript cannot infer the child select from that. Same
  // limitation as the legacy generator — see README.md.
}

/* -------------------------------------------------------------------------- */
/* related records                                                            */
/* -------------------------------------------------------------------------- */

async function related() {
  const contact = await XrmQuery.retrieveRelated(
    (x) => x.accounts,
    guid,
    (x) => x.primarycontactid,
  )
    .select((x) => [x.fullname])
    .execute();
  expectType<string | null>()(contact.fullname);

  const contacts = await XrmQuery.retrieveRelatedMultiple(
    (x) => x.accounts,
    guid,
    (x) => x.contact_customer_accounts,
  )
    .select((x) => [x.creditlimit])
    .execute();
  expectType<number | null>()(contacts[0]!.creditlimit);
}

/* -------------------------------------------------------------------------- */
/* create / update, including `_bind$` lookups                                */
/* -------------------------------------------------------------------------- */

async function mutating() {
  const id = await XrmQuery.create((x) => x.accounts, {
    name: "Contoso A/S",
    revenue: 1234,
    accountcategorycode: 1,
    primarycontactid_bind$contacts: guid,
    ownerid_bind$systemusers: guid,
    parentaccountid_bind$accounts: guid,
  }).execute();
  expectType<string>()(id);

  await XrmQuery.update((x) => x.accounts, guid, { name: "Contoso" }).execute();
  await XrmQuery.deleteRecord((x) => x.accounts, guid).execute();
}

export { selecting, filtering, expanding, related, mutating };
