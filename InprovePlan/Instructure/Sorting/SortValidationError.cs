using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instructure.Sorting;

/// <summary>
/// 排序参数校验错误。
/// 
/// 不直接依赖 HTTP，方便在 Controller、ApplicationService、仓储层之间复用。
/// </summary>
/// <param name="Field">错误字段，例如 SortBy、SortDirection。</param>
/// <param name="Message">错误说明。</param>
public sealed record SortValidationError(string Field, string Message);