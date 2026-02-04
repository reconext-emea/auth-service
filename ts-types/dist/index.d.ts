import { AccountInfo, BrowserAuthOptions } from "@azure/msal-browser";
import { JWTPayload } from "jose";
declare namespace Common {
    interface ClientResponse<T> extends Response {
        json(): Promise<T>;
    }
    class Client {
        protected static fetch<T>(input: string | Request | URL, init?: RequestInit | undefined): Promise<ClientResponse<T>>;
    }
}
export declare namespace UsersService {
    interface JWTPayloadPassport {
        uuid: string;
        username: string;
        email: string;
        office_location: string;
        confidentiality: string;
        region: string;
        employeeId: string;
        department: string;
        jobTitle: string;
        permission: string[];
        role: string[];
    }
    /**
     * List of allowed emea office locations, based on environmental variable: Ldap__AllowedEmeaOfficeNames.
     *
     * **Supposed to be adjusted accordingly to changes.**
     */
    /**
     * List of allowed preferred language codes, based on: AuthService.Constants.PreferredLanguage.
     *
     * **Supposed to be adjusted accordingly to changes.**
     */
    /**
     * List of allowed preferred language codes, based on: AuthService.Constants.ColorTheme.
     *
     * **Supposed to be adjusted accordingly to changes.**
     */
}
export declare namespace AuthService {
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
        Development = 0,
        Production = 1,
        _ = 2
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
    export type MsalAuthConfig<T extends Environment.Development | Environment.Production> = T extends Environment.Production ? Pick<BrowserAuthOptions, "redirectUri" | "postLogoutRedirectUri"> : null;
    interface IIntranetMsalClient {
        msalLogin(): Promise<IConnectTokenResponse | void>;
        msalLogout(): Promise<void>;
        msalAccount(): Promise<AccountInfo | null>;
    }
    export class AuthIntranetClient extends Common.Client implements IIntranetMsalClient {
        private static readonly ORIGIN;
        private static readonly TEST_ORIGIN;
        private static readonly CLIENT_ID;
        private static readonly TEST_CLIENT_ID;
        private static readonly AUTHORITY;
        private static readonly TEST_REDIRECT_URI;
        private static readonly TEST_POST_LOGOUT_REDIRECT_URI;
        private static readonly AUTH_SCOPES;
        private host;
        private jwksHost;
        private jwks;
        private msal;
        private initialized;
        private constructor();
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
        static create<T extends Environment.Production | Environment.Development = Environment.Production>(msalConfig: MsalAuthConfig<T>): AuthIntranetClient;
        private init;
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
        msalLogin(scopes?: string[]): Promise<IConnectTokenResponse>;
        /**
         * Logs out the currently authenticated MSAL account.
         *
         * Ensures MSAL is initialized before performing logout.
         *
         * @returns Resolves when logout completes successfully.
         *
         * @throws {Error} If MSAL initialization fails or logout encounters an error.
         */
        msalLogout(): Promise<void>;
        msalAccount(): Promise<AccountInfo | null>;
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
        ldapLogin(username: string, password: string, scopes?: string[]): Promise<IConnectTokenResponse>;
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
        isAuthError(error: unknown): error is IAuthError;
        /**
         * Saves an error to the backend's `/api/auth-error` endpoint for administrative diagnostics.
         *
         * A unique reference ID is generated and returned to the caller.
         * This ID is safe to show to end users so that administrators can
         * locate the corresponding detailed error record.
         *
         * @param error Any error object or value to be serialized and stored.
         * @returns The generated reference ID linked to the stored error.
         */
        saveErrorAsync(error: unknown): Promise<string>;
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
        refreshTokenAsync(refreshToken: string, scopes?: string[]): Promise<IConnectTokenResponse>;
        static decodeJwt(token: string): Record<string, unknown>;
        validateJwtSignature(token: string): Promise<{
            payload: JWTPayload;
            passport: UsersService.JWTPayloadPassport;
        } | false>;
    }
    export namespace Dto {
        namespace Errors {
            interface ErrorResponseDto {
            }
        }
        namespace Roles {
            interface GetAccessLevelsResponseDto {
                accessLevels: string[];
            }
            interface GetPermissionTypesResponseDto {
                permissions: string[];
            }
            interface GetRolesResponseDto {
                roles: string[];
            }
            interface GetRolesOfUserResponseDto {
                roles: string[];
            }
            interface CreateRoleDto {
                tool: string;
                access: string;
            }
            interface CreateRoleResponseDto {
                message: string;
            }
            interface DeleteRoleResponseDto {
                message: string;
            }
            interface AssignRoleDto {
                userIdentifier: string;
                roleName: string;
            }
            interface AssignRoleResponseDto {
                message: string;
            }
            interface UnassignRoleDto {
                userIdentifier: string;
                roleName: string;
            }
            interface UnassignRoleResponseDto {
                message: string;
            }
        }
        namespace Miscellaneous {
            interface GetAllowedEmeaOffices {
                offices: string[];
            }
        }
        namespace Applications {
            interface ApplicationDto {
                id: string;
                clientId: string;
                displayName: string;
            }
            interface GetApplicationsResponseDto {
                applications: ApplicationDto[];
            }
            interface GetApplicationsOfUserResponseDto {
                applications: ApplicationDto[];
            }
        }
        namespace Users {
            interface GetDepartmentsResponseDto {
                departments: string[];
            }
            interface ImportUsersRequestDto {
                users: [
                    {
                        username: string;
                        roles: [
                            {
                                tool: string;
                                access: string;
                            }
                        ];
                        customProperties?: {
                            confidentiality: string;
                            programs: string[];
                        };
                    }
                ];
            }
            interface ImportUsersResponseDto {
                created: number;
                skipped: number;
                errors: [
                    {
                        error: string;
                        details: string;
                    }
                ];
                message: string;
            }
            interface DeleteUserResponseDto {
                message: string;
            }
            interface AuthServiceUserSettingsDto {
                preferredLanguageCode: string;
                preferredColorThemeCode: string;
            }
            interface AuthServiceUserCustomPropertiesDto {
                confidentiality: string;
                region: string;
                programs: ReadonlyArray<string>;
            }
            interface AuthServiceUserDto {
                id: string;
                userName: string;
                email: string;
                displayName: string;
                officeLocation: string;
                employeeId: string;
                department: string;
                jobTitle: string;
                appSettings: AuthServiceUserSettingsDto;
                customProperties: AuthServiceUserCustomPropertiesDto;
                applications: Dto.Applications.ApplicationDto[];
            }
            interface GetUsersResponseDto {
                users: AuthServiceUserDto[];
            }
            interface GetUserResponseDto {
                user: AuthServiceUserDto;
            }
            interface UpdateEmployeeIdDto {
                employeeId: string;
            }
            interface UpdateEmployeeIdResponseDto {
                message: string;
            }
            interface UpdateUserSettingsDto {
                preferredLanguageCode: string;
                preferredColorThemeCode: string;
            }
            interface UpdateUserSettingsResponseDto {
                message: string;
            }
            interface UpdateUserPropertiesDto {
                confidentiality: string;
                programs: string[];
            }
            interface UpdateUserPropertiesResponseDto {
                message: string;
            }
            interface GetUserClaimsResponseDto {
                userClaims: string[];
                roleClaims: string[];
            }
            interface DeleteClaimFromUserResponseDto {
                message: string;
            }
            interface AddClaimToUserDto {
                tool: string;
                privilege: string;
            }
            interface AddClaimToUserDtoResponseDto {
                message: string;
            }
        }
    }
    export {};
}
export {};
