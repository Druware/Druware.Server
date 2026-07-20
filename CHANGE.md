# Changes

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


