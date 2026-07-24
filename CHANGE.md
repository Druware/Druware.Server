# Changes

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


