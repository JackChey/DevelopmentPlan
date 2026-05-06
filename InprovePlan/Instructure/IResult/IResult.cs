using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instructure.IResult
{
    public interface  IResult
    {
        public ResultStatus Status { get;}

        public IReadOnlyList<string>? Errors { get; } 

        bool IsSuccess { get; }
    }

    public interface IResult<out T>: IResult
    {
       T? Value { get; }
    }
}
