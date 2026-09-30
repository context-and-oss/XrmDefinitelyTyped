import type { CompatibleSelection } from "./base.js";
import { Filter } from "../filter.js";
import {
  FORMATTED_VALUES_HEADER,
  INCLUDE_ANNOTATIONS_HEADER,
  maxPageSizeHeader,
} from "../headers.js";
import { idToString, type RecordId } from "../id.js";
import { send, type RawResponse } from "../http.js";
import { parseResponseBody, type MultiResult } from "../parse.js";
import { parseRaw, parseSelects, parseSingle, taggedForCrm } from "../proxy.js";
import { SortOrder } from "../globals.js";
import { followPages, iteratePages } from "./links.js";
import { Query, type FormattedOf, type ResultOf } from "./base.js";

/** A query for a set of records. */
export class RetrieveMultipleRecords<
  ISelect,
  IExpand,
  IFilter,
  IFixed,
  FormattedResult,
  Result,
> extends Query<Result[]> {
  /** @internal */
  private specialQuery: string | undefined = undefined;
  /** @internal */
  private selects: string[] = [];
  /** @internal */
  private expands: string[] = [];
  /** @internal */
  private expandKeys: string[] = [];
  /** @internal */
  private explicitQuery: ExplicitQuery = {};
  /** @internal */
  private ordering: string[] = [];
  /** @internal */
  private filters: WebFilter | undefined = undefined;
  /** @internal */
  private skipAmount: number | null = null;
  /** @internal */
  private topAmount: number | null = null;

  static Get<ISelect, IExpand, IFilter, IFixed, FormattedResult, Result>(
    entityPicker: (
      x: WebEntitiesRetrieve,
    ) => WebMappingRetrieve<ISelect, IExpand, IFilter, IFixed, Result, FormattedResult>,
  ): RetrieveMultipleRecords<ISelect, IExpand, IFilter, IFixed, FormattedResult, Result> {
    return new RetrieveMultipleRecords(parseRaw(entityPicker));
  }

  static Related<IMultiple, ISelect, IExpand, IFilter, IFixed, FormattedResult, Result>(
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    entityPicker: (x: WebEntitiesRelated) => WebMappingRelated<any, IMultiple>,
    id: RecordId,
    relatedPicker: (
      x: IMultiple,
    ) => WebMappingRetrieve<ISelect, IExpand, IFilter, IFixed, Result, FormattedResult>,
  ): RetrieveMultipleRecords<ISelect, IExpand, IFilter, IFixed, FormattedResult, Result> {
    return new RetrieveMultipleRecords(parseRaw(entityPicker), id, parseRaw(relatedPicker));
  }

  private constructor(
    private entitySetName: string,
    private id?: RecordId,
    private relatedNav?: string,
  ) {
    super("GET");
  }

  /** @internal */
  protected handleResponse(response: RawResponse): Promise<Result[]> {
    return followPages(parseResponseBody<MultiResult>(response.text), this.expandKeys, {
      headers: this.additionalHeaders,
      signal: this.abortSignal,
    });
  }

  /** Retrieves only the first matching record, or `null` when there is none. */
  async getFirst(): Promise<Result | null> {
    this.top(1);
    const records = await this.execute();
    return records.length > 0 ? records[0] : null;
  }

  /** Alias of {@link getFirst}, kept for source compatibility with the legacy library. */
  promiseFirst(): Promise<Result | null> {
    return this.getFirst();
  }

  /**
   * Iterates the result one page at a time, following `@odata.nextLink` lazily. Combine with
   * {@link maxPageSize} to control the page size.
   */
  async *pages(): AsyncGenerator<Result[], void, undefined> {
    const response = await send(this.toRequest());
    yield* iteratePages(parseResponseBody<MultiResult>(response.text), this.expandKeys, {
      headers: this.additionalHeaders,
      signal: this.abortSignal,
    });
  }

  /** Retrieves every page and returns all records — the same as {@link execute}. */
  all(): Promise<Result[]> {
    return this.execute();
  }

  getQueryString(): string {
    let prefix = this.entitySetName;

    if (this.id !== undefined && this.relatedNav) {
      prefix += `(${idToString(this.id)})/${this.relatedNav}`;
    }
    if (this.specialQuery) return prefix + this.specialQuery;

    const options: string[] = [];
    this.addOption(options, "select", this.selects);
    this.addOption(options, "expand", this.expands);
    this.addOption(options, "filter", this.filters ?? null);
    this.addOption(options, "orderby", this.ordering);
    this.addOption(options, "skip", this.skipAmount);
    this.addOption(options, "top", this.topAmount);

    return prefix + (options.length > 0 ? `?${options.join("&")}` : "");
  }

  /** @internal */
  private addOption(
    options: string[],
    name: keyof ExplicitQuery,
    values: number | string[] | WebFilter | null,
  ): void {
    const explicit = this.explicitQuery[name];
    const urlName = "$" + name + "=";
    if (explicit) {
      options.push(urlName + explicit);
    } else if (values !== null && values !== undefined) {
      if (Array.isArray(values)) {
        if (values.length > 0) options.push(urlName + values.join(","));
      } else {
        options.push(urlName + values);
      }
    }
  }

  /** Selects the attributes to retrieve, replacing any previous selection. */
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  select<const T extends readonly WebAttribute<any, any, any>[]>(
    vars: (x: ISelect) => T & CompatibleSelection<ISelect, T>,
  ): RetrieveMultipleRecords<
    ISelect,
    IExpand,
    IFilter,
    IFixed,
    FormattedOf<T>,
    IFixed & ResultOf<T>
  > {
    this.selects = parseSelects(vars as (x: never) => readonly unknown[]);
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    return this as any;
  }

  /** Adds more attributes to the current selection. */
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  selectMore<const T extends readonly WebAttribute<any, any, any>[]>(
    vars: (x: ISelect) => T & CompatibleSelection<ISelect, T>,
  ): RetrieveMultipleRecords<
    ISelect,
    IExpand,
    IFilter,
    IFixed,
    FormattedResult & FormattedOf<T>,
    Result & ResultOf<T>
  > {
    this.selects = this.selects.concat(parseSelects(vars as (x: never) => readonly unknown[]));
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    return this as any;
  }

  /** Overrides individual query options with raw OData strings. */
  explicit(query: ExplicitQuery): this {
    this.explicitQuery = query;
    return this;
  }

  /** Expands a related record or collection, optionally selecting and shaping it. */
  expand<IExpSelect, IExpFilter, IExpResult>(
    exps: (x: IExpand) => WebExpand<IExpand, IExpSelect, IExpFilter, IExpResult>,
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    selectVarFunc?: (x: IExpSelect) => readonly WebAttribute<IExpSelect, any, any>[],
    optArgs?: ExpandOptions<IExpSelect, IExpFilter>,
  ): RetrieveMultipleRecords<
    ISelect,
    IExpand,
    IFilter,
    IFixed,
    FormattedResult,
    IExpResult & Result
  > {
    const expand = parseRaw(exps as (x: never) => unknown);
    this.expandKeys.push(expand);

    const options: string[] = [];
    if (selectVarFunc) {
      options.push(`$select=${parseSelects(selectVarFunc as (x: never) => readonly unknown[])}`);
    }
    if (optArgs) {
      if (optArgs.top) options.push(`$top=${optArgs.top}`);
      if (optArgs.orderBy) {
        const direction = optArgs.sortOrder !== SortOrder.Descending ? "asc" : "desc";
        options.push(
          `$orderby=${parseSingle(optArgs.orderBy as (x: never) => unknown)} ${direction}`,
        );
      }
      if (optArgs.filter) {
        options.push(`$filter=${taggedForCrm(optArgs.filter as (x: never) => WebFilter)}`);
      }
    }

    this.expands.push(expand + (options.length > 0 ? `(${options.join(";")})` : ""));
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    return this as any;
  }

  /** Sets the filter, replacing any previous one. */
  filter(filter: (x: IFilter) => WebFilter): this {
    this.filters = taggedForCrm(filter as (x: never) => WebFilter);
    return this;
  }

  /** Ors an extra condition onto the current filter. */
  orFilter(filter: (x: IFilter) => WebFilter): this {
    if (this.filters) {
      this.filters = Filter.or(this.filters, taggedForCrm(filter as (x: never) => WebFilter));
      return this;
    }
    return this.filter(filter);
  }

  /** Ands an extra condition onto the current filter. */
  andFilter(filter: (x: IFilter) => WebFilter): this {
    if (this.filters) {
      this.filters = Filter.and(this.filters, taggedForCrm(filter as (x: never) => WebFilter));
      return this;
    }
    return this.filter(filter);
  }

  /** @internal */
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  private order(varFunc: (x: ISelect) => WebAttribute<ISelect, any, any>, by: string): this {
    this.ordering.push(parseSingle(varFunc as (x: never) => unknown) + " " + by);
    return this;
  }

  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  orderAsc(vars: (x: ISelect) => WebAttribute<ISelect, any, any>): this {
    return this.order(vars, "asc");
  }

  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  orderDesc(vars: (x: ISelect) => WebAttribute<ISelect, any, any>): this {
    return this.order(vars, "desc");
  }

  skip(amount: number): this {
    this.skipAmount = amount;
    return this;
  }

  top(amount: number): this {
    this.topAmount = amount;
    return this;
  }

  /** Asks Dataverse to return at most `size` records per page. */
  maxPageSize(size: number): this {
    this.additionalHeaders.push(maxPageSizeHeader(size));
    return this;
  }

  /**
   * Also retrieves formatted values. Use after selecting and expanding attributes, since it narrows
   * the result type.
   */
  includeFormattedValues(): Query<(FormattedResult & Result)[]> {
    this.additionalHeaders.push(FORMATTED_VALUES_HEADER);
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    return this as any;
  }

  /** Also retrieves formatted values and lookup properties. */
  includeFormattedValuesAndLookupProperties(): Query<(FormattedResult & Result)[]> {
    this.additionalHeaders.push(INCLUDE_ANNOTATIONS_HEADER);
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    return this as any;
  }

  /**
   * Filters the entity using the given FetchXML instead of the typed query options.
   * @param xml The query in FetchXML format
   */
  useFetchXml(xml: string): Query<Result[]> {
    this.specialQuery = `?fetchXml=${encodeURIComponent(xml)}`;
    return this;
  }

  /**
   * Filters the entity using a predefined query.
   * @param type Whether the query is a system view (`savedQuery`) or a personal one (`userQuery`)
   * @param guid GUID of the query record
   */
  usePredefinedQuery(type: "savedQuery" | "userQuery", guid: string): Query<Result[]> {
    this.specialQuery = `?${type}=${guid}`;
    return this;
  }
}
