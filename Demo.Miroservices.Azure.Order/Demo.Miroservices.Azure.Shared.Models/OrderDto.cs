namespace Demo.Miroservices.Azure.Shared.Models
{
    public sealed class OrderDto
    {
        public Guid? Id { get; init; }
        public string? CustomerId { get; init; }
        public decimal Amount { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
    }
}
