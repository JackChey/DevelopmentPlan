using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instructure.Repositories;

using System;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Redis 仓储接口定义。
/// 
/// 该接口封装了常用的 Redis 操作，提供统一的异步访问入口。
/// 实现类需负责处理连接的获取、序列化/反序列化逻辑以及异常处理。
/// </summary>
public interface IRedisRepository
{
    /// <summary>
    /// 异步获取指定键的值。
    /// </summary>
    /// <typeparam name="T">期望返回的数据类型。实现类需负责将 Redis 字符串值反序列化为该类型。</typeparam>
    /// <param name="key">Redis 中的键名。</param>
    /// <param name="cancellationToken">用于取消操作的取消令牌。</param>
    /// <returns>
    /// 如果键存在且反序列化成功，返回对应的值；
    /// 如果键不存在或反序列化失败，返回 default(T)（对于引用类型通常为 null）。
    /// </returns>
    Task<T?> GetAsync<T>(
        string key,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步设置指定键的值。
    /// </summary>
    /// <typeparam name="T">要存储的数据类型。实现类需负责将该对象序列化为字符串或二进制数据。</typeparam>
    /// <param name="key">Redis 中的键名。</param>
    /// <param name="value">要存储的值。</param>
    /// <param name="expiry">可选的过期时间。如果为 null，则键永不过期（除非 Redis 配置了全局策略）。</param>
    /// <param name="cancellationToken">用于取消操作的取消令牌。</param>
    /// <returns>如果设置成功，返回 true；否则返回 false。</returns>
    Task<bool> SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiry = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步删除指定键。
    /// </summary>
    /// <param name="key">要删除的 Redis 键名。</param>
    /// <param name="cancellationToken">用于取消操作的取消令牌。</param>
    /// <returns>如果键被成功删除，返回 true；如果键不存在，返回 false。</returns>
    Task<bool> RemoveAsync(
        string key,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步检查指定键是否存在。
    /// </summary>
    /// <param name="key">要检查的 Redis 键名。</param>
    /// <param name="cancellationToken">用于取消操作的取消令牌。</param>
    /// <returns>如果键存在，返回 true；否则返回 false。</returns>
    Task<bool> ExistsAsync(
        string key,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步对指定键的值进行原子递增操作。
    /// 如果键不存在，通常会先初始化为 0，然后再执行递增。
    /// </summary>
    /// <param name="key">要递增的 Redis 键名。该键对应的值必须是整数或可转换为整数的字符串。</param>
    /// <param name="value">递增的步长，默认为 1。可以是负数以实现递减。</param>
    /// <param name="expiry">可选的过期时间。仅在键是新创建时应用过期时间（具体行为取决于实现）。</param>
    /// <param name="cancellationToken">用于取消操作的取消令牌。</param>
    /// <returns>递增操作完成后的新值。</returns>
    Task<long> IncrementAsync(
        string key,
        long value = 1,
        TimeSpan? expiry = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步设置哈希表（Hash）中指定字段的值。
    /// 对应 Redis 命令 HSET。
    /// </summary>
    /// <typeparam name="T">字段值的数据类型。实现类需负责序列化。</typeparam>
    /// <param name="key">哈希表所在的键名。</param>
    /// <param name="field">哈希表中的字段名。</param>
    /// <param name="value">要设置的字段值。</param>
    /// <param name="cancellationToken">用于取消操作的取消令牌。</param>
    /// <returns>如果字段是新建的，返回 true；如果字段已存在并被更新，返回 false（具体返回值语义可根据业务需求调整，通常 HSET 返回 1 表示新建，0 表示更新）。</returns>
    Task<bool> SetHashAsync<T>(
        string key,
        string field,
        T value,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步获取哈希表（Hash）中指定字段的值。
    /// 对应 Redis 命令 HGET。
    /// </summary>
    /// <typeparam name="T">期望返回的字段值类型。实现类需负责反序列化。</typeparam>
    /// <param name="key">哈希表所在的键名。</param>
    /// <param name="field">要获取值的字段名。</param>
    /// <param name="cancellationToken">用于取消操作的取消令牌。</param>
    /// <returns>
    /// 如果字段存在且反序列化成功，返回对应的值；
    /// 如果字段不存在，返回 default(T)。
    /// </returns>
    Task<T?> GetHashAsync<T>(
        string key,
        string field,
        CancellationToken cancellationToken = default);
}
