using FluentValidation;
using InprovePlan.Domain.Entities;
using InprovePlan.ShareKernel.Messaging;
using Instructure.Attributes;
using Instructure.Configurations.Entities;
using Instructure.IResult;
using Instructure.Paging;
using Instructure.Repositories;
using Instructure.Sorting;
using Instructure.Sorting.SortWhitelists;
using Instructure.Specification;

namespace InprovePlan.UserCase.AppUsers.Queries;

[RequireAuthorization]
public sealed record GetAppUsersPagedQuery(
    Pagination Pagination,
    SortQuery Sort,
    string? Keyword,
    AppUserStatus? Status,
    AppUserSex? Sex,
    bool IncludeDeleted = false
) : IQuery<Result<PagedResult<AppUserDto>>>;

public sealed class GetAppUsersPagedQueryValidator
    : AbstractValidator<GetAppUsersPagedQuery>
{
    public GetAppUsersPagedQueryValidator()
    {
        RuleFor(query => query.Pagination)
            .NotNull()
            .WithMessage("分页参数不能为空。");

        RuleFor(query => query.Pagination.PageIndex)
            .GreaterThanOrEqualTo(1)
            .WithMessage("pageIndex 必须大于等于 1。")
            .When(query => query.Pagination is not null);

        RuleFor(query => query.Pagination.PageSize)
            .GreaterThanOrEqualTo(1)
            .LessThanOrEqualTo(Pagination.MaxPageSize)
            .WithMessage($"pageSize 必须在 1 到 {Pagination.MaxPageSize} 之间。")
            .When(query => query.Pagination is not null);

        RuleFor(query => query.Sort)
            .NotNull()
            .WithMessage("排序参数不能为空。");

        RuleFor(query => query.Keyword)
            .MaximumLength(DataSchemaConstants.EmailLength)
            .WithMessage($"关键字长度不能超过 {DataSchemaConstants.EmailLength} 个字符。")
            .When(query => !string.IsNullOrWhiteSpace(query.Keyword));

        RuleFor(query => query.Status)
            .IsInEnum()
            .When(query => query.Status.HasValue)
            .WithMessage("用户状态参数不合法。");

        RuleFor(query => query.Sex)
            .IsInEnum()
            .When(query => query.Sex.HasValue)
            .WithMessage("性别参数不合法。");
    }
}

public sealed class AppUsersPagedSpecification : Specification<AppUser>
{
    public AppUsersPagedSpecification(GetAppUsersPagedQuery query)
    {
        if (!query.IncludeDeleted)
        {
            AddCriteria(user => !user.IsDeleted);
        }

        if (query.Status.HasValue)
        {
            AddCriteria(user => user.UserStatus == query.Status.Value);
        }

        if (query.Sex.HasValue)
        {
            AddCriteria(user => user.Sex == query.Sex.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var keyword = query.Keyword.Trim();

            AddCriteria(user =>
                user.UserName.Contains(keyword)
                || user.Email.Contains(keyword)
                || user.PhoneNumber.Contains(keyword));
        }
    }
}

public sealed class GetAppUsersPagedQueryHandler(
    IReadRepository<AppUser> appUserRepository)
    : IQueryHandler<GetAppUsersPagedQuery, Result<PagedResult<AppUserDto>>>
{
    public async Task<Result<PagedResult<AppUserDto>>> Handle(
        GetAppUsersPagedQuery request,
        CancellationToken cancellationToken)
    {
        var sortErrors = AppUserSortWhitelist.Instance.Validate(request.Sort);

        if (sortErrors.Count > 0)
        {
            return Result<PagedResult<AppUserDto>>.Invalid(
                sortErrors.Select(error => error.Message).ToArray());
        }

        var users = await appUserRepository.PageAsync(
            new AppUsersPagedSpecification(request),
            request.Pagination,
            request.Sort,
            AppUserSortWhitelist.Instance,
            cancellationToken);

        var dtoItems = users.Items
            .Select(user => new AppUserDto(
                user.Id,
                user.UserName,
                user.Email,
                user.PhoneNumber,
                user.Sex,
                user.UserStatus))
            .ToList();

        return Result<PagedResult<AppUserDto>>.Success(
            PagedResult<AppUserDto>.Create(dtoItems, users.Total, request.Pagination));
    }
}