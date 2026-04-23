using Salematic.Domain.Models;

namespace Salematic.Domain.Interfaces;

public interface IMockPaymentConfigStore
{
    public Task<IEnumerable<MockPaymentConfig>> GetAllAsync();
    public Task UpdateAsync(string methodId, MockPaymentConfig config);
}


