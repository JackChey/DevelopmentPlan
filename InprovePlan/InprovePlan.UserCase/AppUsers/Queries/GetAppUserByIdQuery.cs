using FluentValidation;
using InprovePlan.Domain.Entities;
using InprovePlan.ShareKernel.Messaging;
using Instructure.Attributes;
using Instructure.IResult;
using Instructure.Repositories;

namespace InprovePlan.UserCase.AppUsers.Queries;

[RequireAuthorization]
public sealed record GetAppUserByIdQuery(long Id)
    : IQuery<Result<AppUserDto>>;

public sealed class GetAppUserByIdQueryValidator
    : AbstractValidator<GetAppUserByIdQuery>
{
    public GetAppUserByIdQueryValidator()
    {
        RuleFor(query => query.Id)
            .GreaterThan(0)
            .WithMessage("用户 ID 必须大于 0。");
    }
}

public sealed class GetAppUserByIdQueryHandler(
    IReadRepository<AppUser> appUserRepository)
    : IQueryHandler<GetAppUserByIdQuery, Result<AppUserDto>>
{
    public async Task<Result<AppUserDto>> Handle(
        GetAppUserByIdQuery request,
        CancellationToken cancellationToken)
    {
        var appUser = await appUserRepository.GetByIdAsync(request.Id, cancellationToken);

        if (appUser is null || appUser.IsDeleted)
        {
            return Result<AppUserDto>.NotFound("用户不存在。");
        }

        return Result<AppUserDto>.Success(new AppUserDto(
            appUser.Id,
            appUser.UserName,
            appUser.Email,
            appUser.PhoneNumber,
            appUser.Sex,
            appUser.UserStatus));
    }
}