using FluentValidation;
using Salematic.Application.Commands.Checkout;
using Salematic.Application.DTOs;

namespace SeuProjeto.Application.Validators;

// 1. Validador Principal do Command (O MediatR vai encontrar este cara na Pipeline)
public class ProcessarCheckoutCommandValidator : AbstractValidator<ProcessarCheckoutCommand>
{
    public ProcessarCheckoutCommandValidator()
    {
        // Validações das propriedades da raiz do Command
        RuleFor(x => x.ClienteId)
            .GreaterThan(0).WithMessage("O ID do cliente deve ser maior que zero.");

        RuleFor(x => x.MetodoPagamento)
            .NotEmpty().WithMessage("O método de pagamento é obrigatório.");

        // Validação condicional: Se for cartão, o número é obrigatório
        When(x => x.MetodoPagamento.Equals("Cartao", StringComparison.OrdinalIgnoreCase), () =>
        {
            RuleFor(x => x.NumeroCartao)
                .NotEmpty().WithMessage("O número do cartão é obrigatório para pagamentos via cartão.");
        });

        // Valida a lista em si (se está vazia ou nula)
        RuleFor(x => x.Itens)
            .NotEmpty().WithMessage("A lista de itens não pode estar vazia.");

        // Dispara a validação para CADA item dentro da lista
        RuleForEach(x => x.Itens)
            .SetValidator(new WebhookItemRequestValidator());
    }
}

// 2. Validador do Item da Lista (Pode ser internal se for usado só aqui)
internal class WebhookItemRequestValidator : AbstractValidator<WebhookItemRequest>
{
    public WebhookItemRequestValidator()
    {
        // Supondo que suas propriedades sejam Id (do produto) e Quantidade
        RuleFor(x => x.ProdutoId)
            .GreaterThan(0).WithMessage("O ID do produto é obrigatório.");

        RuleFor(x => x.Quantidade)
            .GreaterThan(0).WithMessage("A quantidade do produto deve ser maior que zero.");
    }
}