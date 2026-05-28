using AutoMapper;
using InprovePlan.Exceptions;
using InprovePlan.FakeData;
using InprovePlan.ModeDto;
using Instructure.IResult;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InprovePlan.Controllers
{
    /// <summary>
    /// 用户业务接口
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class UserController(IMapper mapper,ILogger<UserController> logger) : BaseController
    {

        /// <summary>
        /// /
        /// </summary>
        /// <param name="userid"></param>
        /// <param name="password"></param>
        public record UserRequest(int userid, string password);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="userRequest"></param>
        /// <returns></returns>
        [HttpPost()]
        public IActionResult Get([FromBody] UserRequest userRequest)
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

            var user = Users._users.FirstOrDefault(u => u.UserId.Equals(userRequest.userid) && u.PassWord.Equals(userRequest.password));

            if (user == null)
            {
                return ReturnResult(new Result(ResultStatus.NotFound, new List<string>() { "用户id或密码输入错误" }));
            }

            return ReturnResult(new Result<AppUserDto>(mapper.Map<AppUserDto>(user)));
        }

        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="userName"></param>
        /// <returns></returns>
        [HttpGet()]
        [Authorize()]
        public IActionResult Update(int userId,string userName)
        {
            if (userId <= 0)
            {
                return ReturnResult(new Result(ResultStatus.Invalid, new List<string>() { "用户ID未传或传值失败" }));
            }

            if (string.IsNullOrEmpty(userName))
            {
                return ReturnResult(new Result(ResultStatus.Invalid, new List<string>() { "用户名未传或传值失败" }));
            }

            //if (userName.Equals("Nick"))
            //{
            //    // 这里为测试日志异常,后续可用下面的返回结果

            //    //throw new ValidationException(new Dictionary<string, string[]>() { { "验证不通过", new string[] { "用户名非法" } } });
            //    throw new OperationCanceledException("用户名非法");
            //    //return ReturnResult(Result.Invalid("用户名非法"));
            //}

            var user = Users._users.FirstOrDefault(u => u.UserId.Equals(userId));

            if (user == null)
            {
                logger.LogWarning(string.Format("Update user:{userid}-userName faild"), userId);

                return ReturnResult(new Result(ResultStatus.NotFound, new List<string>() { "用户id输入错误" }));
            }

            user.UserName = userName;

            return ReturnResult(Result.SeccessWithNoMsg);
        }
    }
}
