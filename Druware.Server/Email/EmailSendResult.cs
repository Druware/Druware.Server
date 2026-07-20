namespace Druware.Server.Email;

/// <summary>
/// Reports whether an email was accepted by the configured transport.
/// </summary>
public sealed record EmailSendResult
{
    private EmailSendResult(
        bool succeeded,
        string? operationId,
        int? status,
        string? errorCode,
        string? errorMessage)
    {
        Succeeded = succeeded;
        OperationId = operationId;
        Status = status;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public bool Succeeded { get; }
    public string? OperationId { get; }
    public int? Status { get; }
    public string? ErrorCode { get; }
    public string? ErrorMessage { get; }

    public static EmailSendResult Success(string? operationId) =>
        new(true, operationId, null, null, null);

    public static EmailSendResult Failure(
        int status,
        string? errorCode,
        string errorMessage) =>
        new(false, null, status, errorCode, errorMessage);
}
