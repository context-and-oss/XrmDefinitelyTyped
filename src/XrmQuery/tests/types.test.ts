/**
 * Type-level parity checks. These assertions are enforced by `npm run typecheck` as much as by the
 * test run — the point is that XDT-shaped ambient typings (see `fixtures/entities.d.ts`) infer the
 * same result types they did against the legacy `dg.xrmquery.web.d.ts`.
 *
 * Everything here is deliberately type-only: queries are built but never executed, so no request is
 * ever made.
 */

import { describe, expectTypeOf, it } from "vitest";
import { Filter, XrmQuery } from "../src/index.js";
import type { Query, RetrieveMultipleRecords } from "../src/index.js";

/** The result a query resolves with. */
type ResultOfQuery<Q> = Q extends { execute(): Promise<infer R> } ? R : never;

describe("inference", () => {
  it("intersects the selected attributes into the result", () => {
    const query = XrmQuery.retrieve((x) => x.accounts, "ID").select((x) => [
      x.name,
      x.accountnumber,
      x.revenue,
    ]);

    expectTypeOf<ResultOfQuery<typeof query>>().toExtend<{
      accountid: string;
      name: string | null;
      accountnumber: string | null;
      revenue: number | null;
    }>();
  });

  it("accepts shared selectors only for compatible entity contracts", () => {
    interface SharedSelect {
      emailaddress1: WebAttribute<SharedSelect, { emailaddress1: string | null }, object>;
      telephone1: WebAttribute<SharedSelect, { telephone1: string | null }, object>;
    }
    const shared = (x: SharedSelect) => [x.emailaddress1, x.telephone1] as const;
    const query = XrmQuery.retrieveMultiple(x => x.accounts).select(shared);
    expectTypeOf<ResultOfQuery<typeof query>>().toExtend<{
      accountid: string;
      emailaddress1: string | null;
      telephone1: string | null;
    }[]>();
    const record = XrmQuery.retrieve(x => x.accounts, "ID").select(shared).selectMore(x => [x.revenue]);
    expectTypeOf<ResultOfQuery<typeof record>>().toExtend<{
      accountid: string;
      emailaddress1: string | null;
      telephone1: string | null;
      revenue: number | null;
    }>();
    interface UnrelatedSelect {
      missing: WebAttribute<UnrelatedSelect, { missing: string }, object>;
    }
    const unrelated = (x: UnrelatedSelect) => [x.missing] as const;
    // @ts-expect-error The account entity does not satisfy this shared contract.
    query.select(unrelated);
    // @ts-expect-error selectMore must also reject incompatible contracts.
    record.selectMore(unrelated);
  });

  it("keeps inferring past the legacy 15-attribute overload cap", () => {
    const query = XrmQuery.retrieveMultiple((x) => x.accounts).select((x) => [
      x.accountid,
      x.name,
      x.accountnumber,
      x.address1_city,
      x.address1_line1,
      x.address1_line2,
      x.address1_line3,
      x.address1_country,
      x.address1_postalcode,
      x.address2_city,
      x.address2_line1,
      x.address2_line2,
      x.address2_line3,
      x.address2_country,
      x.address2_postalcode,
      x.emailaddress1,
      x.telephone1,
      x.websiteurl,
      x.description,
      x.revenue,
    ]);

    expectTypeOf<ResultOfQuery<typeof query>>().toExtend<
      { address2_postalcode: string | null; websiteurl: string | null; revenue: number | null }[]
    >();
  });

  it("appends to the result with selectMore", () => {
    const query = XrmQuery.retrieve((x) => x.accounts, "ID")
      .select((x) => [x.name])
      .selectMore((x) => [x.revenue]);

    expectTypeOf<ResultOfQuery<typeof query>>().toExtend<{
      name: string | null;
      revenue: number | null;
    }>();
  });

  it("adds the formatted values only after includeFormattedValues", () => {
    const selected = XrmQuery.retrieve((x) => x.accounts, "ID").select((x) => [x.revenue]);
    expectTypeOf<ResultOfQuery<typeof selected>>().not.toHaveProperty("revenue_formatted");

    const formatted = selected.includeFormattedValues();
    expectTypeOf<ResultOfQuery<typeof formatted>>().toExtend<{ revenue_formatted: string }>();
  });

  it("adds expanded records to the result", () => {
    const query = XrmQuery.retrieve((x) => x.accounts, "ID")
      .select((x) => [x.name])
      .expand(
        (x) => x.contact_customer_accounts,
        (x) => [x.fullname],
      );

    expectTypeOf<ResultOfQuery<typeof query>>().toExtend<{
      contact_customer_accounts: Contact_Result[];
    }>();
  });

  it("narrows getFirst to a single nullable record", () => {
    const query = XrmQuery.retrieveMultiple((x) => x.accounts).select((x) => [x.name]);
    expectTypeOf(query.getFirst).returns.resolves.toExtend<{ name: string | null } | null>();
  });

  it("types the builder methods against the entity's own attributes", () => {
    const query = XrmQuery.retrieveMultiple((x) => x.accounts);
    expectTypeOf(query).toExtend<
      RetrieveMultipleRecords<
        Account_Select,
        Account_Expand,
        Account_Filter,
        Account_Fixed,
        Account_FormattedResult,
        Account_Result
      >
    >();

    // @ts-expect-error `fullname` is a contact attribute, not an account one.
    query.select((x) => [x.fullname]);

    // @ts-expect-error a filter compares like with like.
    query.filter((x) => Filter.equals(x.name, 5));
  });

  it("types related queries against the related entity", () => {
    const query = XrmQuery.retrieveRelated(
      (x) => x.accounts,
      "ID",
      (x) => x.primarycontactid,
    ).select((x) => [x.fullname]);

    expectTypeOf<ResultOfQuery<typeof query>>().toExtend<{
      contactid: string;
      fullname: string | null;
    }>();
  });

  it("types create and update against the generated _Create/_Update shapes", () => {
    const create = XrmQuery.create((x) => x.accounts, {
      name: "Acme",
      parentaccountid_bind$accounts: "ID",
    });
    expectTypeOf<ResultOfQuery<typeof create>>().toEqualTypeOf<string>();

    // @ts-expect-error `fullname` is not an account attribute.
    XrmQuery.create((x) => x.accounts, { fullname: "Acme" });

    const update = XrmQuery.update((x) => x.accounts, "ID", { name: "Acme" });
    expectTypeOf<ResultOfQuery<typeof update>>().toEqualTypeOf<undefined>();
  });

  it("maps each batched query onto its own result type", () => {
    const batch = XrmQuery.batch([
      XrmQuery.retrieveMultiple((x) => x.accounts).select((x) => [x.name]),
      XrmQuery.create((x) => x.accounts, { name: "Acme" }),
    ] as const);

    expectTypeOf(batch.execute).returns.resolves.toExtend<
      readonly [{ name: string | null }[], string]
    >();
  });

  it("keeps a query assignable to the abstract Query type", () => {
    const query: Query<Account_Result[]> = XrmQuery.retrieveMultiple((x) => x.accounts);
    expectTypeOf(query.execute).returns.resolves.toEqualTypeOf<Account_Result[]>();
  });
});
