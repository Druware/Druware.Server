# Changes

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


