// import {
//   BrowserAuthOptions,
//   PublicClientApplication,
// } from "../../node_modules/@azure/msal-browser/dist/index";
import { BrowserAuthOptions, PublicClientApplication } from "@azure/msal-browser";

export namespace AuthService {
  /**
   * Response returned from `/connect/token`.
   */
  export interface IConnectTokenResponse {
    /** Access token (JWT) used to call backend APIs. */
    access_token: string;

    /** ID token (JWT) used by the frontend to identify the user. */
    id_token: string;

    /** Refresh token used to obtain new access/id tokens. */
    refresh_token: string;

    /** Access token lifetime in seconds. */
    expires_in: number;

    /** Token type — always "Bearer" (OAuth2 standard). */
    token_type: "Bearer";
  }

  /**
   * Base JWT fields shared between access_token and id_token.
   */
  export interface IToken {
    /** Expiration timestamp (seconds since UNIX epoch). */
    exp: number;

    /** Issued-at timestamp (seconds since UNIX epoch). */
    iat: number;

    /** Token issuer (URL of the auth service). */
    iss: string;

    /** OpenIddict internal auth session ID. */
    oi_au_id: string;

    /** OpenIddict internal token ID. */
    oi_tkn_id: string;
  }

  /**
   * Shared identity fields injected into both access_token and id_token.
   */
  export interface ITokenSub {
    /** Unique user identifier (GUID). */
    sub: string;

    /** Username used for sign-in. */
    sub_username: string;

    /** User email address. */
    sub_email: string;

    /** User office location. */
    sub_office_location: string;
  }

  /**
   * Decoded access token fields.
   */
  export interface IAccessToken extends IToken, ITokenSub {
    /** OAuth2 scopes granted by the token (space-delimited). */
    scope: string;

    /** Token unique ID (JWT ID). */
    jti: string;
  }

  /**
   * Decoded ID token fields.
   */
  export interface TIdToken extends IToken, ITokenSub {
    /** User-friendly display name. */
    sub_display_username: string;

    /** Hash of the access token (OpenID Connect requirement). */
    at_hash: string;

    /**
     * Audience claim — identifies the intended client.
     * May be missing when using anonymous clients (Currently missing).
     */
    aud?: string[];
  }

  /**
   * Represents the allowed runtime environments for `BydIntranetClient`.
   *
   * The special `"_"` member is used as a type guard: it prevents consumers
   * from passing the entire enum as a generic parameter (e.g. `create<Environment>`).
   * Only the specific members `Environment.Development` and `Environment.Production`
   * are valid options when calling `BydIntranetClient.create<T>()`.
   */
  export enum Environment {
    Development,
    Production,
    _,
  }

  /**
   * Represents an OAuth2 error response returned by the `/connect/token` endpoint.
   *
   * When request fails (e.g., invalid credentials), the server responds with
   * HTTP 400 *and* a JSON payload following the OAuth2 error format. This is not
   * thrown as an exception — it is a valid response body that your code receives
   * as an object:
   *
   *   { error: "invalid_grant", error_description: "Invalid username or password." }
   *
   * Fields:
   * - `error` — short error code identifying the failure.
   * - `error_description` — human-readable explanation.
   *
   * Both fields are safe to display to end users. OAuth2 guarantees these values
   * never contain sensitive information.
   */
  export interface IAuthError {
    error: string;
    error_description: string;
  }

  /**
   * MSAL configuration for the selected environment.
   *
   * - **Production**: may include redirect URIs (optional for SPAs).
   *   If omitted, MSAL Browser defaults:
   *     - `redirectUri` → current page (`window.location.href`)
   *     - `postLogoutRedirectUri` → `redirectUri` (or current page if absent)
   *   Explicit redirect URIs are only needed when authentication is handled
   *   on a different page than where the flow starts (e.g., non-SPA apps).
   *
   * - **Development**: type is always `null`.
   *   Your client wrapper uses this to apply
   *   `"http://localhost"` for both `redirectUri` and `postLogoutRedirectUri`.
   */
  export type MsalAuthConfig<T extends Environment.Development | Environment.Production> =
    T extends Environment.Production
      ? Pick<BrowserAuthOptions, "redirectUri" | "postLogoutRedirectUri">
      : null;

