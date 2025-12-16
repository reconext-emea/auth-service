import { BrowserAuthOptions, PublicClientApplication } from "@azure/msal-browser";
import {
  jwtVerify,
  createRemoteJWKSet,
  decodeJwt,
  JWSHeaderParameters,
  FlattenedJWSInput,
  JSONWebKeySet,
} from "jose";

namespace Common {
  export interface ClientResponse<T> extends Response {
    json(): Promise<T>;
  }

  export class Client {
    protected static async fetch<T>(
      input: string | Request | URL,
      init?: RequestInit | undefined
    ): Promise<ClientResponse<T>> {
      const response = await fetch(input, init);

      const authResponse = response as ClientResponse<T>;
      authResponse.json = response.json.bind(response) as () => Promise<T>;

      return authResponse;
    }
  }
}

export namespace UsersService {
  /**
   * List of allowed emea office locations, based on environmental variable: Ldap__AllowedEmeaOfficeNames.
   *
   * **Supposed to be adjusted accordingly to changes.**
   */
  export type OfficeLocation =
    | "Bydgoszcz Site (PL)"
    | "Havant Site (UK)"
    | "Prague Site (CZ)"
    | "REMOTE / HOME OFFICE"
    | "Tallinn Site (EE)"
    | "Zoetermeer Site (NL)";

  /**
   * List of allowed preferred language codes, based on: AuthService.Constants.PreferredLanguage.
   *
   * **Supposed to be adjusted accordingly to changes.**
   */
  export type PreferredLanguageCode = "en" | "pl" | "ua" | "cs";
  /**
   * List of allowed preferred language codes, based on: AuthService.Constants.ColorTheme.
   *
   * **Supposed to be adjusted accordingly to changes.**
   */
  export type ColorThemeCode = "light" | "dark";

  export interface ISettings {
    preferredLanguageCode: PreferredLanguageCode;
    colorThemeCode: ColorThemeCode;
  }

  export type PutSettings = ISettings;

  export type UserSettings<WithSettings> = WithSettings extends true ? ISettings : null;

  /**
   * Id (e.g., **123e4567-e89b-12d3-a456-426614174000**)
   *
   * Username (e.g., **marian.pazdzioch**)
   *
   * Email (e.g., **marian.pazdzioch@reconext.com**)
   *
   * Display Name (e.g., **Marian Pazdzioch**)
   */
  export interface IUser<WithSettings> {
    id: string;
    userName: string;
    email: string;
    displayName: string;
    officeLocation: OfficeLocation;
    appSettings: UserSettings<WithSettings>;
  }

  export type UserClaims = string[];
  export type RoleClaims = string[];
  export interface IClaims {
    userClaims: UserClaims;
    roleClaims: RoleClaims;
  }

  export type GetManyResponse<WithSettings> = IUser<WithSettings>[];

  export type GetOneResponse<WithSettings> = IUser<WithSettings>;

  export interface IMessage {
    message: string;
  }

  export type PutSettingsResponse = IMessage;

  export type GetClaimsResponse = IClaims;

  export type DeleteClaimResponse = IMessage;

  export type PostUserClaim = {
    tool: string;
    privilege: string;
  };

  export type PostUserClaimResponse = IMessage;

  export class UsersClient extends Common.Client {
    private static readonly ORIGIN = "https://10.41.0.85:5081";
    private static readonly DEVELOPMENT_ORIGIN = "http://localhost:5081";

    private baseUrl: string;
    private getBaseUrl(development: boolean) {
      return `${development ? UsersClient.DEVELOPMENT_ORIGIN : UsersClient.ORIGIN}`;
    }

    constructor(environment: "Development" | "Production" = "Development") {
      super();
      this.baseUrl = this.getBaseUrl(environment === "Development");
    }

    async getMany<WithSettings extends true | null>(
      includeSettings: WithSettings
    ): Promise<GetManyResponse<WithSettings>> {
      const clientResponse: Common.ClientResponse<GetManyResponse<WithSettings>> =
        await UsersClient.fetch<GetManyResponse<WithSettings>>(
          `${this.baseUrl}/api/users/many/${!!includeSettings}`,
          {
            method: "GET",
            headers: { "Content-Type": "application/json; charset=utf-8" },
          }
        );
      return clientResponse.json();
    }

    async getOne<WithSettings extends true | null>(
      userIdentifier: string,
      includeSettings: WithSettings
    ): Promise<GetOneResponse<WithSettings>> {
      const clientResponse: Common.ClientResponse<GetOneResponse<WithSettings>> =
        await UsersClient.fetch<GetOneResponse<WithSettings>>(
          `${this.baseUrl}/api/users/one/${userIdentifier}/${!!includeSettings}`,
          {
            method: "GET",
            headers: { "Content-Type": "application/json; charset=utf-8" },
          }
        );
      return clientResponse.json();
    }

    async putSettings(
      userIdentifier: string,
      userSettings: PutSettings
    ): Promise<PutSettingsResponse> {
      const clientResponse: Common.ClientResponse<PutSettingsResponse> =
        await UsersClient.fetch<PutSettingsResponse>(
          `${this.baseUrl}/api/users/one/${userIdentifier}/settings`,
          {
            method: "GET",
            headers: { "Content-Type": "application/json; charset=utf-8" },
            body: JSON.stringify(userSettings),
          }
        );
      return clientResponse.json();
    }

