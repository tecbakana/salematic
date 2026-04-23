using Salematic.Domain.Interfaces;
using Salematic.Domain.Models;

namespace Salematic.Infrastructure.Payment
{
    public class MockPaymentConfigStore : IMockPaymentConfigStore
    {

        public static readonly Dictionary<string, MockPaymentConfig> _store = new Dictionary<string, MockPaymentConfig>
        {

            ["pix"] = new MockPaymentConfig
            {
                Id = "pix",
                Enabled = true,
                Behavior = "approved",
                RejectionReason = null,
                Delay = 1200
            },
            ["credit_visa"] = new MockPaymentConfig
            {
                Id = "credit_visa",
                Enabled = true,
                Behavior = "approved",
                RejectionReason = null,
                Delay = 2000
            },
            ["credit_master"] = new MockPaymentConfig
            {
                Id = "credit_master",
                Enabled = true,
                Behavior = "approved",
                RejectionReason = null,
                Delay = 2000
            },
            ["debit"] = new MockPaymentConfig
            {
                Id = "debit",
                Enabled = false,
                Behavior = "rejected",
                RejectionReason = "Saldo insuficiente simulado para testes de rejeição",
                Delay = 1500
            },
            ["boleto"] = new MockPaymentConfig
            {
                Id = "boleto",
                Enabled = true,
                Behavior = "pending",
                RejectionReason = null,
                Delay = 800
            }
        };

        public MockPaymentConfigStore() { }

        public Task<IEnumerable<MockPaymentConfig>> GetAllAsync()
        {
            return Task.FromResult(_store.Values.AsEnumerable());
        }


        public Task UpdateAsync(string methodId, MockPaymentConfig config)
        {
            // Neste mock, não persistimos as alterações. Em uma implementação real, salvaríamos em banco ou cache.
            _store[methodId] = config;
            return Task.CompletedTask;
        }
    }
}
