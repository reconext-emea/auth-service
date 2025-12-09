import { PublicClientApplication, } from "../../node_modules/@azure/msal-browser/dist/index";
export var AuthService;
(function (AuthService) {
    /**
     * Represents the allowed runtime environments for `BydIntranetClient`.
     *
     * The special `"_"` member is used as a type guard: it prevents consumers
     * from passing the entire enum as a generic parameter (e.g. `create<Environment>`).
     * Only the specific members `Environment.Development` and `Environment.Production`
     * are valid options when calling `BydIntranetClient.create<T>()`.
     */
    let Environment;
    (function (Environment) {
        Environment[Environment["Development"] = 0] = "Development";
        Environment[Environment["Production"] = 1] = "Production";
        Environment[Environment["_"] = 2] = "_";
    })(Environment = AuthService.Environment || (AuthService.Environment = {}));
    class MsalBrowser {
        constructor(configuration) {
            this.instance = new PublicClientApplication({
                auth: configuration,
            });
        }
        async initialize() {
            await this.instance.initialize();
            await this.instance.handleRedirectPromise();
            return new InitializedMsalBrowser(this.instance);
        }
    }
    class InitializedMsalBrowser {
        constructor(instance) {
            this.instance = instance;
        }
        async login() {
            const result = await this.instance.loginPopup({
                scopes: ["api://e05b3070-b0d6-4cd0-b76c-16a46b820bd4/access_as_user"],
            });
            this.instance.setActiveAccount(result.account);
            const graphToken = await this.instance.acquireTokenSilent({
                scopes: ["User.Read"],
            });
            return { accessToken: result.accessToken, graphToken: graphToken.accessToken };
        }
        async logout() {
            await this.instance.logoutPopup();
        }
    }
    class AuthClient {
        static async fetch(input, init) {
            const response = await fetch(input, init);
            const authResponse = response;
            authResponse.json = response.json.bind(response);
            return authResponse;
        }
    }
    class BydIntranetClient extends AuthClient {
        constructor() {
            super();
            this.initialized = null;
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
        static create(msalConfig) {
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
        async init() {
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
        async msalLogin(scopes = []) {
            if (!this.initialized)
                await this.init();
            const { accessToken, graphToken } = await this.initialized.login();
            const params = new URLSearchParams({
                grant_type: "urn:entra:access_token",
                access_token: accessToken,
                graph_token: graphToken,
                scope: [...BydIntranetClient.AUTH_SCOPES, ...scopes].join(" "),
            });
            const clientResponse = await BydIntranetClient.fetch(`${this.endpoint}/connect/token`, {
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
        async msalLogout() {
            if (!this.initialized)
                await this.init();
            await this.initialized.logout();
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
        async ldapLogin(username, password, scopes = []) {
            const params = new URLSearchParams({
                grant_type: "password",
                username,
                password,
                domain: "reconext.com",
                scope: [...BydIntranetClient.AUTH_SCOPES, ...scopes].join(" "),
            });
            const clientResponse = await BydIntranetClient.fetch(`${this.endpoint}/connect/token`, {
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
        isAuthError(error) {
            return (typeof error === "object" &&
                error !== null &&
                typeof error?.error === "string" &&
                typeof error?.error_description === "string");
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
        async saveErrorAsync(error) {
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
        async refreshTokenAsync(refreshToken, scopes = []) {
            const params = new URLSearchParams({
                grant_type: "refresh_token",
                refresh_token: refreshToken,
                scope: [...BydIntranetClient.AUTH_SCOPES, ...scopes].join(" "),
            });
            const clientResponse = await BydIntranetClient.fetch(`${this.endpoint}/connect/token`, {
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
        static decodeJwt(token) {
            try {
                const [, payload] = token.split(".");
                if (!payload)
                    throw new Error("Invalid JWT format.");
                const decoded = atob(payload.replace(/-/g, "+").replace(/_/g, "/"));
                return JSON.parse(decoded);
            }
            catch {
                throw new Error("Failed to decode JWT.");
            }
        }
    }
    BydIntranetClient.ENDPOINT = "https://10.41.0.85:5081";
    BydIntranetClient.TEST_ENDPOINT = "http://localhost:5081";
    BydIntranetClient.CLIENT_ID = "2d4d603d-f0bc-4727-9b23-40b08c2e6e63";
    BydIntranetClient.TEST_CLIENT_ID = "e05b3070-b0d6-4cd0-b76c-16a46b820bd4";
    BydIntranetClient.AUTHORITY = "https://login.microsoftonline.com/7e8ee4aa-dcc0-4745-ad28-2f942848ac88/v2.0";
    BydIntranetClient.TEST_REDIRECT_URI = "http://localhost";
    BydIntranetClient.TEST_POST_LOGOUT_REDIRECT_URI = "http://localhost";
    BydIntranetClient.AUTH_SCOPES = ["openid", "offline_access", "roles"];
    AuthService.BydIntranetClient = BydIntranetClient;
})(AuthService || (AuthService = {}));
