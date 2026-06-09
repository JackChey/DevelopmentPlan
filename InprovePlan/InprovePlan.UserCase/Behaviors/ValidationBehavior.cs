using FluentValidation;
using MediatR;
using ValidationException = InprovePlan.UserCase.Behaviors.ValidationException;

namespace InprovePlan.UserCase.Behaviors;

/// <summary>
/// 验证异常处理
/// </summary>
/// <typeparam name="TRequest"></typeparam>
/// <typeparam name="TResponse"></typeparam>
/// <param name="validators"></param>
public class ValidationBehavior<TRequest, TResponse>
        (IEnumerable<IValidator<TRequest>> validators) : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (validators.Any())
        {
            // 获取验证信息
            var context = new ValidationContext<TRequest>(request);

            // 进行数据验证并得到验证结果
            var validationResults = await Task.WhenAll(
                validators.Select(validator => validator
                .ValidateAsync(context, cancellationToken)));

            // 分析验证结果
            var errors = validationResults
                .Where(result => result.Errors.Count != 0)
                .SelectMany(result => result.Errors)
                .ToList();

            if (errors.Count != 0)
            {
                throw new ValidationException(new Dictionary<string, string[]>() { { "验证异常", errors.Select(e => $"{e.PropertyName}-{e.ErrorMessage}").ToArray() } });
            }
        }

        return await next();
    }
}
