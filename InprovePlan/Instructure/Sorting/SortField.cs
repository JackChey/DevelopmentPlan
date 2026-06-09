using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instructure.Sorting;

using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;

/// <summary>
/// 单个允许排序的字段定义。
/// 
/// TEntity：实体类型。
/// 
/// 这个类的意义是：
/// 把“前端字段名”和“后端 EF Core 排序表达式”绑定起来。
/// 
/// 例如：
/// 前端传 createdAt
/// 后端实际执行 x => x.CreatedAt
/// </summary>
/// <typeparam name="TEntity">实体类型。</typeparam>
public sealed class SortField<TEntity>
{
    private SortField(
        string name,
        LambdaExpression keySelector)
    {
        Name = name;
        KeySelector = keySelector;
    }

    /// <summary>
    /// 对外暴露的排序字段名。
    /// 
    /// 这是前端允许传入的字段名。
    /// 建议使用 camelCase，例如 createdAt、updatedAt、name。
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// EF Core 排序表达式。
    /// 
    /// 注意：
    /// 这里保存为 LambdaExpression，
    /// 是为了支持不同字段类型：
    /// - DateTime
    /// - string
    /// - int
    /// - long
    /// - decimal
    /// 等等。
    /// </summary>
    public LambdaExpression KeySelector { get; }

    /// <summary>
    /// 创建一个排序字段定义。
    /// 
    /// TKey 是排序字段的真实类型。
    /// 例如：
    /// CreatedAt 是 DateTime
    /// Name 是 string
    /// Id 是 long
    /// </summary>
    public static SortField<TEntity> Create<TKey>(
        string name,
        Expression<Func<TEntity, TKey>> keySelector)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ValidationException("排序字段名不能为空。");
        }

        return new SortField<TEntity>(name, keySelector);
    }
}