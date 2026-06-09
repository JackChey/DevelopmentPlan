using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instructure.Paging;

/// <summary>
/// 分页参数校验错误。
/// 
/// 不直接绑定 HTTP，方便在 Controller、Minimal API、ApplicationService 中
/// 统一转换为业务错误响应。
/// </summary>
/// <param name="Field">错误字段名，例如 PageIndex、PageSize。</param>
/// <param name="Message">错误说明。</param>
public sealed record PagingValidationError(string Field, string Message);