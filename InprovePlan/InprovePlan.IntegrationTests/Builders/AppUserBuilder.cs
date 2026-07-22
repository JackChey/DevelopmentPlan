using InprovePlan.Domain.Entities;
using InprovePlan.IntegrationTests.TestData;
using Instructure.Interfaces;
using Instructure.IResult;

namespace InprovePlan.IntegrationTests.Builders;

/// <summary>
/// 应用用户构建器，用于创建和配置 AppUser 实例。
/// 采用建造者模式，支持链式调用以灵活设置用户属性，并在构建时处理密码哈希和ID生成。
/// </summary>
public class AppUserBuilder
{
    /// <summary>
    /// ID 生成器接口，用于在构建用户时生成唯一标识符。
    /// </summary>
    private readonly IIdGenerator _idGenerator;

    /// <summary>
    /// 密码哈希器接口，用于对明文密码进行哈希处理。
    /// </summary>
    private readonly IPasswordHasher _passwordHasher;

    /// <summary>
    /// 用户 ID，初始化为测试数据中的有效用户 ID。
    /// </summary>
    private long _userId = AppUserTestData.ValidUserId;

    /// <summary>
    /// 用户名，初始化为测试数据中的有效用户名。
    /// </summary>
    private string _userName = AppUserTestData.ValidUserName;

    /// <summary>
    /// 电子邮件地址，初始化为测试数据中的有效邮箱。
    /// </summary>
    private string _email = AppUserTestData.ValidEmail;

    /// <summary>
    /// 电话号码，初始化为测试数据中的有效手机号。
    /// </summary>
    private string _phoneNumber = AppUserTestData.ValidPhoneNumber;

    /// <summary>
    /// 明文密码，初始化为测试数据中的有效密码。
    /// 注意：此字段仅用于临时存储，构建时会转换为哈希值。
    /// </summary>
    private string _password = AppUserTestData.ValidPassword;

    /// <summary>
    /// 性别，初始化为测试数据中的有效性别。
    /// </summary>
    private AppUserSex _sex = AppUserTestData.ValidSex;

    /// <summary>
    /// 用户状态，初始化为测试数据中的有效用户状态。
    /// </summary>
    private AppUserStatus _userStatus = AppUserTestData.ValidUserStatus;

    /// <summary>
    /// 删除标志，初始化为测试数据中的有效删除状态。
    /// </summary>
    private bool _isDeleted = AppUserTestData.ValidIsDeleted;

    /// <summary>
    /// 初始化 AppUserBuilder 的新实例。
    /// </summary>
    /// <param name="idgenerator">ID 生成器实现。</param>
    /// <param name="passwordhasher">密码哈希器实现。</param>
    public AppUserBuilder(IIdGenerator idgenerator,
     IPasswordHasher passwordhasher)
    {
        this._idGenerator = idgenerator;
        this._passwordHasher = passwordhasher; ;
    }


    /// <summary>
    /// 设置用户 ID。
    /// </summary>
    /// <param name="userid">要设置的用户 ID。</param>
    /// <returns>当前的 AppUserBuilder 实例，支持链式调用。</returns>
    public AppUserBuilder WithUserId(long userid)
    {
        this._userId = userid;
        return this;
    }

    /// <summary>
    /// 设置用户名。
    /// </summary>
    /// <param name="username">要设置的用户名。</param>
    /// <returns>当前的 AppUserBuilder 实例，支持链式调用。</returns>
    public AppUserBuilder WithUserName(string username)
    {
        this._userName = username;
        return this;
    }

    /// <summary>
    /// 设置电子邮件地址。
    /// </summary>
    /// <param name="email">要设置的邮箱地址。</param>
    /// <returns>当前的 AppUserBuilder 实例，支持链式调用。</returns>
    public AppUserBuilder WithEmail(string email)
    {
        _email = email;
        return this;
    }

    /// <summary>
    /// 设置电话号码。
    /// </summary>
    /// <param name="phoneNumber">要设置的电话号码。</param>
    /// <returns>当前的 AppUserBuilder 实例，支持链式调用。</returns>
    public AppUserBuilder WithPhoneNumber(string phoneNumber)
    {
        _phoneNumber = phoneNumber;
        return this;
    }

    /// <summary>
    /// 设置明文密码。
    /// </summary>
    /// <param name="password">要设置的明文密码。</param>
    /// <returns>当前的 AppUserBuilder 实例，支持链式调用。</returns>
    public AppUserBuilder WithPassword(string password)
    {
        _password = password;
        return this;
    }

    /// <summary>
    /// 设置性别。
    /// </summary>
    /// <param name="sex">要设置的性别。</param>
    /// <returns>当前的 AppUserBuilder 实例，支持链式调用。</returns>
    public AppUserBuilder WithSex(AppUserSex sex)
    {
        _sex = sex;
        return this;
    }

    /// <summary>
    /// 设置用户状态。
    /// </summary>
    /// <param name="userStatus">要设置的用户状态。</param>
    /// <returns>当前的 AppUserBuilder 实例，支持链式调用。</returns>
    public AppUserBuilder WithUserStatus(AppUserStatus userStatus)
    {
        _userStatus = userStatus;
        return this;
    }

    /// <summary>
    /// 设置删除标志。
    /// </summary>
    /// <param name="isDeleted">要设置的删除状态。</param>
    /// <returns>当前的 AppUserBuilder 实例，支持链式调用。</returns>
    public AppUserBuilder WithIsDeleted(bool isDeleted)
    {
        _isDeleted = isDeleted;
        return this;
    }

    /// <summary>
    /// 根据当前配置构建并返回一个 AppUser 实例。
    /// 如果用户 ID 未被显式修改（仍为默认测试值），则使用默认 ID；否则生成新的唯一 ID。
    /// 密码将在构建过程中通过注入的哈希器进行哈希处理。
    /// 如果用户被标记为已删除，将自动设置删除时间。
    /// </summary>
    /// <returns>新创建的 AppUser 实例。</returns>
    public AppUser Build()
    {
        return new()
        {
            // 逻辑：如果 ID 是默认测试值，则保留该值（通常用于特定测试场景）；否则生成新 ID
            Id = _userId.Equals(AppUserTestData.ValidUserId) ? _userId : _idGenerator.NewId(),
            UserName = _userName,
            Email = _email,
            PhoneNumber = _phoneNumber,
            // 对明文密码进行哈希处理后存储
            PasswordHash = _passwordHasher.Hash(_password),
            Sex = _sex,
            UserStatus = _userStatus,
            IsDeleted = _isDeleted,
            // 如果标记为删除，则记录当前时间为删除时间，否则为空
            DeletedAt = _isDeleted ? DateTimeOffset.UtcNow : null,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }
}

