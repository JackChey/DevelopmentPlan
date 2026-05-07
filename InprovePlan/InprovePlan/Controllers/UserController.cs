using AutoMapper;
using InprovePlan.Exceptions;
using InprovePlan.FakeData;
using InprovePlan.ModeDto;
using InprovePlan.Model;
using Instructure.IResult;
using Microsoft.AspNetCore.Mvc;

namespace InprovePlan.Controllers
{
    /// <summary>
    /// 用户业务接口
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class UserController(IMapper mapper) : BaseController
    {


        public record UserRequest(int userid, string password);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="userRequest"></param>
        /// <returns></returns>
        [HttpPost()]
        public async Task<IActionResult> Get([FromBody] UserRequest userRequest)
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
    }
}
