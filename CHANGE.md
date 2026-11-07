# Changes

## 2026-11-06 - expired sessions return 401

* `CustomController.UpdateUserAccess` now answers an expired session (the 30 minute idle timeout) with HTTP 401 Unauthorized instead of 200 OK. The user is still signed out first, and the body is unchanged: `Result.Error("Session has expired, user must log in.")`, same message text. Previously the 200 let clients treat the response as a normal error and stay "logged in". Every controller action that calls `UpdateUserAccess` returns its non-null result, so the 401 reaches the client from all of them. The "Unable to find User" result is unchanged and still a 200.
* **Consumer-facing:** a client that only reads the body still sees the same failed `Result`; a client that treats any non-2xx as a transport failure will now see a 401 for an expired session and should send the user to log in.
* `Mvc.Core` 2.3 has no `Unauthorized(object)` overload, so the result is `StatusCode(401, ...)`, an `ObjectResult` with a 401 status code.
* Pinned `SQLitePCLRaw.bundle_e_sqlite3` up from 2.1.12 to 2.1.13. `Microsoft.EntityFrameworkCore.Sqlite` 8.0.33, which the `8.0.*` range now resolves, requires 2.1.13 or later, and the 2.1.12 pin failed restore with NU1605 (package downgrade, treated as an error).
* Tests cover `UpdateUserAccess` against a real Identity stack on an in-memory Sqlite `ServerContext`: an expired session returns 401 with the message and clears the Identity cookie, an active session returns null and extends `SessionExpires`, and an unknown user still gets the 200 error.

## 2026-07-28 - compiler warnings and deprecated dependencies

`MailHelper` produced three `CS8604` possible-null-reference warnings on a
full rebuild. Fixed by validating rather than suppressing:

* `SendMessage` now throws `InvalidOperationException` if the configured
  `HostName` is missing, before attempting `client.Connect`. There was
  previously no check at all on the host name.
* `AuthenticateClient` already validated `UserName`, but the compiler could
  not carry that through to `AuthenticateWithBasicCredentials` and2
  `AuthenticateWithOAuth2`. Those two private methods now take the validated
  user name as a parameter. No behaviour change - the same guard runs, in
  the same place.

The build is now warning-free. No `#pragma` suppressions and no
null-forgiving operators were used.

`dotnet list package --deprecated --include-transitive` reported twelve
deprecated packages. All but one arrived through `Microsoft.Data.SqlClient`
5.1.x, which `Microsoft.EntityFrameworkCore.SqlServer` pulls in:
`Azure.Identity` 1.11.4, `Microsoft.Identity.Client` 4.61.3 and its Msal
extensions, the `Microsoft.IdentityModel` / `System.IdentityModel.Tokens.Jwt`
6.35.0 family, and `System.Text.Json` 4.7.2.

* Pinned `Microsoft.Data.SqlClient` to 6.1.x. It carries `Azure.Identity`
  1.17.x, `Microsoft.Identity.Client` 4.84.x and `Microsoft.IdentityModel`
  7.7.x, none deprecated. `System.Text.Json` drops out of the graph
  entirely - it is in-box on net8.0. Note that 5.2.x does not help here: it
  still pins the same deprecated `Azure.Identity` and `IdentityModel`
  versions, so 6.1.x was the lowest version that clears them.
* **Consumer-facing:** `Microsoft.Data.SqlClient` 6.1.x is now a public
  dependency of the package, so consumers move up with it. It is verified
  working against the EF Core 8 SqlServer provider - query and migration
  SQL generation and connection binding were all exercised - but it has not
  been tested against a live SQL Server instance.
* Pinned `System.Collections.Immutable` to 8.0.x with `PrivateAssets`, the
  last deprecated entry. It reaches the project only through
  `Microsoft.CodeAnalysis` 4.5.0 under the build-time EF Core Tools/Design
  packages, so the pin is deliberately kept out of the package's public
  dependency list.

`dotnet list package --deprecated --include-transitive` and
`--vulnerable --include-transitive` both now report nothing for either
project, and the build reports no warnings.

## 2026-07-24 - email attachments

