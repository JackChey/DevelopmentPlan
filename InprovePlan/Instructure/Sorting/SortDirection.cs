
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instructure.Sorting;

/// <summary>
/// 排序方向。
/// 
/// 使用枚举是为了避免系统内部到处传字符串。
/// </summary>
public enum SortDirection
{
    /// <summary>
    /// 升序。
    /// </summary>
    Asc,

    /// <summary>
    /// 降序。
    /// </summary>
    Desc
}
