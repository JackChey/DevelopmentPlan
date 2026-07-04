using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instructure.Interfaces;

/// <summary>
/// 标记一个 MediatR 请求需要启用幂等控制。
/// 
/// 只要某个 IRequest 实现了这个接口，
/// IdempotencyBehavior 就会对它执行幂等校验。
/// 
/// 推荐用于有副作用的业务请求：
/// 1. 创建订单
/// 2. 发起支付
/// 3. 扣减库存
/// 4. 发放优惠券
/// 5. 创建外部资源
/// 
/// 不建议用于纯查询请求。
/// </summary>
public interface IIdempotentRequest
{
    /// <summary>
    /// 客户端传入的幂等键。
    /// 
    /// 一般来自 HTTP Header：
    /// Idempotency-Key: xxxxx
    /// 
    /// Controller 负责从 Header 中读取，
    /// 然后赋值到具体 Command 中。
    /// </summary>
    string IdempotencyKey { get; }
}