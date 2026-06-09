using InprovePlan.Domain.Entities;
using InprovePlan.UserCase.AppUsers.Commands;
using InprovePlan.UserCase.AppUsers.Queries;
using Instructure.Paging;
using Instructure.Sorting;
using Microsoft.AspNetCore.Mvc;

namespace InprovePlan.Controllers
{
    /// <summary>
    /// 用户业务接口
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AppUserController() : BaseController
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="UserName"></param>
        /// <param name="Password"></param>
        /// <param name="Sex"></param>
        /// <param name="PhoneNumber"></param>
        /// <param name="Email"></param>
        public sealed record CreateAppUserRequest(
            string UserName,
            string Password,
            AppUserSex Sex,
            string PhoneNumber,
            string Email);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="UserName"></param>
        /// <param name="Email"></param>
        /// <param name="PhoneNumber"></param>
        /// <param name="Sex"></param>
        /// <param name="UserStatus"></param>
        public sealed record UpdateAppUserRequest(
            string UserName,
            string Email,
            string? PhoneNumber,
            AppUserSex Sex,
            AppUserStatus UserStatus);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="OldPassword"></param>
        /// <param name="NewPassword"></param>
        /// <param name="ConfirmPassword"></param>
        public sealed record ChangeAppUserPasswordRequest(
            string OldPassword,
            string NewPassword,
            string ConfirmPassword);

        /// <summary>
        /// 用户注册
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [HttpPost()]
        public async Task<IActionResult> Create([FromBody] CreateAppUserRequest request,
        CancellationToken cancellationToken)
        {
            var result = await Sender.Send(
             new CreateAppUserCommand(
                 request.UserName,
                 request.Password,
                 request.Sex,
                 request.PhoneNumber,
                 request.Email),
             cancellationToken);

            return ReturnResult(result);
        }

        /// <summary>
        /// 修改用户基础信息。
        /// </summary>
        [HttpPut("{id:long}")]
        public async Task<IActionResult> Update(
            long id,
            [FromBody] UpdateAppUserRequest request,
            CancellationToken cancellationToken)
        {
            var result = await Sender.Send(
                new UpdateAppUserCommand(
                    id,
                    request.UserName,
                    request.Email,
                    request.PhoneNumber,
                    request.Sex,
                    request.UserStatus),
                cancellationToken);

            return ReturnResult(result);
        }

        /// <summary>
        /// 修改用户密码。
        /// </summary>
        [HttpPut("{id:long}/password")]
        public async Task<IActionResult> ChangePassword(
            long id,
            [FromBody] ChangeAppUserPasswordRequest request,
            CancellationToken cancellationToken)
        {
            var result = await Sender.Send(
                new ChangeAppUserPasswordCommand(
                    id,
                    request.OldPassword,
                    request.NewPassword,
                    request.ConfirmPassword),
                cancellationToken);

            return ReturnResult(result);
        }

        /// <summary>
        /// 删除用户。
        /// </summary>
        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(
            long id,
            CancellationToken cancellationToken)
        {
            var result = await Sender.Send(
                new DeleteAppUserCommand(id),
                cancellationToken);

            return ReturnResult(result);
        }

        /// <summary>
        /// 查询单个用户。
        /// </summary>
        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(
            long id,
            CancellationToken cancellationToken)
        {
            var result = await Sender.Send(
                new GetAppUserByIdQuery(id),
                cancellationToken);

            return ReturnResult(result);
        }

        /// <summary>
        /// 分页查询用户。
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetPaged(
            [FromQuery] int pageIndex = Pagination.DefaultPageIndex,
            [FromQuery] int pageSize = Pagination.DefaultPageSize,
            [FromQuery] string? sortBy = null,
            [FromQuery] string? sortDirection = null,
            [FromQuery] string? keyword = null,
            [FromQuery] AppUserStatus? status = null,
            [FromQuery] AppUserSex? sex = null,
            [FromQuery] bool includeDeleted = false,
            CancellationToken cancellationToken = default)
        {
            var result = await Sender.Send(
                new GetAppUsersPagedQuery(
                    new Pagination
                    {
                        PageIndex = pageIndex,
                        PageSize = pageSize
                    },
                    new SortQuery
                    {
                        SortBy = sortBy,
                        SortDirection = sortDirection
                    },
                    keyword,
                    status,
                    sex,
                    includeDeleted),
                cancellationToken);

            return ReturnResult(result);
        }
    }
}
