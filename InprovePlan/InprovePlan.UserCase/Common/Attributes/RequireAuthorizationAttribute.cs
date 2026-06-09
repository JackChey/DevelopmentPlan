using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InprovePlan.UserCase.Common.Attributes;

/// <summary>
/// UseCase 授权特性。
///
/// 用于标记某个 Command / Query 必须登录后才能执行。
///
/// 注意：
/// 这是应用层授权标记，不是 ASP.NET Core MVC 的 AuthorizeAttribute。
/// 这样可以避免和 Controller 层授权体系混淆。
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class RequireAuthorizationAttribute : Attribute
{

}