using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instructure.Idempotency;

/// <summary>
/// 幂等记录的持久化状态。
/// </summary>
public enum IdempotencyStatus
{
    /// <summary>
    /// 请求已经登记，业务还在处理中。
    /// </summary>
    Processing = 1,

    /// <summary>
    /// 请求已经成功完成，后续相同请求可以直接返回缓存响应。
    /// </summary>
    Succeeded = 2,

    /// <summary>
    /// 请求执行失败。
    /// 
    /// 是否允许同一个 Key 再次重试，需要根据业务策略决定。
    /// </summary>
    Failed = 3
}