  interface IMsalBrowser {
    initialize(): Promise<InitializedMsalBrowser | null>;
  }

  interface IInitializedMsalBrowser {
    login(): Promise<{ accessToken: string; graphToken: string }>;
    logout(): Promise<void>;
  }

  interface IIntranetMsalClient {
    msalLogin(): Promise<IConnectTokenResponse | void>;
    msalLogout(): Promise<void>;
  }

  class MsalBrowser implements IMsalBrowser {
    protected instance: PublicClientApplication;

    constructor(configuration: BrowserAuthOptions) {
      this.instance = new PublicClientApplication({
        auth: configuration,
      });
    }

    async initialize(): Promise<InitializedMsalBrowser | null> {
      await this.instance.initialize();
      await this.instance.handleRedirectPromise();

      return new InitializedMsalBrowser(this.instance);
    }
  }

  class InitializedMsalBrowser implements IInitializedMsalBrowser {
    constructor(private instance: PublicClientApplication) {}

    async login(): Promise<{ accessToken: string; graphToken: string }> {
      const result = await this.instance.loginPopup({
        scopes: ["api://e05b3070-b0d6-4cd0-b76c-16a46b820bd4/access_as_user"],
      });

      this.instance.setActiveAccount(result.account);

      const graphToken = await this.instance.acquireTokenSilent({
        scopes: ["User.Read"],
      });

      return { accessToken: result.accessToken, graphToken: graphToken.accessToken };
    }

    async logout(): Promise<void> {
      await this.instance.logoutPopup();
    }
  }

  interface AuthResponse<T> extends Response {
    json(): Promise<T>;
  }

  class AuthClient {
    protected static async fetch<T>(
      input: string | Request | URL,
      init?: RequestInit | undefined
    ): Promise<AuthResponse<T>> {
      const response = await fetch(input, init);

      const authResponse = response as AuthResponse<T>;
      authResponse.json = response.json.bind(response) as () => Promise<T>;

      return authResponse;
    }
  }

  export class BydIntranetClient extends AuthClient implements IIntranetMsalClient {
    private static readonly ENDPOINT = "https://10.41.0.85:5081";
    private static readonly TEST_ENDPOINT = "http://localhost:5081";

    private static readonly CLIENT_ID = "2d4d603d-f0bc-4727-9b23-40b08c2e6e63";
    private static readonly TEST_CLIENT_ID = "e05b3070-b0d6-4cd0-b76c-16a46b820bd4";

    private static readonly AUTHORITY =
      "https://login.microsoftonline.com/7e8ee4aa-dcc0-4745-ad28-2f942848ac88/v2.0";
    private static readonly TEST_REDIRECT_URI = "http://localhost";
    private static readonly TEST_POST_LOGOUT_REDIRECT_URI = "http://localhost";
    private static readonly AUTH_SCOPES: string[] = ["openid", "offline_access"];

    private endpoint!: string;

    private msal!: IMsalBrowser;
    private initialized: IInitializedMsalBrowser | null = null;

    private constructor() {
      super();
    }

    /**
     * Creates and configures a new `BydIntranetClient` instance.
     *
     * @genericType T
     * Determines which MSAL configuration rules apply. Must be either:
     * - `Environment.Development`
     * - `Environment.Production`
     *
     * @param msalConfig
     * Environment-specific MSAL configuration.
     *
     * - **Production**: `msalConfig` may contain redirect URIs.
     *   If omitted, MSAL Browser defaults:
     *     - `redirectUri` → `window.location.href`
     *     - `postLogoutRedirectUri` → `redirectUri` (or current page)
     *
     * - **Development**: must be `null`.
     *   In this mode, the client applies internal defaults:
     *     - redirectUri → `"http://localhost"`
     *     - postLogoutRedirectUri → `"http://localhost"`
     *
     * @returns A fully configured `BydIntranetClient` ready for MSAL and token operations.
     *
     * @throws {Error} If required MSAL configuration values are missing for the selected environment.
     */
    static create<
      T extends Environment.Production | Environment.Development = Environment.Production
    >(msalConfig: MsalAuthConfig<T>): BydIntranetClient {
      const client = new BydIntranetClient();

      client.endpoint =
        msalConfig !== null ? BydIntranetClient.ENDPOINT : BydIntranetClient.TEST_ENDPOINT;
      client.msal = new MsalBrowser({
        authority: BydIntranetClient.AUTHORITY,
        clientId: BydIntranetClient.CLIENT_ID,
        ...(msalConfig
          ? {
              ...msalConfig,
            }
          : {
              clientId: BydIntranetClient.TEST_CLIENT_ID,
              redirectUri: BydIntranetClient.TEST_REDIRECT_URI,
              postLogoutRedirectUri: BydIntranetClient.TEST_POST_LOGOUT_REDIRECT_URI,
            }),
      });

      return client;
    }

