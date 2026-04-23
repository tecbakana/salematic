using System.Text.Json;

namespace Salematic.Domain.Models
{
    public class MockPaymentConfig
    {
        public string Id { get; set; }
        public bool Enabled { get; set; }
        public string Behavior { get; set; } // valores possíveis: approved, rejected, pending, timeout, error
        public string RejectionReason { get; set; }
        public int Delay { get; set; } // milissegundos
    }
}
