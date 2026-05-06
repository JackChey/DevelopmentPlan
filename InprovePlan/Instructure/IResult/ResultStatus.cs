using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instructure.IResult
{
    public enum ResultStatus
    {
        Ok = 200,
        Error,
        Forbidden,
        Unauthorized = 403,
        NotFound,
        Invalid,
    }
}
