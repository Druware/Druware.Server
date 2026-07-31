# Known Issues

The library builds, packs, and publishes, and the Azure email transport
added in 1.1.12 is the one part of it that is properly designed and
covered by tests. Everything older than that is in noticeably worse
shape. `ServerContext` - the context the library actually asks
consumers to inject - has a provider-selection bug that makes two of its
three advertised databases unusable, and it has no migrations bound to
it at all. `MailHelper` swallows every delivery failure, exposes three
`async` methods that are not asynchronous, leaks file handles, and
writes the whole SMTP conversation to standard output. The README
describes several features that do not exist. Most of what follows is a
consequence of code that predates the current package and has not been
revisited since; none of it is subtle, and most of it is cheap to fix.

Each item is marked **Verified** (read the code, the problem is there)
or **Suspected** (looks wrong, but confirmation needs a runtime check
or a consumer this repository does not contain).

## Database contexts and migrations

### `ServerContext.OnConfiguring` registers two providers at once

`Druware.Server/ServerContext.cs:73-99` - **Verified**

The switch at lines 80-93 selects a provider, and then lines 94-95 run
unconditionally:

```csharp
if (settings.ConnectionString != null)
    optionsBuilder.UseSqlServer(settings.ConnectionString);
```

For `DbContextType.PostgreSql` this means `UseNpgsql` is called and then
`UseSqlServer` is called on the same builder. EF Core permits only one
provider per context and throws when the context is first used. The
Microsoft path survives only because it calls `UseSqlServer` twice.

`DbContextType.Sqlite` is not handled by the switch at all, so it falls
to the `default` arm on lines 90-92 and throws "There is no
configuration for this DbType" - even though `OnModelCreating` at lines
148-163 does have a Sqlite arm, and the Sqlite entity configurations and
migration exist.

Fix: give the switch a `Sqlite` case calling `UseSqlite`, and delete
lines 94-95.

### No migration is bound to `ServerContext`

`Druware.Server/Migrations/` - **Verified**

The three `Migrations` folders are not empty, contrary to what the bare
`<Folder Include>` entries in the csproj suggest. Each holds a `Startup`
migration and a model snapshot. But every one of them is stamped to a
platform-specific context:

* `Migrations/Microsoft/20240102194955_Startup.Designer.cs:14` -
  `[DbContext(typeof(ServerContextMicrosoft))]`
* `Migrations/PostgreSQL/20240102200216_Startup.Designer.cs:14` -
  `[DbContext(typeof(ServerContextPostgreSql))]`
* `Migrations/Sqlite/20241028172938_Startup.Designer.cs:13` -
  `[DbContext(typeof(ServerContextSqlite))]`

`ServerContext` itself has none. That is the type `CustomController`
takes in its constructor (`CustomController.cs:16`) and the type
`UserExtensions.UpdateAccessed` requires (`Entities/User.cs:69`), so a
consumer following the library's own shape has a context it cannot
migrate. `Database.Migrate()` on a `ServerContext` finds an empty
migration set and creates nothing.

Fix: decide whether `ServerContext` or the three platform contexts are
the supported surface, and generate migrations for whichever it is.

### Migrations were generated against EF Core 6

`Druware.Server/Migrations/*/ServerContext*ModelSnapshot.cs` -
**Verified**

All six generated files carry `.HasAnnotation("ProductVersion",
"6.0.10")` while the project references EF Core `8.0.*`. The Sqlite pair
was added in October 2024 and inherited the same stamp. This does not
break anything on its own, but the snapshots no longer describe what
EF 8 would produce, so the next `dotnet ef migrations add` will emit a
diff containing unrelated provider-level churn.

Fix: regenerate the snapshots against EF Core 8 in a commit of their
own, before the next real schema change.

### `ServerContext` has a parameterless constructor that cannot work

`Druware.Server/ServerContext.cs:46` and `:111` - **Verified**

`ServerContext()` leaves `_configuration` null. `OnModelCreating` then
does `new AppSettings(_configuration!)` with no guard, and
`AppSettings`'s constructor immediately calls
`configuration.GetSection("API")` (`AppSettings.cs:103`), so the null
surfaces as a `NullReferenceException` on first model build. The
null-forgiving `!` hides this from the compiler rather than handling it.

Fix: remove the parameterless constructor, or throw a clear
`InvalidOperationException` from it.