    private async init(): Promise<boolean> {
      this.initialized = await this.msal.initialize();
      return this.initialized !== null;
    }

    /**
     * Performs an interactive MSAL login and exchanges the resulting MSAL access token
     * for a backend-issued token via `/connect/token`.
     *
     * Ensures MSAL is initialized before the login begins.
     * If login or initialization fails, the error is thrown to the caller.
     *
     * @param scopes Additional OAuth2 scopes to request from the backend.
     *               The app's default scopes already include all permissions supported
     *               by this AuthService. Any extra scopes passed here are simply added
     *               to the resulting JWT and may be used by other backend APIs that
     *               understand them – they do not change AuthService behavior.
     *
     * @returns A backend token response (`IConnectTokenResponse`) on success.
     *
     * @throws {Error} If MSAL initialization fails or MSAL's interactive login fails.
     *
     * Note:
     * If the backend rejects the token request (e.g., invalid credentials, disabled account),
     * the `/connect/token` endpoint returns an OAuth2 error object:
     *   { error: "invalid_grant", error_description: "Invalid username or password." }
     * This is **not** thrown as an exception — it arrives as JSON and can be checked using `isAuthError()`.
     */
    public async msalLogin(scopes: string[] = []): Promise<IConnectTokenResponse> {
      if (!this.initialized) await this.init();

      const { accessToken, graphToken } = await this.initialized!.login();

      const params = new URLSearchParams({
        grant_type: "urn:entra:access_token",
        access_token: accessToken,
        graph_token: graphToken,
        scope: [...BydIntranetClient.AUTH_SCOPES, ...scopes].join(" "),
      });

      const clientResponse: AuthResponse<IConnectTokenResponse> =
        await BydIntranetClient.fetch<IConnectTokenResponse>(`${this.endpoint}/connect/token`, {
          method: "POST",
          headers: { "Content-Type": "application/x-www-form-urlencoded" },
          body: params.toString(),
        });

      return clientResponse.json();
    }

    /**
     * Logs out the currently authenticated MSAL account.
     *
     * Ensures MSAL is initialized before performing logout.
     *
     * @returns Resolves when logout completes successfully.
     *
     * @throws {Error} If MSAL initialization fails or logout encounters an error.
     */
    public async msalLogout(): Promise<void> {
      if (!this.initialized) await this.init();

      await this.initialized!.logout();
    }

    /**
     * Performs a Resource Owner Password Credentials (ROPC) login against the backend.
     *
     * Sends the username and password directly to the `/connect/token` endpoint
     * using the OAuth2 password grant.
     *
     * @param scopes Additional OAuth2 scopes to request from the backend.
     *               The app's default scopes already include all permissions supported
     *               by this AuthService. Any extra scopes passed here are simply added
     *               to the resulting JWT and may be used by other backend APIs that
     *               understand them – they do not change AuthService behavior.
     *
     * @returns A backend token response (`IConnectTokenResponse`) on success.
     *
     * @throws {Error} Network errors or unexpected failures.
     *
     * Note:
     * Authentication failures return OAuth2 error objects
     * (e.g., `{ error: "invalid_grant", error_description: "Invalid username or password." }`)
     * and should be detected with `isAuthError()`.
     */
    public async ldapLogin(
      username: string,
      password: string,
      scopes: string[] = []
    ): Promise<IConnectTokenResponse> {
      const params = new URLSearchParams({
        grant_type: "password",
        username,
        password,
        domain: "reconext.com",
        scope: [...BydIntranetClient.AUTH_SCOPES, ...scopes].join(" "),
      });

      const clientResponse: AuthResponse<IConnectTokenResponse> =
        await BydIntranetClient.fetch<IConnectTokenResponse>(`${this.endpoint}/connect/token`, {
          method: "POST",
          headers: { "Content-Type": "application/x-www-form-urlencoded" },
          body: params.toString(),
        });

      return clientResponse.json();
    }

