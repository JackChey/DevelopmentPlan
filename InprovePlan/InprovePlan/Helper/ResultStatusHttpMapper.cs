using Instructure.IResult;

namespace InprovePlan.Helper
{
    /// <summary>
    /// 
    /// </summary>
    public static class ResultStatusHttpMapper
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="status"></param>
        /// <returns></returns>
        public static int ToHttpStatusCode(this ResultStatus status) => status switch
        {
            ResultStatus.Ok => StatusCodes.Status200OK,
            ResultStatus.Invalid => StatusCodes.Status400BadRequest,
            ResultStatus.Unauthorized => StatusCodes.Status401Unauthorized,
            ResultStatus.Forbidden => StatusCodes.Status403Forbidden,
            ResultStatus.NotFound => StatusCodes.Status404NotFound,
            ResultStatus.Conflict => StatusCodes.Status409Conflict,
            ResultStatus.Error => StatusCodes.Status500InternalServerError,
            _ => StatusCodes.Status500InternalServerError
        };

        /// <summary>
        /// 
        /// </summary>
        /// <param name="status"></param>
        /// <returns></returns>
        public static string ToErrorCode(this ResultStatus status) => status switch
        {
            ResultStatus.Invalid => "invalid_request",
            ResultStatus.NotFound => "not_found",
            ResultStatus.Unauthorized => "unauthorized",
            ResultStatus.Forbidden => "forbidden",
            ResultStatus.Conflict => "conflict",
            _ => "internal_error"
        };
    }
}
