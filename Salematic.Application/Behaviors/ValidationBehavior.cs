using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;


namespace Salematic.Application.Behaviors;

public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    
    // O ASP.NET injeta automaticamente todos os validadores do tipo TRequest encontrados
    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next(); // Não há validadores, segue para o próximo passo da pipeline (ou Handler)
        }

        var context = new ValidationContext<TRequest>(request);

        // Executa todos os validadores associados a este request de forma assíncrona
        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken))
        );

        // Agrupa todos os erros encontrados
        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Count != 0)
        {
            // Abordagem por Exception (Lançará um erro que seu Middleware global de exceptions pode capturar)
            throw new ValidationException(failures);
        }

        // Se passou em todas as validações, chama o próximo comportamento ou o Handler final
        return await next();
    }
}