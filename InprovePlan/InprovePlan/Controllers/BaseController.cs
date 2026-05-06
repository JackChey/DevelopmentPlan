using Microsoft.AspNetCore.Mvc;
using Instructure.IResult;

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
        public IActionResult ReturnResult(Instructure.IResult.IResult result)
        {
            switch (result.Status)
            {
                case ResultStatus.Ok:
                    {
                        var value = result.GetValue();
                        return value is null ? NoContent() : Ok(value);
                    }

                case ResultStatus.Error:
                    return result.Errors is null ? BadRequest() : BadRequest(new { errors = result.Errors, });

                case ResultStatus.Forbidden:
                    return StatusCode(403);

                case ResultStatus.NotFound:
                    return result.Errors is null ? NotFound() : NotFound(new { errors = result.Errors });

                case ResultStatus.Unauthorized:
                    return Unauthorized();

                case ResultStatus.Invalid:
                    return result.Errors is null ? BadRequest() : BadRequest(new { errors = result.Errors, });

                default:
                    return BadRequest();


            }
        }
    }
}
