using InprovePlan.Domain.Entities;
using InprovePlan.UnitTests.TestData;
using InprovePlan.UserCase.AppUsers.Commands;

namespace InprovePlan.UnitTests.Builders;

/// <summary>
/// 创建用户命令 (CreateAppUserCommand) 的测试数据构建器。
/// 用于在单元测试中快速构造合法或特定场景下的命令对象，
/// 避免在每个测试方法中重复编写繁琐的对象初始化代码。
/// </summary>
public sealed class CreateAppUserCommandBuilder
{
    // 默认用户名：初始化为测试数据中定义的有效用户名。
    // 这确保了在未显式指定时，构建出的命令包含一个符合格式要求的用户名。
    private string _userName = AppUserTestData.ValidUserName;

    // 默认密码：初始化为测试数据中定义的有效密码。
    // 通常符合复杂度要求（如长度、大小写、特殊字符等），用于模拟正常的注册流程。
    private string _password = AppUserTestData.ValidPassword;

    // 默认性别：初始化为测试数据中定义的有效性别枚举值。
    private AppUserSex _sex = AppUserTestData.ValidSex;

    // 默认手机号：初始化为测试数据中定义的有效手机号码。
    // 符合标准的手机号格式，用于模拟正常的联系方式录入。
    private string _phoneNumber = AppUserTestData.ValidPhoneNumber;

    // 默认邮箱：初始化为测试数据中定义的有效邮箱地址。
    // 符合标准的电子邮件格式，用于模拟正常的邮箱录入。
    private string _email = AppUserTestData.ValidEmail;

    /// <summary>
    /// 设置用户名。
    /// </summary>
    /// <param name="userName">要设置的用户名字符串。</param>
    /// <returns>返回当前构建器实例，支持链式调用（Fluent Interface）。</returns>
    /// <remarks>
    /// 此方法常用于测试用户名唯一性校验、长度限制或非法字符拦截等场景。
    /// </remarks>
    public CreateAppUserCommandBuilder WithUserName(string userName)
    {
        // 更新内部用户名字段
        _userName = userName;

        // 返回 this 以支持链式调用
        return this;
    }

    /// <summary>
    /// 设置密码。
    /// </summary>
    /// <param name="password">要设置的密码字符串。</param>
    /// <returns>返回当前构建器实例，支持链式调用（Fluent Interface）。</returns>
    /// <remarks>
    /// 此方法常用于测试密码复杂度校验、长度限制或弱密码拦截等场景。
    /// </remarks>
    public CreateAppUserCommandBuilder WithPassword(string password)
    {
        // 更新内部密码字段
        _password = password;

        // 返回 this 以支持链式调用
        return this;
    }

    /// <summary>
    /// 设置邮箱地址。
    /// </summary>
    /// <param name="email">要设置的邮箱地址字符串。</param>
    /// <returns>返回当前构建器实例，支持链式调用（Fluent Interface）。</returns>
    /// <remarks>
    /// 此方法常用于测试邮箱格式校验、邮箱唯一性校验等场景。
    /// </remarks>
    public CreateAppUserCommandBuilder WithEmail(string email)
    {
        // 更新内部邮箱字段
        _email = email;

        // 返回 this 以支持链式调用
        return this;
    }

    /// <summary>
    /// 构建并返回最终的 CreateAppUserCommand 对象。
    /// </summary>
    /// <returns>包含当前配置参数的命令对象实例。</returns>
    /// <remarks>
    /// 此方法是构建过程的终点。它使用当前内部存储的各字段值
    /// 实例化命令对象。在此之后，构建器通常不再被使用，或者可以重置以构建新对象。
    /// </remarks>
    public CreateAppUserCommand Build()
    {
        // 使用当前配置好的参数创建命令对象
        return new CreateAppUserCommand(_userName, _password, _sex, _phoneNumber, _email);
    }
}

