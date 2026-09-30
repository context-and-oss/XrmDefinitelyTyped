import type { CompatibleSelection } from "./base.js";
import { FORMATTED_VALUES_HEADER, INCLUDE_ANNOTATIONS_HEADER } from "../headers.js";
import { idToString, type RecordId } from "../id.js";
import type { RawResponse } from "../http.js";
import { parseResponseBody } from "../parse.js";
import { parseRaw, parseSelects, parseSingle, taggedForCrm } from "../proxy.js";
import { SortOrder } from "../globals.js";
import { populateRecord } from "./links.js";
import { Query, type FormattedOf, type ResultOf } from "./base.js";

/** A query for a single record. */
export class RetrieveRecord<
  ISelect,
  IExpand,
  IFixed,
  FormattedResult,
  Result,
> extends Query<Result> {
  /** @internal */
  protected selects: string[] = [];
  /** @internal */
  protected expands: string[] = [];
  /** @internal */
  protected expandKeys: string[] = [];

  static Get<ISelect, IExpand, IFixed, FormattedResult, Result>(
    entityPicker: (
      x: WebEntitiesRetrieve,
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
    ) => WebMappingRetrieve<ISelect, IExpand, any, IFixed, Result, FormattedResult>,
    id: RecordId,
  ): RetrieveRecord<ISelect, IExpand, IFixed, FormattedResult, Result> {
    return new RetrieveRecord(parseRaw(entityPicker), id);
  }

  static Related<ISingle, ISelect, IExpand, IFixed, FormattedResult, Result>(
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    entityPicker: (x: WebEntitiesRelated) => WebMappingRelated<ISingle, any>,
    id: RecordId,
    relatedPicker: (
      x: ISingle,
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
    ) => WebMappingRetrieve<ISelect, IExpand, any, IFixed, Result, FormattedResult>,
  ): RetrieveRecord<ISelect, IExpand, IFixed, FormattedResult, Result> {
    return new RetrieveRecord(parseRaw(entityPicker), id, parseRaw(relatedPicker));
  }

  private constructor(
    private entitySetName: string,
    private id: RecordId,
    private relatedNav?: string,
  ) {
    super("GET");
  }

  /** @internal */
  protected async handleResponse(response: RawResponse): Promise<Result> {
    const record = parseResponseBody<Result>(response.text);
    await populateRecord(record, this.expandKeys, {
      headers: this.additionalHeaders,
      signal: this.abortSignal,
    });
    return record;
  }

  /** Selects the attributes to retrieve, replacing any previous selection. */
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  select<const T extends readonly WebAttribute<any, any, any>[]>(
    varFunc: (x: ISelect) => T & CompatibleSelection<ISelect, T>,
  ): RetrieveRecord<ISelect, IExpand, IFixed, FormattedOf<T>, IFixed & ResultOf<T>> {
    this.selects = parseSelects(varFunc as (x: never) => readonly unknown[]);
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    return this as any;
  }

  /** Adds more attributes to the current selection. */
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  selectMore<const T extends readonly WebAttribute<any, any, any>[]>(
    varFunc: (x: ISelect) => T & CompatibleSelection<ISelect, T>,
  ): RetrieveRecord<
    ISelect,
    IExpand,
    IFixed,
    FormattedResult & FormattedOf<T>,
    Result & ResultOf<T>
  > {
    this.selects = this.selects.concat(parseSelects(varFunc as (x: never) => readonly unknown[]));
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    return this as any;
  }

  /** Expands a related record or collection, optionally selecting and shaping it. */
  expand<IExpSelect, IExpFilter, IExpResult>(
    exps: (x: IExpand) => WebExpand<IExpand, IExpSelect, IExpFilter, IExpResult>,
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    selectVarFunc?: (x: IExpSelect) => readonly WebAttribute<IExpSelect, any, any>[],
    optArgs?: ExpandOptions<IExpSelect, IExpFilter>,
  ): RetrieveRecord<ISelect, IExpand, IFixed, FormattedResult, IExpResult & Result> {
    const expand = parseRaw(exps as (x: never) => unknown);
    this.expandKeys.push(expand);

    const options: string[] = [];
    if (selectVarFunc)
      options.push(`$select=${parseSelects(selectVarFunc as (x: never) => readonly unknown[])}`);
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

  getQueryString(): string {
    let prefix = `${this.entitySetName}(${idToString(this.id)})`;

    const options: string[] = [];
    if (this.selects.length > 0) options.push("$select=" + this.selects.join(","));
    if (this.expands.length > 0) options.push("$expand=" + this.expands.join(","));

    if (this.relatedNav) prefix += `/${this.relatedNav}`;

    return prefix + (options.length > 0 ? "?" + options.join("&") : "");
  }

  /**
   * Also retrieves formatted values. Use after selecting and expanding attributes, since it narrows
   * the result type.
   */
  includeFormattedValues(): Query<FormattedResult & Result> {
    this.additionalHeaders.push(FORMATTED_VALUES_HEADER);
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    return this as any;
  }

  /** Also retrieves formatted values and lookup properties. */
  includeFormattedValuesAndLookupProperties(): Query<FormattedResult & Result> {
    this.additionalHeaders.push(INCLUDE_ANNOTATIONS_HEADER);
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    return this as any;
  }
}
