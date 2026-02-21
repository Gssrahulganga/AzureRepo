using Demo.Miroservices.Azure.Shared.Models;
using System.Threading;
using System.Threading.Tasks;

namespace Demo.Miroservices.Azure.Order.Services
{
    public interface IOrderPublisher
    {
        Task PublishOrderAsync(OrderDto order, CancellationToken cancellationToken = default);
    }
}