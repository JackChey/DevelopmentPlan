using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instructure.Idempotency;

using InprovePlan.Domain.BaseEntities;
using System.ComponentModel.DataAnnotations;

/// <summary>
/// 幂等请求记录实体。
/// 
/// 这张表用于记录一次带有 Idempotency-Key 的请求处理状态。
/// 
/// 核心目标：
/// 1. 防止同一个业务请求被重复执行。
/// 2. 支持客户端安全重试。
/// 3. 支持重复请求直接返回第一次处理结果。
/// 4. 支持并发场景下通过数据库唯一索引兜底。
/// 
/// 注意：
/// 幂等键不应该只按 Key 判断，生产环境建议至少结合：
/// Key + UserId + TenantId
/// 
/// 否则不同用户如果意外生成了相同 Key，可能互相影响。
/// </summary>
public  class IdempotencyRecord : AppAuditWithUserEntity
{
    /// <summary>
    /// 客户端传入的幂等键。
    /// 
    /// 一般来自 HTTP Header：
    /// Idempotency-Key: xxxxx
    /// 
    /// 建议客户端使用 UUID v4。
    /// 服务端应限制最大长度，避免恶意超长 Header。
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// 请求内容 Hash。
    /// 
    /// 用于判断同一个 Idempotency-Key 是否被复用于不同请求。
    /// 
    /// 通常 Hash 内容包含：
    /// 1. HTTP Method
    /// 2. Request Path
    /// 3. QueryString
    /// 4. Request Body
    /// 5. UserId
    /// 6. TenantId
    /// 
    /// 如果同一个 Key 对应的 RequestHash 不一致，应返回 409 Conflict。
    /// </summary>
    public string RequestHash { get; set; } = string.Empty;

    /// <summary>
    /// 当前请求所属用户。
    /// 
    /// 对外部用户接口，通常取 JWT 中的 sub/user_id。
    /// 对内部系统接口，可以取 client_id/app_id。
    /// 
    /// 它是唯一索引的一部分，用于避免不同用户之间 Key 冲突。
    /// </summary>
    public long UserId { get; set; } 

    /// <summary>
    /// HTTP Method。
    /// 
    /// 例如：
    /// POST
    /// PUT
    /// PATCH
    /// 
    /// 幂等机制通常用于有副作用的 POST/PUT/PATCH。
    /// </summary>
    public string Method { get; set; } = string.Empty;

    /// <summary>
    /// 请求路径。
    /// 
    /// 例如：
    /// /api/orders
    /// /api/payments
    /// 
    /// 记录 Path 有助于排查问题，也可以参与 RequestHash。
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// 幂等请求处理状态。
    /// 
    /// Processing：请求已登记，业务正在处理中。
    /// Succeeded：请求已成功处理，后续重复请求可直接返回缓存响应。
    /// Failed：请求处理失败，是否允许重试取决于业务策略。
    /// </summary>
    public IdempotencyRecordStatus Status { get; set; }

    /// <summary>
    /// 第一次请求成功时的 HTTP 状态码。
    /// 
    /// 重复请求命中缓存时，直接返回这个状态码。
    /// 
    /// 例如：
    /// 200
    /// 201
    /// </summary>
    public int? ResponseStatusCode { get; set; }

    /// <summary>
    /// 第一次请求成功时的响应体。
    /// 
    /// 重复请求命中缓存时，直接返回这个响应体。
    /// 
    /// 注意：
    /// 1. 不建议存储超大响应。
    /// 2. 不建议存储敏感明文。
    /// 3. 必要时可以只存 ResourceId，然后重新查询资源返回。
    /// </summary>
    public string? ResponseBody { get; set; }

    /// <summary>
    /// 失败原因。
    /// 
    /// 仅在 Status = Failed 时使用。
    /// 
    /// 注意：
    /// 不建议保存完整异常堆栈到数据库字段。
    /// 详细异常应写日志系统。
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 完成时间。
    /// 
    /// 当请求进入 Succeeded 或 Failed 状态时设置。
    /// </summary>
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>
    /// 过期时间。
    /// 
    /// 用于定时清理幂等记录。
    /// 
    /// 常见设置：
    /// 1. 普通创建接口：24 小时
    /// 2. 支付/订单接口：48 小时或更久
    /// 3. 金融类接口：按审计要求保留
    /// </summary>
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>
    /// 并发控制字段。
    /// 
    /// EF Core 可把它配置成 RowVersion。
    /// 
    /// 用途：
    /// 1. 防止多个线程同时更新同一条幂等记录。
    /// 2. 避免后写覆盖先写。
    /// 
    /// SQL Server 中通常映射为 rowversion。
    /// PostgreSQL 中可以使用 xmin 或普通 bytea/token 策略。
    /// </summary>
    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}