using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instructure.Idempotency;

/// <summary>
/// 幂等记录状态。
/// </summary>
public enum IdempotencyRecordStatus
{
    /// <summary>
    /// 请求已登记，业务正在执行。
    /// 
    /// 如果重复请求命中该状态，通常返回：
    /// 409 Conflict
    /// 或
    /// 202 Accepted
    /// </summary>
    Processing = 1,

    /// <summary>
    /// 请求已成功完成。
    /// 
    /// 如果重复请求命中该状态，直接返回第一次请求保存的响应。
    /// </summary>
    Succeeded = 2,

    /// <summary>
    /// 请求处理失败。
    /// 
    /// 是否允许同一个 Idempotency-Key 重试，要看业务策略：
    /// 1. 如果失败发生在业务执行前，可以允许重试。
    /// 2. 如果失败发生在业务执行后、响应返回前，应谨慎处理。
    /// </summary>
    Failed = 3
}