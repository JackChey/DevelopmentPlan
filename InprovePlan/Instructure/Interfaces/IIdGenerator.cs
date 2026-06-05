using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instructure.Interfaces
{
    /// <summary>
    /// 全局唯一 ID 生成器接口。
    /// </summary>
    public interface IIdGenerator
    {
        /// <summary>
        /// 生成一个新的全局唯一 ID。
        /// </summary>
        /// <returns>全局唯一 long 类型 ID。</returns>
        long NewId();
    }
}
