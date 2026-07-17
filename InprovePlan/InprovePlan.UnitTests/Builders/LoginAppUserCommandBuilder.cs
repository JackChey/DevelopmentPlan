using InprovePlan.UnitTests.TestData;
using InprovePlan.UserCase.AppUsers.Commands;

namespace InprovePlan.UnitTests.Builders;

/// <summary>
/// 用户登录命令 (LoginAppUserCommand) 的测试数据构建器。
/// 用于在单元测试中快速构造合法或特定异常场景下的登录请求对象，
/// 避免在每个测试方法中重复编写繁琐的对象初始化代码。
/// </summary>
public sealed class LoginAppUserCommandBuilder
{
    // 默认用户名：初始化为测试数据中定义的有效用户名。
    // 这确保了在未显式指定时，构建出的命令包含一个已注册且有效的用户标识。
    private string _userName = AppUserTestData.ValidUserName;

    // 默认密码：初始化为测试数据中定义的正确密码。
    // 这确保了在未显式指定时，构建出的命令可以通过身份验证（Happy Path）。
    private string _password = AppUserTestData.ValidPassword;

    /// <summary>
    /// 设置用户名。
    /// </summary>
    /// <param name="userName">要设置的用户名字符串。</param>
    /// <returns>返回当前构建器实例，支持链式调用（Fluent Interface）。</returns>
    /// <remarks>
    /// 此方法常用于测试以下场景：
    /// 1. 用户名不存在（传入未注册的用户名）。
    /// 2. 用户名格式错误（如空字符串、超长字符串）。
    /// 3. 大小写敏感性测试。
    /// </remarks>
    public LoginAppUserCommandBuilder WithUserName(string userName)
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
    /// 此方法常用于测试以下场景：
    /// 1. 密码错误（传入与用户名不匹配的密码）。
    /// 2. 空密码或 null 值处理。
    /// 3. 特殊字符或编码问题。
    /// </remarks>
    public LoginAppUserCommandBuilder WithPassword(string password)
    {
        // 更新内部密码字段
        _password = password;

        // 返回 this 以支持链式调用
        return this;
    }

    /// <summary>
    /// 构建并返回最终的 LoginAppUserCommand 对象。
    /// </summary>
    /// <returns>包含当前配置参数的登录命令对象实例。</returns>
    /// <remarks>
    /// 此方法是构建过程的终点。它使用当前内部存储的 _userName 和 _password 
    /// 实例化命令对象。在此之后，构建器通常不再被使用，或者可以重置以构建新对象。
    /// </remarks>
    public LoginAppUserCommand Build()
    {
        // 使用当前配置好的参数创建登录命令对象
        return new LoginAppUserCommand(_userName, _password);
    }
}