### An unrecognised `ConnectionType` silently selects SQL Server

`Druware.Server/AppSettings.cs:108-113` - **Verified**

`DbType` is assigned only when `dbTypeString` matches one of MSSQL,
POSTGRESQL, PGSQL or SQLITE. A missing or misspelled value leaves the
field at its default, which is `DbContextType.Microsoft` (value 0 in the
enum at `AppSettings.cs:19-24`). The application then tries to open a
PostgreSQL connection string with the SQL Server provider and fails with
a connection error rather than a configuration error.

Fix: throw when `ConnectionType` is present but unrecognised.

## Security

### SMTP credentials are protected by a key and IV that are public

`Druware.Server/MailHelper.cs:213-215` and `:227-229` - **Verified**

Both call sites are of the form:

```csharp
_configuration.Password.Decrypt(_assemblyName, "4D584868CCA84221823D53D80DB30FCB");
```

`Druware.Extensions.StringLightEncryptionExtensions.Decrypt(cipherText,
key, iv)` uses the second argument as the AES **key** and the third as
the **IV**. Three things follow:

* The key is the consumer's assembly name, which is not a secret - it
  is readable from the shipped DLL.
* Assembly names are shorter than 32 characters, and the extension pads
  the key with its own constant `Len` to reach AES-256 key length. That
  constant is `"4D584868CCA84221823D53D80DB30FCB"` - the identical
  string hardcoded here. So most of the key material is published in
  the source of a public NuGet package.
* The IV is a fixed literal, reused for every value ever encrypted, so
  identical passwords produce identical ciphertext.

`Druware.Extensions/StringLightEncryptionExtensions.cs:8-10` carries its
own warning that it should not be used "for anything that is truly
intended to be secure". This is obfuscation, not encryption, and the
stored SMTP password should be treated as plaintext in any threat model.

Fix: treat `API:Mail:SMTP:Password` as a secret supplied by the
deployment environment - the same pattern the Azure sender already uses
for its connection string (`Email/AzureCommunicationEmailSender.cs:110-113`)
- and drop the `Decrypt` calls.

### Every SMTP session is written to standard output

`Druware.Server/MailHelper.cs:177` - **Verified**

```csharp
using var client = new SmtpClient(new ProtocolLogger(Console.OpenStandardOutput()));
```

There is no way for a consumer to turn this off. Every send writes the
full protocol exchange - envelope sender, every recipient address, the
server's responses - to the host process's stdout, which in a container
deployment means the aggregated application log.

To be accurate about what is *not* leaked: MailKit 4.17's
`ProtocolLogger(Stream, bool)` constructor sets `RedactSecrets = true`,
so the AUTH exchange is masked. Credentials do not reach the log. The
exposure is recipient addresses and delivery metadata, not passwords.

