# SchoolWise API Endpoints

Base path: `/api/v1`. JSON over HTTP. Swagger UI is available at `/swagger` in development. Authentication uses a JWT bearer access token and rotating refresh token.

## Authentication

| Verb | Path | Access | Purpose |
|---|---|---|---|
| POST | `/auth/register` | Anonymous | Register Parent account |
| POST | `/auth/login` | Anonymous | Authenticate and issue tokens |
| POST | `/auth/refresh` | Anonymous + HttpOnly refresh cookie | Rotate refresh token and issue access token |
| POST | `/auth/revoke` | Authenticated + refresh cookie | Revoke refresh token and clear cookie |
| GET | `/auth/me` | Authenticated | Return current profile and role |

Registration never accepts a role from the client; it always creates a Parent.

## School discovery

| Verb | Path | Access | Purpose |
|---|---|---|---|
| GET | `/schools` | Anonymous | Paged catalogue with filters/sort |
| GET | `/schools/{id:guid}` | Anonymous | Approved school detail and ordered photos |
| GET | `/schools/slug/{slug}` | Anonymous | Approved school detail for canonical public URLs |
| GET | `/schools/admin` | Admin | Paged list including unapproved schools |
| GET | `/schools/admin/{id:guid}` | Admin | School detail including unapproved records |
| POST | `/schools` | Admin | Create school |
| PUT | `/schools/{id:guid}` | Admin | Replace editable school fields |
| DELETE | `/schools/{id:guid}` | Admin | Delete school and its photos |

### `GET /schools` parameters

`page` (1-based, default 1), `pageSize` (default 12, max 100), `sortBy` (`name`, `city`, `tuitionFrom`, `createdAt`), `sortDirection` (`asc`/`desc`), `keyword`, `city`, `type`, `isPrivate`, `minTuition`, `maxTuition`.

Example: `/api/v1/schools?city=Athens&type=Kindergarten&isPrivate=true&minTuition=2000&page=1&pageSize=12`

### Response envelope

Successful collections return `{ "items": [...], "page": 1, "pageSize": 12, "totalCount": 42 }`. Errors use RFC 7807 ProblemDetails with stable extensions such as `code` and `errors`. `401` means unauthenticated, `403` forbidden, `404` not found, and `400` invalid query/body.

### School list item

`id`, `name`, `slug`, `schoolType`, `city`, `address`, `isPrivate`, `tuitionFrom`, `tuitionTo`, `logoUrl`.

### School detail

All school properties plus `photos: [{ id, url, sortOrder }]`, ordered by `sortOrder`.

### Create/update body

`name`, `description`, `schoolType`, `address`, `city`, `postalCode`, `phone`, `email`, `website`, `tuitionFrom`, `tuitionTo`, `studentCapacity`, `establishedYear`, `isPrivate`, `logoUrl`, `isApproved`, `photos`.

## Authentication examples

Login returns `accessToken`, `expiresAt`, and `user`. The refresh token is set only as an HttpOnly, SameSite=Lax cookie (Secure outside Development), scoped to `/api/v1/auth`; it is never returned in JSON. `POST /auth/refresh` takes no body, consumes that cookie, and rotates it using `Set-Cookie`. `POST /auth/revoke` requires a valid access token, revokes the cookie's token, and clears the cookie. Refresh tokens are stored as SHA-256 hashes; reuse of a revoked token is rejected.

`GET /auth/me` returns `{ id, firstName, lastName, email, role, isActive, createdAt }`.

## API conventions

- UUID IDs; ISO-8601 UTC timestamps.
- Invalid model/query input returns `400` ProblemDetails.
- List filters execute in SQL; sorting is allowlisted to avoid arbitrary expressions.
- Only approved records are returned by public list/detail queries.
- School write operations require the `Admin` role.
- API versioning is URL-segment based; a new incompatible contract uses `/api/v2`.
