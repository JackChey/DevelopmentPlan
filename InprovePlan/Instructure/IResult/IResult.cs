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

        public object? GetValue();

        public IEnumerable<string>? Errors { get; } 

        bool IsSuccess { get; }
    }
}