Fix: accept an optional `IProtocolLogger` (or a plain "enable protocol
logging" flag) and default it to none.

## Correctness

### `IsSessionExpired` returns false when no expiry has been set

`Druware.Server/Entities/User.cs:90-91` - **Verified**

```csharp
public static bool IsSessionExpired(this User user) =>
    user.SessionExpires < DateTime.UtcNow;
```

`SessionExpires` is `DateTime?`. A lifted comparison where either
operand is null evaluates to `false`, so a user whose `SessionExpires`
has never been written is reported as having a live session. Nothing in
the library sets `SessionExpires` at registration - the only assignment
is in `UpdateAccessed` (`Entities/User.cs:86`) - so this is the state
every newly created user starts in.

`CustomController.UpdateUserAccess` (`CustomController.cs:52-57`) is the
consumer of this, and it is the check that decides whether to sign a
user out. A null therefore fails open.

Fix: `user.SessionExpires == null || user.SessionExpires < DateTime.UtcNow`,
and set `SessionExpires` when a user is created.

### `MailHelper` reports send failures only to the console

`Druware.Server/MailHelper.cs:68-72`, `:98-102`, `:154-157` -
**Verified**

All three public send methods wrap their body in `try`/`catch
(Exception e)` and respond with `Console.WriteLine`. The methods return
`void` (or a `Task` that always completes successfully), so a caller
cannot distinguish a delivered message from a refused connection, a
rejected recipient, or an authentication failure.

The same methods also open with `if (_configuration == null) return;`
(`:58`, `:80`, `:110`), which silently does nothing when mail is
unconfigured. `_configuration` is assigned from a non-nullable
constructor parameter at `:34-38`, so this branch is unreachable in
practice, but the declared type at `:13` is `MailSettings?` and the
guards were written as though it were reachable.

Fix: let exceptions propagate, or return a result type. The Azure
transport already models this correctly with `EmailSendResult`; the two
senders should agree.

### The three `SendAsync` methods are not asynchronous

`Druware.Server/MailHelper.cs:40-50` - **Verified**

```csharp
public async Task SendAsync(string to, ... string body) =>
    Send(to, from, replyTo, subject, body);
```

`SendAsync`, `SendHtmlAsync` and `SendWithAttachmentsAsync` are all
`async` methods with no `await`. Each runs its synchronous counterpart
to completion on the calling thread - connect, TLS handshake,
authenticate, send, disconnect - and only then returns an
already-completed `Task`. On a web request that blocks a thread pool
thread for the whole SMTP round trip while presenting an API that
suggests it does not. The author's own note at `:53` records the
intent: "Move this to Async and then wrap the async call from the sync
side."

MailKit exposes `ConnectAsync`, `AuthenticateAsync`, `SendAsync` and
`DisconnectAsync`, so the inversion the TODO describes is
straightforward.

Separately, an `async` method with no `await` normally raises CS1998,
which sits oddly with the "build is now warning-free" claim in
`CHANGE.md`. Worth re-checking with `dotnet build -c Release
--no-incremental` - **Suspected**, and cosmetic either way next to the
threading behaviour.

### Attachment file handles are never closed

`Druware.Server/MailHelper.cs:131-149` - **Verified**

`File.OpenRead(path)` at `:139` hands a live `FileStream` to
`MimeContent`. `MimeMessage` implements `IDisposable` in MimeKit 4.x and
disposing it disposes the part content, but no `MimeMessage` created in
this file is ever disposed - `CreateMessage` at `:160-168` returns one
and `SendMessage` at `:170-184` drops it. Every attachment therefore
holds an open file handle until the finalizer runs.

`File.Exists(path)` followed by `File.OpenRead(path)` at `:135-139` is
also a check-then-act on the filesystem, and a path that fails
`File.Exists` is skipped silently - the message goes out without the
attachment and the caller is not told.

Fix: `using var message = CreateMessage(...)` in each send method, and
throw (or report) when a requested attachment path does not exist.

### The protocol logger closes standard output

`Druware.Server/MailHelper.cs:177` - **Verified chain, Suspected
consequence**

`using var client = new SmtpClient(new ProtocolLogger(...))`. The chain
is confirmed by reading the MailKit 4.17 assembly:
`MailService.Dispose(bool)` disposes its `IProtocolLogger`, and
`ProtocolLogger.Dispose(bool)` disposes the underlying stream unless
`leaveOpen` was set - and the `ProtocolLogger(Stream)` overload defaults
`leaveOpen` to `false`. So each call to `SendMessage` opens a fresh
standard-output stream and disposes it on exit.

Whether that actually closes the process's stdout handle depends on
what `Console.OpenStandardOutput()` returns on the target platform;
.NET's Unix implementation duplicates the descriptor, the Windows one
does not own the handle. Confirming it means running two sends in a row
in a console host on the target OS and checking that `Console.WriteLine`
still works after the first. Passing `leaveOpen: true` removes the
question entirely.

### `LoggingSignInManager` blocks on a lookup it then discards

`Druware.Server/LoggingSigninManager.cs:34-44` - **Verified**

```csharp
var user = UserManager.FindByEmailAsync(userName).Result;
```

`user` is never read. The only code that used it is commented out at
`:38-41`. So every password sign-in pays for an extra database round
trip, taken by blocking on `.Result` inside a method that returns a
`Task` - the classic sync-over-async pattern that deadlocks under a
synchronization context and starves the thread pool under load.

The class is also declared `LoggingSignInManager<TUser> where TUser :
class` at `:12` but derives from `SignInManager<User>` and uses `User`
throughout, so the type parameter is unused and misleading. `_userManager`,
`_db` and `_contextAccessor` (`:14-16`) are assigned and never read.

Fix: delete the discarded lookup and the commented-out block, or
implement the enabled-user check it was meant to perform. Drop the
unused type parameter.

### `UpdateAccessed` can write a null into a required column

`Druware.Server/Entities/User.cs:77` - **Verified**

```csharp
access.Who = user.UserName;
```

`IdentityUser.UserName` is `string?` and `Access.Who` is `string?`, but
all three `AccessConfiguration` classes mark the column
`IsRequired()` (for example
`Entities/Configuration/PostgreSql/AccessConfiguration.cs:46-49`). A
user with a null `UserName` produces a `DbUpdateException` on
`SaveChangesAsync`.

`CustomController.LogRequest` gets this right at `CustomController.cs:35`
with `user?.UserName ?? "anonymous"`. The two paths should agree.

### `StripHtmlTags` is not a usable HTML-to-text conversion

`Druware.Server/MailHelper.cs:235-239` - **Verified**

```csharp
return System.Text.RegularExpressions.Regex.Replace(html, "<[^>]*>", "");
```

This feeds the plain-text alternative of every HTML message sent through
`SendHtml` (`:88`) and `SendWithAttachments` (`:121`) when the caller
does not supply one. Concretely, on real HTML it:

* leaves entities undecoded, so `&amp;`, `&nbsp;` and `&lt;` appear
  literally in the plain-text part;
* strips `<script>` and `<style>` tags but keeps their contents, so
  CSS and JavaScript become visible body text;
* inserts nothing in place of block elements, so `<p>a</p><p>b</p>`
  becomes `ab` and every `<br>` disappears - the output is one
  unbroken run-on line with all structure lost;
* mis-parses `>` inside attribute values: `<a title="a>b">x</a>`
  yields `b">x`.

A degraded plain-text alternative matters more than it looks, because
spam filters compare the two parts of a `multipart/alternative` message.

Fix: require callers to supply `plainTextBody`, or use a real converter.
The simplest honest change is to drop the parameter default and make
the plain-text body mandatory.

### `OnSendComplete` is dead code with an inverted guard

`Druware.Server/MailHelper.cs:16-32` - **Verified**

Nothing references this method - it is the only match for its own name
in the repository. It is a leftover from an `SmtpClient.SendAsync`
event-based API that the MailKit rewrite removed. It also dereferences
`e!.UserState!` at `:19`, one line before checking `if (e?.UserState ==
null) return;` at `:22`, so the guard runs after the throw it was meant
to prevent.

Fix: delete it.

### Two unused private members

`Druware.Server/AppSettings.cs:145-149` - **Verified**

`DecodeString` is private and never called. The `MailType.Pop3`,
`Imap` and `Web` enum members (`AppSettings.cs:5-11`) are likewise
selectable through `MailSettings`'s constructor but have no
implementation behind them - `MailHelper` only ever speaks SMTP. Both
are flagged here rather than removed, per the note in the guidelines
about pre-existing dead code.

## Dependencies

### The library pulls ASP.NET Core 2.3.x shim packages into a net8.0 build

`Druware.Server/Druware.Server.csproj:38-39` - **Verified fact,
Suspected consequence**

`Microsoft.AspNetCore.Identity` `2.3.*` and
`Microsoft.AspNetCore.Mvc.Core` `2.3.*` drag in the whole ASP.NET Core
2.x family. Reading the restored graph in
`Druware.Server/obj/project.assets.json` confirms what resolves:

```
Microsoft.AspNetCore.Http.Abstractions/2.3.9
Microsoft.AspNetCore.Mvc.Core/2.3.11
Microsoft.AspNetCore.Routing/2.3.10
Microsoft.AspNetCore.Identity/2.3.11
Microsoft.AspNetCore.DataProtection/2.3.0
```

sitting alongside `Microsoft.AspNetCore.Identity.EntityFrameworkCore/8.0.29`.
These 2.3.x packages are the `netstandard2.0` shims that ASP.NET Core
3.0 replaced with the `Microsoft.AspNetCore.App` shared framework. A
net8.0 class library that needs these types is supposed to use
`<FrameworkReference Include="Microsoft.AspNetCore.App" />`.

One consequence is verified and visible in this repository: those 2.3.x
assemblies carry no nullable annotations at all (the
`Microsoft.AspNetCore.Http.Abstractions` 2.3.9 assembly contains zero
`NullableContextAttribute` entries), so `<Nullable>enable</Nullable>`
produces no diagnostics anywhere ASP.NET Core types are touched. That is
why `CustomController.cs:33` and `:65` can write

```csharp
string? where = HttpContext.Connection.RemoteIpAddress.ToString();
```

with no warning, even though `RemoteIpAddress` is `IPAddress?` in
ASP.NET Core 8 and is null for connections over a Unix socket or an
in-memory test server. Under a `FrameworkReference` that line is a
CS8602 and a latent `NullReferenceException`.

The other consequence - what happens when a real net8.0 web host has
both the shared framework and these packages in its graph - cannot be
checked from this repository. Confirming it needs a consumer web app
built against the package.

Fix: replace both `PackageReference`s with `<FrameworkReference
Include="Microsoft.AspNetCore.App" />` and expect a batch of previously
suppressed nullability warnings to appear.

### `Microsoft.Data.SqlClient` 6.1.x sits a major version above the EF 8 provider

`Druware.Server/Druware.Server.csproj:70` - **Watch item, not a defect**

The pin was made deliberately to clear the deprecated-package report and
is documented in `CHANGE.md`. Provider service build, query and
migration SQL generation, and connection binding were all exercised
against it. The residual risk is that the driver is a major version
ahead of the provider that was tested against 5.1.x, and nothing has
been run against a live SQL Server instance. Keep it, but the first
integration test against a real SQL Server should be treated as
gating.

### `System.Collections.Immutable` is pinned for a build-time report only

`Druware.Server/Druware.Server.csproj:71-73` - **Watch item**

The `8.0.*` pin with `PrivateAssets` exists to silence a deprecation
report on a package that reaches the project only through
`Microsoft.CodeAnalysis` 4.5.0 under the EF Core Tools/Design
build-time references. It has no effect on the shipped package. It can
be dropped whenever the EF Core tooling moves off that Roslyn version.

## Test coverage

### All 16 tests exercise one class

`UnitTests/AzureCommunicationEmailSenderTests.cs` - **Verified**

The test project holds exactly one fixture. Its 16 cases cover
`AzureCommunicationEmailSender` well - message mapping, attachments,
structured Azure failures, cancellation, validation, connection-string
and sender-address resolution, and DI registration.

Nothing else in the library has a test. The untested surface includes
every item above:

* `MailHelper` - no coverage at all, including `StripHtmlTags`, which
  is pure and trivially testable.
* `ServerContext.OnConfiguring` - the double-provider bug would be
  caught by a single test asserting the provider chosen for each
  `DbContextType`.
* `AppSettings` - configuration parsing, the environment and
  command-line connection-string precedence at `AppSettings.cs:118-124`,
  and the silent default for an unrecognised `ConnectionType`.
* `UserExtensions.IsSessionExpired` - three cases (null, past, future)
  would have caught the null-fails-open bug.
* `UserRegistrationModelExtensions.ToUser` / `ApplyTo` - the
  replacement for the removed AutoMapper profile, added in 1.2.0 with
  no test to confirm the mapping is equivalent.
* `CustomController`, `LoggingSignInManager`,
  `DbContextExtensions.TagWithSource`, `Tag.ByNameOrId`.

`AppSettings`, `UserExtensions` and the mapping extensions need no
database or web host and are the obvious place to start.

### The test project's tooling is a release behind

`UnitTests/UnitTests.csproj:13-19` - **Verified**

NUnit 3.14.0, Microsoft.NET.Test.Sdk 17.8.0 and coverlet 6.0.0. Not
broken, and not urgent, but `coverlet.collector` is referenced while
nothing in `publish.yml` collects or reports coverage, so it is
currently inert.

## Build and packaging

### The solution has a project configuration block for a project it does not contain

`Druware.Server.sln:29-32` - **Verified**

```
{8B80BB58-1B7B-4745-88B7-F6851513EDEF}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
{8B80BB58-1B7B-4745-88B7-F6851513EDEF}.Debug|Any CPU.Build.0 = Debug|Any CPU
{8B80BB58-1B7B-4745-88B7-F6851513EDEF}.Release|Any CPU.ActiveCfg = Release|Any CPU
{8B80BB58-1B7B-4745-88B7-F6851513EDEF}.Release|Any CPU.Build.0 = Release|Any CPU
```

That GUID appears in no `Project(...)` declaration. It is dead
configuration left behind when a project was removed. Harmless to
MSBuild, but it confuses IDEs and anything that parses the solution.

### There is a second, stale solution file inside the project folder

`Druware.Server/Druware.Server.sln` - **Verified**

It is tracked in git, contains only the library project, and refers to
it by GUID `{053C8171-C8F0-4F5B-9BEE-C5A3C5830CC3}` - a different GUID
from the `{66DF1190-FDF1-4500-8432-7B67C61197DD}` the root solution
uses for the same csproj. Opening the inner one gives a build without
the test project.

Fix: delete it.

### The removed `.nuspec` is still in the repository

`Druware.Server/Druware.Server.nuspec` - **Verified**

`CHANGE.md`'s 2026-01-02 entry records "Removed .nuspec", but the file
is still tracked. It declares `<version>11.1.4</version>` against a
package currently at 1.1.17, and release notes about a MailHelper fix
from an unknown release. Nothing consumes it - the csproj carries all
the package metadata - so it is purely a source of confusion.

`Druware.Server/Notes.md` documents the matching workflow (`nuget pack`
against the nuspec, `nuget add` to a network share) which was replaced
by `publish.yml`.

### `RepositoryUrl` is misspelled

`Druware.Server/Druware.Server.csproj:19` - **Verified**

`https://github.com/Druware/Druware.SErver` - capital E. This is
embedded in every published `.nuspec` and drives the "Source repository"
link on the nuget.org listing.

### Every push to main publishes a new patch version

`.github/workflows/publish.yml:3-5` - **Verified**

The workflow triggers on any push to `main` with no `paths` filter. A
commit that only touches `README.md`, `CHANGE.md`, `LICENSE.txt` or the
workflow itself still bumps the patch number, builds, packs, pushes to
both nuget.org and the Satori feed, and commits a `Release vX.Y.Z [skip
ci]` back with a tag. The result is a stream of published versions whose
assemblies are byte-identical to their predecessor, and a version number
that no longer tells a consumer whether upgrading gets them anything.

The `[skip ci]` guard at `:30-32` prevents the bot's own commit from
looping, but nothing stops a human docs commit from shipping a package.

Fix: add `paths-ignore` for the docs and workflow files, or gate the
publish on the packed assembly differing from the last published one.

### The csproj declares five folders that do not need declaring

`Druware.Server/Druware.Server.csproj:81-87` - **Verified, cosmetic**

`Migrations\Microsoft\`, `Migrations\PostgreSQL\`, `Migrations\Sqlite\`,
`Models\` and `pub\` are all listed as bare `<Folder Include>` items.
Every one of them contains files and is picked up by the SDK's default
globs regardless, so the entries do nothing. They read as if the folders
were empty, which is what an IDE adds them for. `pub\` in particular is
a local package output directory whose contents are already excluded by
the `*.nupkg` rule in `.gitignore`.

## Documentation

### The README describes features that do not exist

`README.md` - **Verified**

* Lines 11-12: "For convenience there is a MigrationManager built into
  the library that can be overridden, or called from the parent." There
  is no `MigrationManager` type anywhere in the repository.
* Lines 8-10: "the Models and Controllers that are included in this
  library will be published by default in any API or Web application".
  The library contains one abstract-ish base class, `CustomController`,
  with no `[Route]`, no `[ApiController]` and no action methods.
  Nothing is published by including the package.
* Lines 14-17: "the migration should, in theory at least, detect the
  connection type, and select the right migration based upon that". The
  detection code is the switch in `ServerContext.OnConfiguring`
  documented above, which does not work for PostgreSQL or Sqlite.
* Line 48: "See Changelog.md for detailed history". The file is
  `CHANGE.md`. `Druware.Server/Docs/index.md:21` makes the same
  reference as `CHANGELOG.md`.
* Lines 40-44: the Dependencies section lists three packages. The
  csproj declares eighteen, including MailKit, EF Core and three
  database providers.

### Copyright and licence headers disagree with each other

**Verified, cosmetic**

* `README.md:55-57` - "Copyright 2019-2024 by: Andy 'Dru' Satori @
  Satori & Associates, Inc."
* `Druware.Server/Docs/index.md:14` - 2019-2025, Druware Software
  Designs.
* `Druware.Server/Druware.Server.csproj:22-24` - 2019-2026, Druware
  Software Designs.

Sixteen source files also open with a GPL header that still contains
the FSF template's placeholder: "Foobar is free software: you can
redistribute it..." (for example `Druware.Server/ServerContext.cs:3`).
`Entities/Tag.cs:3` is the one file where it was corrected.
