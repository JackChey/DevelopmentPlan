using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instructure.IResult
{
    /// <summary>
    /// 无返回数据响应类型
    /// </summary>
    public class Result : IResult
    {
        public ResultStatus Status { get; }

        public IReadOnlyList<string>? Errors { get; }

        public bool IsSuccess => Status.Equals(ResultStatus.Ok);

        public Result(ResultStatus status, IReadOnlyList<string>? errors = null)
        {
            Status = status;
            Errors = errors ?? Array.Empty<string>();

            if (status == ResultStatus.Ok && Errors.Count > 0)
                throw new InvalidOperationException("Success result cannot contain errors.");
        }

        public static Result Seccess => new Result(ResultStatus.Ok);

        internal static IReadOnlyList<string>? NormalizeErrors(IEnumerable<string> errors) => errors.Where(e => !string.IsNullOrWhiteSpace(e)).Distinct().ToArray();

        public static Result Invalid(params string[] errors) => new Result(ResultStatus.Invalid, NormalizeErrors(errors));
        public static Result Unauthorized(params string[] errors) => new Result(ResultStatus.Unauthorized, NormalizeErrors(errors));

        public static Result NotFound(params string[] errors) => new Result(ResultStatus.NotFound, NormalizeErrors(errors));

        public static Result Forbidden(params string[] errors) => new Result(ResultStatus.Forbidden, NormalizeErrors(errors));

        public static Result Conflict(params string[] errors) => new(ResultStatus.Conflict, NormalizeErrors(errors));

        public static Result Failure(params string[] errors) => new(ResultStatus.Error, NormalizeErrors(errors));
    }

    /// <summary>
    /// 带返回数据响应类型
    /// </summary>
    public class Result<T> : IResult<T>
    {
        public ResultStatus Status { get; }

        public IReadOnlyList<string>? Errors { get; }

        public bool IsSuccess => Status.Equals(ResultStatus.Ok);

        public T? Value { get; }


        public Result(T value)
        {
            Value = value;
            Status = ResultStatus.Ok;
            Errors = Array.Empty<string>();
        }

        public Result(ResultStatus status, IReadOnlyList<string>? errors = null)
        {
            Status = status;
            Errors = errors ?? Array.Empty<string>();

            if (status == ResultStatus.Ok && Errors.Count > 0)
                throw new InvalidOperationException("Success result cannot contain errors.");
        }

        public static Result<T> Seccess(T value) => new Result<T>(value);

        public static Result<T> Invalid(params string[] errors) => new Result<T>(ResultStatus.Invalid, Result.NormalizeErrors(errors));
        public static Result<T> Unauthorized(params string[] errors) => new Result<T>(ResultStatus.Unauthorized, Result.NormalizeErrors(errors));

        public static Result<T> NotFound(params string[] errors) => new Result<T>(ResultStatus.NotFound, Result.NormalizeErrors(errors));

        public static Result<T> Forbidden(params string[] errors) => new Result<T>(ResultStatus.Forbidden, Result.NormalizeErrors(errors));

        public static Result<T> Conflict(params string[] errors) => new(ResultStatus.Conflict, Result.NormalizeErrors(errors));

        public static Result<T> Failure(params string[] errors) => new(ResultStatus.Error, Result.NormalizeErrors(errors));

        public static Result<T> From(Result result) =>
        result.Status == ResultStatus.Ok
            ? throw new InvalidOperationException("Cannot convert successful non-generic Result to Result<T> without value.")
            : new Result<T>(result.Status, result.Errors);
    }
}


