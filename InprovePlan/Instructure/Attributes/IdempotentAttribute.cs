using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instructure.Attributes;

/// <summary>
/// 标记某个接口需要启用幂等控制。
/// 
/// 建议只给有副作用的接口添加，比如：
/// 1. 创建订单
/// 2. 发起支付
/// 3. 扣减库存
/// 4. 发放优惠券
/// 
/// 不建议给 GET 接口添加，因为 GET 理论上应该天然无副作用。
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class IdempotentAttribute : Attribute
{
}
