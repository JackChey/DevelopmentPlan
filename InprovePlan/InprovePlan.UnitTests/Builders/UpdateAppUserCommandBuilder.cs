using InprovePlan.Domain.Entities;
using InprovePlan.UnitTests.TestData;
using InprovePlan.UserCase.AppUsers.Commands;

namespace InprovePlan.UnitTests.Builders;

/// <summary>
/// 更新用户信息命令 (UpdateAppUserCommand) 的测试数据构建器。
/// 用于在单元测试中快速构造合法或特定场景下的更新命令对象，
/// 避免在每个测试方法中重复编写繁琐的对象初始化代码。
/// </summary>
public sealed class UpdateAppUserCommandBuilder
{
    // 默认用户ID：初始化为测试数据中定义的有效用户ID。
    // 这确保了在未显式指定ID时，构建出的命令指向一个存在的用户记录。
    private long _id = AppUserTestData.ValidUserId;

    // 默认用户名：初始化为测试数据中定义的有效用户名。
    private string _userName = AppUserTestData.ValidUserName;

    // 默认邮箱：初始化为测试数据中定义的有效邮箱地址。
    private string _email = AppUserTestData.ValidEmail;

    // 默认手机号：初始化为测试数据中定义的有效手机号码。
    private string _phoneNumber = AppUserTestData.ValidPhoneNumber;

    // 默认性别：初始化为测试数据中定义的有效性别枚举值。
    private AppUserSex _sex = AppUserTestData.ValidSex;

    // 默认用户状态：初始化为测试数据中定义的有效状态（如正常、冻结等）。
    private AppUserStatus _userStatus = AppUserTestData.ValidUserStatus;

    /// <summary>
    /// 设置用户ID。
    /// </summary>
    /// <param name="id">要更新的目标用户ID。</param>
    /// <returns>返回当前构建器实例，支持链式调用（Fluent Interface）。</returns>
    /// <remarks>
    /// 此方法允许测试人员自定义目标用户ID，例如测试更新不存在的用户ID或权限隔离场景。
    /// </remarks>
    public UpdateAppUserCommandBuilder WithId(long id)
    {
        // 更新内部用户ID字段
        _id = id;

        // 返回 this 以支持链式调用
        return this;
    }

    /// <summary>
    /// 设置邮箱地址。
    /// </summary>
    /// <param name="email">要设置的新邮箱地址字符串。</param>
    /// <returns>返回当前构建器实例，支持链式调用（Fluent Interface）。</returns>
    /// <remarks>
    /// 此方法常用于测试邮箱格式校验、邮箱唯一性冲突或空值处理等场景。
    /// </remarks>
    public UpdateAppUserCommandBuilder WithEmail(string email)
    {
        // 更新内部邮箱字段
        _email = email;

        // 返回 this 以支持链式调用
        return this;
    }

    /// <summary>
    /// 构建并返回最终的 UpdateAppUserCommand 对象。
    /// </summary>
    /// <returns>包含当前配置参数的命令对象实例。</returns>
    /// <remarks>
    /// 此方法是构建过程的终点。它使用当前内部存储的各字段值
    /// 实例化命令对象。在此之后，构建器通常不再被使用，或者可以重置以构建新对象。
    /// </remarks>
    public UpdateAppUserCommand Build()
    {
        // 使用当前配置好的参数创建更新命令对象
        return new UpdateAppUserCommand(_id, _userName, _email, _phoneNumber, _sex, _userStatus);
    }
}

