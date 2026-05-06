using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instructure.IResult
{
    public class Result<T> : IResult
    {
        public Result(T value)
        {
            Value = value;
        }

        public Result(ResultStatus status)
        {
            Status = status;
        }

        public T? Value { get; init; }

        public ResultStatus Status { get; set; } = ResultStatus.Ok;

        public IEnumerable<string>? Errors { get; set; }

        public bool IsSuccess => Status.Equals(ResultStatus.Ok);

        public object? GetValue()
        {
            return Value;
        }

        /// <summary>
        /// 从带泛型结构转换为不带泛型结果
        /// </summary>
        /// <param name="result"></param>
        public static implicit operator Result<T>(Result result)
        {
            return new Result<T>(default(T))
            {
                Status = result.Status,
                Errors = result.Errors,
            };
        }
    }

    public class Result : Result<Result>
    {
        public Result(Result value) : base(value)
        {
            Value = value;
        }

        public Result(ResultStatus status) : base(status)
        {
            Status = status;
        }

        /// <summary>
        /// 从不带泛型结果转换为带泛型结果
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        public static Result From(IResult result)
        {
            return new Result(result.Status)
            {
                Errors = result.Errors,
            };
        }

        public static Result Success()
        {
            return new Result(ResultStatus.Ok);
        }

        public static Result<T> Success<T>(T value)
        {
            return new Result<T>(value);
        }

        public static Result Failure()
        {
            return new Result(ResultStatus.Error);
        }

        public static Result Failure(IEnumerable<string>? errors)
        {
            return new Result(ResultStatus.Error)
            {
                Errors = errors
            };
        }

        /// <summary>
        /// 数据未找到
        /// </summary>
        /// <returns></returns>
        public static Result NotFound(params string[] error)
        {
            return new Result(ResultStatus.NotFound)
            {
                Errors = error.AsEnumerable()
            };
        }

        public static Result Forbidden()
        {
            return new Result(ResultStatus.Forbidden);
        }

        public static Result Unauthorized()
        {
            return new Result(ResultStatus.Unauthorized);
        }

        public static Result Invalid()
        {
            return new Result(ResultStatus.Invalid);
        }

        public static Result Invalid(params string[] errors)
        {
            return new Result(ResultStatus.Invalid)
            {
                Errors = errors
            };
        }
    }
}


