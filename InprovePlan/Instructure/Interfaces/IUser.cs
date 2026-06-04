using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instructure.Interfaces
{
    /// <summary>
    /// 系统当前用户标识
    /// </summary>
    public interface IUser
    {
        public long? Id { get;  }
    }
}
