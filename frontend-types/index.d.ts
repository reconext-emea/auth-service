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
