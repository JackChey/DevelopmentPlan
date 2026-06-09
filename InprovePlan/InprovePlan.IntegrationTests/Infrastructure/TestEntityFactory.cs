using InprovePlan.Domain.Entities;
using Instructure.Interfaces;
using Instructure.IResult;

namespace InprovePlan.IntegrationTests.Infrastructure;

/// <summary>
/// 集成测试实体工厂。
///
/// 所有实体显式设置 Id，
/// 避免 ValueGeneratedNever 下 Id = 0 导致跟踪冲突。
/// </summary>
public static class TestEntityFactory
{
    public static AppUser CreateUser(
        IIdGenerator idGenerator,
        IPasswordHasher passwordHasher,
        string userName,
        string email,
        string phoneNumber,
        AppUserStatus status = AppUserStatus.Enable,
        bool isDeleted = false)
    {
        return new AppUser
        {
            Id = idGenerator.NewId(),
            UserName = userName,
            Email = email,
            PhoneNumber = phoneNumber,
            PasswordHash = passwordHasher.Hash("Password123?"),
            Sex = AppUserSex.Secret,
            UserStatus = status,
            IsDeleted = isDeleted,
            DeletedAt = isDeleted ? DateTimeOffset.UtcNow : null,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}