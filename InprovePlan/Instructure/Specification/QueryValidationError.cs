using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instructure.Specification;

/// <summary>
/// 查询参数校验错误。
/// 
/// 用于描述条件查询中的非法参数，例如：
/// - 开始时间大于结束时间
/// - 关键字过长
/// - 状态枚举非法
/// 
/// 不直接绑定 HTTP，方便上层统一转换为 API 错误响应。
/// </summary>
/// <param name="Field">错误字段名。</param>
/// <param name="Message">错误说明。</param>
public sealed record QueryValidationError(string Field, string Message);