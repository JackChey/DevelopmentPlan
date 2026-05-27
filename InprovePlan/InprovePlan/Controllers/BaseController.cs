using Microsoft.AspNetCore.Mvc;
using IResult = Instructure.IResult.IResult;
using Instructure.IResult;
using InprovePlan.Helper;
using Instructure.Response;

namespace InprovePlan.Controllers
{
    /// <summary>
    /// Controller基础
    /// </summary>
    public class BaseController : ControllerBase
    {
        /// <summary>
        /// 对返回结果的二次封装
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        [NonAction]
        public IActionResult ReturnResult(IResult result)
        {
            var traceId = HttpContext.TraceIdentifier;

            if (result.Status == ResultStatus.Ok)
                return Ok(ApiResponse<object?>.Ok(null, traceId));

            var statusCode = result.Status.ToHttpStatusCode();
            var response = ApiResponse<object?>.Fail(
                result.Status.ToErrorCode(),
                "Request failed",
                traceId,
                result.Errors);

            return StatusCode(statusCode, response);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="result"></param>
        /// <returns></returns>
        [NonAction]
        public IActionResult ReturnResult<T>(IResult<T> result)
        {
            var traceId = HttpContext.TraceIdentifier;

            if (result.Status == ResultStatus.Ok)
                return Ok(ApiResponse<T?>.Ok(result.Value, traceId));

            var statusCode = result.Status.ToHttpStatusCode();
            var response = ApiResponse<T?>.Fail(
                result.Status.ToErrorCode(),
                "Request failed",
                traceId,
                result.Errors);

            return StatusCode(statusCode, response);
        }

        
    }

    
}
