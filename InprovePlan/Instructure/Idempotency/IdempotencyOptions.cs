using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instructure.Idempotency;

/// <summary>
/// 幂等配置项。
/// 
/// 建议放在 appsettings.json 中：
/// 
/// "Idempotency": {
///   "HeaderName": "Idempotency-Key",
///   "ExpirationHours": 24,
///   "LockSeconds": 30
/// }
/// </summary>
public sealed class IdempotencyOptions
{
    /// <summary>
    /// 
    /// </summary>
    public string HeaderName { get; set; } = "Idempotency-Key";

    /// <summary>
    /// 幂等记录保留多久。
    /// 
    /// 支付、订单类业务通常建议至少 24 小时。
    /// 某些金融场景可能需要更长时间。
    /// </summary>
    public int ExpirationHours { get; set; } = 24;

    /// <summary>
    /// Redis 短期锁过期时间。
    /// 
    /// 它只用于降低并发穿透概率，不是最终一致性保障。
    /// </summary>
    public int LockSeconds { get; set; } = 60;
}