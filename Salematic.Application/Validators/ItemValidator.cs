using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Salematic.Application.DTOs;
using System.Diagnostics;

namespace Salematic.Application.Validators
{
    public class SubItemValidator : AbstractValidator<WebhookItemRequest>
    {
        public SubItemValidator()
        {
            RuleFor(x => x.ProdutoId).GreaterThan(0).WithMessage("O ID do produto deve ser maior que zero.");
            RuleFor(x => x.Quantidade).GreaterThan(0).WithMessage("A quantidade deve ser maior que zero.");
        }
    }
    public class ItemValidator : AbstractValidator<ItemValidatorRecord>
    {
        public ItemValidator()
        {
            RuleFor(x => x.Itens).NotEmpty().WithMessage("O produto é obrigatório.");
            RuleForEach(x => x.Itens).SetValidator(new SubItemValidator());
        }
    }
}
