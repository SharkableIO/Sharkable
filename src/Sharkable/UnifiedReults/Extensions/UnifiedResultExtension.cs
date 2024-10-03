using System.Net;

namespace Sharkable;

public static class UnifiedResultExtension
{
    public static UnifiedResult<TResult>? AsUnifiedResult<TResult>(this TResult? data, 
        string? errors = null, 
        HttpStatusCode statusCode = HttpStatusCode.OK, 
        string? extra = null, 
        DateTimeOffset? timeStamp = null)
    {
        if (data == null)
            return null;

        var result = new UnifiedResult<TResult>(data, errors, statusCode, extra, timeStamp);

        return result;
    }
}