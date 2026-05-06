using InprovePlan.Exceptions;
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
    public class UserController: BaseController
    {
        private List<AppUser> _users = new()
        {
            new AppUser()
            {
                UserId = 100001,
                UserName = "Jack",
                PassWord = "123456",
                Address = "China",
            },
            new AppUser()
            {
                UserId = 100002,
                UserName = "Json",
                PassWord = "123456",
                Address = "Singaple",
            },
            new AppUser()
            {
                UserId = 100003,
                UserName = "Mary",
                PassWord = "123456",
                Address = "Jepan",
            },
        };

        /// <summary>
        /// 
        /// </summary>
        /// <param name="userid"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        [HttpGet()]
        public async Task<IActionResult> Get(int userid,string password)
        {
            if (userid <= 0)
            {
                return ReturnResult(new Result(ResultStatus.Invalid)
                {
                    Errors = new List<string>() { "用户ID未传或传值失败" },
                });
            }

            if(string.IsNullOrEmpty(password))
            {
                throw new ValidationException(new Dictionary<string, string[]>() { { "验证不通过", new string[] { "用户密码未传或传值失败" } } });

                //return ReturnResult(new Result(ResultStatus.Invalid)
                //{
                //    Errors = new List<string>() { "用户密码未传或传值失败" },
                //});
            }

            if (password.Equals("123456789"))
            {
                throw new ValidationException(new Dictionary<string, string[]>() { { "验证不通过", new string[] { "用户密码未传或传值失败" } } });

                //return ReturnResult(new Result(ResultStatus.Invalid)
                //{
                //    Errors = new List<string>() { "用户密码未传或传值失败" },
                //});
            }

            var user = _users.FirstOrDefault(u=>u.UserId.Equals(userid) && u.PassWord.Equals(password));

            if (user == null)
            {
                return ReturnResult(new Result(ResultStatus.NotFound)
                {
                    Errors = new List<string>() { "用户id或密码输入错误" },
                });
            }

             return ReturnResult(new Result<AppUser>(ResultStatus.Ok)
            {
                Value = user,
            });
        }
    }
}
