namespace Druware.Server.Email;

/// <summary>
/// A file carried with an email message.
/// </summary>
/// <param name="FileName">
/// The name the recipient sees, including its extension (e.g. "onboarding.pdf").
/// </param>
/// <param name="ContentType">
/// The MIME type of <paramref name="Content"/> (e.g. "application/pdf").
/// </param>
/// <param name="Content">The raw bytes of the file.</param>
public sealed record EmailAttachment(
    string FileName,
    string ContentType,
    ReadOnlyMemory<byte> Content);
