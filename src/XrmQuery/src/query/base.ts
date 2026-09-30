import { stripGuid } from "../parse.js";
import type { HttpMethod, RawRequest, RawResponse, RequestHeader } from "../http.js";
import { send } from "../http.js";

/**
 * Base class for every query. Subclasses supply the query string, the body and the response
 * handling; everything to do with headers, impersonation, cancellation and execution lives here.
 */
export abstract class Query<T> {
  /** @internal */
  protected additionalHeaders: RequestHeader[] = [];
  /** @internal */
  protected abortSignal: AbortSignal | undefined = undefined;

  constructor(
    /** @internal */
    protected requestType: HttpMethod,
  ) {}

  abstract getQueryString(): string;

  /** @internal */
  protected abstract handleResponse(response: RawResponse): T | Promise<T>;

  /** @internal */
  protected getObjectToSend(): string | undefined {
    return undefined;
  }

  /** Adds an arbitrary header to this request. */
  header(type: string, value: string): this {
    this.additionalHeaders.push({ type, value });
    return this;
  }

  /** Executes this request on behalf of another user (`MSCRMCallerID`). */
  impersonate(userId: string): this {
    return this.header("MSCRMCallerID", stripGuid(userId));
  }

  /** Aborts this request when the given signal fires. */
  signal(signal: AbortSignal): this {
    this.abortSignal = signal;
    return this;
  }

  /** The request this query will send. Used by `$batch`, and handy for debugging. */
  toRequest(): RawRequest {
    return {
      method: this.requestType,
      queryString: this.getQueryString(),
      body: this.getObjectToSend(),
      headers: this.additionalHeaders,
      signal: this.abortSignal,
    };
  }

  /**
   * Applies this query's response handling to a response obtained elsewhere — used by `$batch`,
   * where one HTTP call carries the responses of many queries.
   * @internal
   */
  handleRawResponse(response: RawResponse): T | Promise<T> {
    return this.handleResponse(response);
  }

  /** Sends the request and resolves with the parsed result. */
  async execute(): Promise<T> {
    const response = await send(this.toRequest());
    return this.handleResponse(response);
  }

  /** Alias of {@link execute}, kept for source compatibility with the legacy library. */
  promise(): Promise<T> {
    return this.execute();
  }
}

/** Turns a union into an intersection — used to fold selected attributes into one result type. */
export type UnionToIntersection<U> = (U extends unknown ? (k: U) => void : never) extends (
  k: infer I,
) => void
  ? I
  : never;

// eslint-disable-next-line @typescript-eslint/no-explicit-any
type AttributeResult<A> = A extends WebAttribute<any, infer R, any> ? R : never;
// eslint-disable-next-line @typescript-eslint/no-explicit-any
type AttributeFormatted<A> = A extends WebAttribute<any, any, infer F> ? F : never;

/** Intersection of the result types of every attribute in a select tuple. */
export type ResultOf<T extends readonly unknown[]> = UnionToIntersection<
  AttributeResult<T[number]>
>;

/** Intersection of the formatted-value types of every attribute in a select tuple. */
export type FormattedOf<T extends readonly unknown[]> = UnionToIntersection<
  AttributeFormatted<T[number]>
>;

/** Allows a shared selector only when the queried entity satisfies its attribute contract. */
export type CompatibleSelection<S, T extends readonly unknown[]> = {
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  [K in keyof T]: T[K] extends WebAttribute<infer A, any, any>
    ? S extends A ? T[K] : never
    : never;
};
