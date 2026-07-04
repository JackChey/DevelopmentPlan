using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instructure.Idempotency;

public enum IdempotencyResultState
{
    /// <summary>
    /// 首次请求，允许继续执行业务。
    /// </summary>
    Started = 1,

    /// <summary>
    /// 已有成功响应，直接返回缓存内容。
    /// </summary>
    Cached = 2,

    /// <summary>
    /// 相同请求正在处理中。
    /// </summary>
    Processing = 3,

    /// <summary>
    /// 相同 Key 被用于不同请求内容。
    /// </summary>
    Conflict = 4
}