    async getClaims(userIdentifier: string): Promise<GetClaimsResponse> {
      const clientResponse: Common.ClientResponse<GetClaimsResponse> =
        await UsersClient.fetch<GetClaimsResponse>(
          `${this.baseUrl}/api/users/one/${userIdentifier}/claims`,
          {
            method: "GET",
            headers: { "Content-Type": "application/json; charset=utf-8" },
          }
        );
      return clientResponse.json();
    }

    async deleteUserClaim(
      userIdentifier: string,
      userClaimValue: string
    ): Promise<DeleteClaimResponse> {
      const clientResponse: Common.ClientResponse<DeleteClaimResponse> =
        await UsersClient.fetch<DeleteClaimResponse>(
          `${this.baseUrl}/api/users/one/${userIdentifier}/claims/${userClaimValue}`,
          {
            method: "GET",
            headers: { "Content-Type": "application/json; charset=utf-8" },
          }
        );
      return clientResponse.json();
    }

    async postUserClaim(
      userIdentifier: string,
      userClaim: PostUserClaim
    ): Promise<PostUserClaimResponse> {
      const clientResponse: Common.ClientResponse<PostUserClaimResponse> =
        await UsersClient.fetch<PostUserClaimResponse>(
          `${this.baseUrl}/api/users/one/${userIdentifier}/settings`,
          {
            method: "GET",
            headers: { "Content-Type": "application/json; charset=utf-8" },
            body: JSON.stringify(userClaim),
          }
        );
      return clientResponse.json();
    }
  }
}

export namespace AuthService {
  export interface Jwk {
    kid: string;
    kty: string;
    alg: string;
    use: string;
    n?: string;
    e?: string;
    crv?: string;
    x?: string;
    y?: string;
  }

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

  export class AuthIntranetClient extends Common.Client implements IIntranetMsalClient {
    private static readonly ORIGIN = "https://10.41.0.85:5081";
    private static readonly TEST_ORIGIN = "http://localhost:5081";

    private static readonly CLIENT_ID = "2d4d603d-f0bc-4727-9b23-40b08c2e6e63";
    private static readonly TEST_CLIENT_ID = "e05b3070-b0d6-4cd0-b76c-16a46b820bd4";

    private static readonly AUTHORITY =
      "https://login.microsoftonline.com/7e8ee4aa-dcc0-4745-ad28-2f942848ac88/v2.0";
    private static readonly TEST_REDIRECT_URI = "http://localhost";
    private static readonly TEST_POST_LOGOUT_REDIRECT_URI = "http://localhost";
    private static readonly AUTH_SCOPES: string[] = ["openid", "offline_access"];

    private endpoint!: string;
    private jwks!: {
      (protectedHeader?: JWSHeaderParameters, token?: FlattenedJWSInput): Promise<CryptoKey>;
      coolingDown: boolean;
      fresh: boolean;
      reloading: boolean;
      reload: () => Promise<void>;
      jwks: () => JSONWebKeySet | undefined;
    };
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
    >(msalConfig: MsalAuthConfig<T>): AuthIntranetClient {
      const client = new AuthIntranetClient();

      client.endpoint =
        msalConfig !== null ? AuthIntranetClient.ORIGIN : AuthIntranetClient.TEST_ORIGIN;
      client.jwks = createRemoteJWKSet(new URL(`${client.endpoint}/.well-known/jwks`));
      client.msal = new MsalBrowser({
        authority: AuthIntranetClient.AUTHORITY,
        clientId: AuthIntranetClient.CLIENT_ID,
        ...(msalConfig
          ? {
              ...msalConfig,
            }
          : {
              clientId: AuthIntranetClient.TEST_CLIENT_ID,
              redirectUri: AuthIntranetClient.TEST_REDIRECT_URI,
              postLogoutRedirectUri: AuthIntranetClient.TEST_POST_LOGOUT_REDIRECT_URI,
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
        scope: [...AuthIntranetClient.AUTH_SCOPES, ...scopes].join(" "),
      });

      const clientResponse: Common.ClientResponse<IConnectTokenResponse> =
        await AuthIntranetClient.fetch<IConnectTokenResponse>(`${this.endpoint}/connect/token`, {
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
        scope: [...AuthIntranetClient.AUTH_SCOPES, ...scopes].join(" "),
      });

      const clientResponse: Common.ClientResponse<IConnectTokenResponse> =
        await AuthIntranetClient.fetch<IConnectTokenResponse>(`${this.endpoint}/connect/token`, {
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
        scope: [...AuthIntranetClient.AUTH_SCOPES, ...scopes].join(" "),
      });

      const clientResponse: Common.ClientResponse<IConnectTokenResponse> =
        await AuthIntranetClient.fetch<IConnectTokenResponse>(`${this.endpoint}/connect/token`, {
          method: "POST",
          headers: { "Content-Type": "application/x-www-form-urlencoded" },
          body: params.toString(),
        });

      return clientResponse.json();
    }

    public static decodeJwt(token: string): Record<string, unknown> {
      return decodeJwt(token);
    }

    public async validateJwtSignature(token: string): Promise<Record<string, unknown>> {
      const { payload } = await jwtVerify(token, this.jwks, {
        issuer: `${this.endpoint}/`,
      });

      return payload;
    }
  }
}
