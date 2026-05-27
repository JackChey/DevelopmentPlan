using InprovePlan.IService.Jwt;
using Instructure.IResult;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static InprovePlan.Controllers.UserController;

namespace InprovePlan.Controllers
{
    /// <summary>
    /// 
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class IdentityController(IJwtService jwtService) : BaseController
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="userRequest"></param>
        /// <returns></returns>
        [HttpPost()]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] UserRequest userRequest)
        {
            if (userRequest.userid <= 0)
            {
                return ReturnResult(new Result(ResultStatus.Invalid, new List<string>() { "用户ID未传或传值失败" }));
            }

            if (string.IsNullOrEmpty(userRequest.password))
            {
                return ReturnResult(new Result(ResultStatus.Invalid, new List<string>() { "用户密码未传或传值失败" }));


                //throw new ValidationException(new Dictionary<string, string[]>() { { "验证不通过", new string[] { "用户密码未传或传值失败" } } });

                //return ReturnResult(new Result(ResultStatus.Invalid)
                //{
                //    Errors = new List<string>() { "用户密码未传或传值失败" },
                //});
            }

            //if (password.Equals("123456789"))
            //{
            //    throw new ValidationException(new Dictionary<string, string[]>() { { "验证不通过", new string[] { "用户密码未传或传值失败" } } });

            //    //return ReturnResult(new Result(ResultStatus.Invalid)
            //    //{
            //    //    Errors = new List<string>() { "用户密码未传或传值失败" },
            //    //});
            //}

            //var token = await jwtService.GetAccessTokenAsync(userRequest.userid, userRequest.password);
            var token =  jwtService.GetAccessToken(userRequest.userid, userRequest.password);

            if (token is null)
            {
                return ReturnResult(Result.Failure(new string[] { "获取Token失败"}));
            }

            return ReturnResult(token);
        }
    }
}
