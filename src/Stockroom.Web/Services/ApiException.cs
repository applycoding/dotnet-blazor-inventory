namespace Stockroom.Web.Services;

public sealed class ApiException : Exception
{
    public ApiException(int statusCode, string message)
        : base(message)
    {
        StatusCode = statusCode;
    }

    public int StatusCode { get; }
}