* Added `EmailAttachment` (file name, content type, bytes) and an `Attachments`
  collection on `EmailMessage`, carried through to Azure Communication Services.
  Messages without attachments are unaffected.
* Attachments are validated alongside the rest of the message: a missing file
  name or content type throws rather than reaching Azure.

## 2026-07-24 - dependency advisories

Applies the dependency changes the 1.2.0 entry below described but never made -
that commit edited only code, leaving every `PackageReference` untouched, so
the advisories it claimed to resolve shipped in every package since.

* Dropped the `AutoMapper` reference. No library code has used it since
  `UserRegistrationModelExtensions` replaced the profile; the reference alone
  was carrying CVE-2026-32933 into consumers.
* Bumped `MailKit` 3.4.1 to 4.17.x, clearing the MailKit advisory plus the
  transitive `MimeKit` and `System.Security.Cryptography.Pkcs` ones.
  `MailHelper` needed no source changes.
* Removed the unused `Microsoft.AspNetCore.Server.Kestrel` reference, source of
  the Critical `Kestrel.Core` advisory. Nothing in the library referenced it -
  a web host supplies its own server.
* Bumped `Microsoft.AspNetCore.Identity` and `Microsoft.AspNetCore.Mvc.Core`
  from 2.2.x to 2.3.x.
* Pinned `SQLitePCLRaw.bundle_e_sqlite3` to 2.1.12 and
  `System.Security.Cryptography.Xml` to 8.0.x, above the vulnerable versions
  reached transitively through EF Core Sqlite and DataProtection.

`dotnet list package --vulnerable --include-transitive` now reports nothing.

`Microsoft.AspNetCore.Server.IISIntegration` is deliberately left in place. It
carries no advisory and is not in the Kestrel dependency chain, so removing it
is housekeeping rather than a fix.

## 2026-07-20 - 1.2.0

* **Breaking:** Removed the public `Druware.Server.AutomapperProfile` type.
  Consumers registering it via `cfg.AddProfile<AutomapperProfile>()` must drop
  that registration and use the replacement below.
* Removed the `AutoMapper` package dependency entirely.
* Added `UserRegistrationModelExtensions` with `ToUser()` and `ApplyTo(user)`,
  replacing the former `UserRegistrationModel` to `User` map. Behaviour is
  unchanged: `FirstName`, `LastName`, and `Email` carry across, and `UserName`
  is taken from `Email`.
* Resolved all known NuGet vulnerabilities:
  * Bumped `MailKit` 3.4.1 to 4.17.x, clearing the MailKit/MimeKit advisories
    and their transitive `System.Security.Cryptography.Pkcs` exposure.
  * Bumped `Microsoft.AspNetCore.Identity` and `Microsoft.AspNetCore.Mvc.Core`
    from 2.2.x to 2.3.x.
  * Removed the unused `Microsoft.AspNetCore.Server.Kestrel` (source of the
    Critical Kestrel.Core advisory) and `Microsoft.AspNetCore.Server.IISIntegration`
    references; neither is referenced by the library.
  * Pinned `SQLitePCLRaw.bundle_e_sqlite3` (2.1.x) and
    `System.Security.Cryptography.Xml` (8.0.4) above their vulnerable
    transitive versions.

## 2026-07-20 - 1.1.12

* Added a reusable `IEmailSender` abstraction and Azure Communication Services
  Email transport with To, Cc, Bcc, Reply-To, HTML, and plain-text support.
* Added dependency-injection registration and environment-first Azure
  connection-string resolution.
* Added structured Azure delivery failures, cancellation support, and unit
  coverage through a testable Azure client adapter.
* Preserved the legacy MailKit-based `MailHelper` for existing consumers.

## 2026-01-02

* Removed .nuspec
* Updated build scripts to push to both nuget.org and satori.

## 2025/09/18

* Updated to support .net8 and .net9

## 2024-10-28

* Added full SqlLit support to the library in order to support fully
  self-contained containers.

## 2023-02-11

* Extended User to store both a session expiration and registration date
* Moved Tag from Content to Server
* Added license headers to all relevant files
* Added an Access Log to the Server space to enable access reporting and auditing
