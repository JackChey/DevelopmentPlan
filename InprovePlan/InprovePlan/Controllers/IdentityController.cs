using InprovePlan.UserCase.AppUsers.Commands;
using Instructure.Interfaces;
using Instructure.Interfaces.Jwt;
using Instructure.IResult;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace InprovePlan.Controllers
{
    /// <summary>
    /// 
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class IdentityController() : BaseController
    {
       
        /// <summary>
        /// 用户登录请求信息
        /// </summary>
        /// <param name="UserName"></param>
        /// <param name="Password"></param>
        public record LoginUserRequest(string UserName, string Password);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost()]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginUserRequest request)
        {
            var result = await Sender.Send(new LoginAppUserCommand(request.UserName,request.Password));

            return ReturnResult(result);
        }
    }
}