    /**
     * Determines whether the provided value matches the OAuth2 error format returned
     * by the `/connect/token` endpoint.
     *
     * OAuth2 error responses always contain:
     *   - `error` (short code)
     *   - `error_description` (human-readable explanation)
     *
     * @param error The value to inspect.
     * @returns `true` if the value is an OAuth2 error object, otherwise `false`.
     *
     * These values are safe to display to end users. OAuth2 guarantees they contain
     * no sensitive information.
     */
    public isAuthError(error: unknown): error is IAuthError {
      return (
        typeof error === "object" &&
        error !== null &&
        typeof (error as any)?.error === "string" &&
        typeof (error as any)?.error_description === "string"
      );
    }

    /**
     * Saves an error to the backend's `/api/error-log` endpoint for administrative diagnostics.
     *
     * A unique reference ID is generated and returned to the caller.
     * This ID is safe to show to end users so that administrators can
     * locate the corresponding detailed error record.
     *
     * @param error Any error object or value to be serialized and stored.
     * @returns The generated reference ID linked to the stored error.
     */
    public async saveErrorAsync(error: unknown): Promise<string> {
      const reference = crypto.randomUUID();

      await fetch(`${this.endpoint}/api/error-log`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          reference,
          errorDetails: JSON.stringify(error, null, 2),
        }),
      });

      return reference;
    }

    /**
     * Exchanges a refresh token for a new access token using `/connect/token`.
     *
     * This uses the OAuth2 refresh_token grant:
     *   grant_type="refresh_token"
     *   refresh_token="<token>"
     *
     * @param refreshToken The refresh token previously returned by `/connect/token`.
     *
     * @param scopes Additional OAuth2 scopes to request from the backend.
     *               The app's default scopes already include all permissions supported
     *               by this AuthService. Any extra scopes passed here are simply added
     *               to the resulting JWT and may be used by other backend APIs that
     *               understand them – they do not change AuthService behavior.
     *
     * @returns A new `IConnectTokenResponse` containing updated tokens.
     *
     * @throws {Error} For network errors or unexpected failures.
     *
     * Note:
     * If the refresh token is invalid or expired, `/connect/token` returns
     * an OAuth2 error object, which can be detected using `isAuthError()`.
     */
    public async refreshTokenAsync(
      refreshToken: string,
      scopes: string[] = []
    ): Promise<IConnectTokenResponse> {
      const params = new URLSearchParams({
        grant_type: "refresh_token",
        refresh_token: refreshToken,
        scope: [...BydIntranetClient.AUTH_SCOPES, ...scopes].join(" "),
      });

      const clientResponse: AuthResponse<IConnectTokenResponse> =
        await BydIntranetClient.fetch<IConnectTokenResponse>(`${this.endpoint}/connect/token`, {
          method: "POST",
          headers: { "Content-Type": "application/x-www-form-urlencoded" },
          body: params.toString(),
        });

      return clientResponse.json();
    }

    /**
     * Decodes a JWT without validating its signature.
     * Returns the payload as an object.
     *
     * @param token The JWT string to decode.
     * @returns The decoded payload object.
     *
     * @throws {Error} If the token is not a valid JWT.
     */
    public static decodeJwt(token: string): Record<string, unknown> {
      try {
        const [, payload] = token.split(".");
        if (!payload) throw new Error("Invalid JWT format.");

        const decoded = atob(payload.replace(/-/g, "+").replace(/_/g, "/"));
        return JSON.parse(decoded);
      } catch {
        throw new Error("Failed to decode JWT.");
      }
    }
  }
}
