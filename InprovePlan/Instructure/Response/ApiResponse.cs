using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instructure.Response
{
    public record ApiError(string Code,string Message,IEnumerable<string>? Details = null);

    public class ApiResponse<T>()
    {
        public bool Success { get; init; }
        public T? Data { get; init; }
        public ApiError? Error { get; init; }
        public string TraceId { get; init; } = string.Empty;

        public static ApiResponse<T> Ok(T? data, string traceId) => new()
        {
            Success = true,
            Data = data,
            TraceId = traceId
        };

        public static ApiResponse<T> Fail(string code, string message, string traceId, IEnumerable<string>? details = null) => new()
        {
            Success = false,
            Error = new ApiError(code, message, details),
            TraceId = traceId
        };
    }
}
