using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Salematic.Application.Behaviors;

public class TestingBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<TestingBehavior<TRequest, TResponse>> _logger;

    public TestingBehavior(ILogger<TestingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
    TRequest request,
    RequestHandlerDelegate<TResponse> next,
    CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var sw = Stopwatch.StartNew();

        _logger.LogInformation("[MediatR] Intermediario - {RequestName}", requestName);

        try
        {
            var response = await next();
            sw.Stop();
            _logger.LogInformation(
                "[MediatR] Intermediario - {RequestName} concluído em {ElapsedMs}ms",
                requestName, sw.ElapsedMilliseconds);
            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex,
                "[MediatR] Intermediário - {RequestName} falhou em {ElapsedMs}ms",
                requestName, sw.ElapsedMilliseconds);
            throw;
        }
    }
}
