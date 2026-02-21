using System;
using System.Threading.Tasks;
using Demo.Miroservices.Azure.Order.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Demo.Miroservices.Azure.Shared.Models;

namespace Demo.Miroservices.Azure.Order.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderPublisher _publisher;

        public OrdersController(IOrderPublisher publisher)
        {
            _publisher = publisher;
        }

        /// <summary>
        /// Publish a new order to the Service Bus topic.
        /// </summary>
        /// <param name="order">Order payload. If Id or CreatedAt are not provided they will be set by the server.</param>
        /// <returns>Accepted with created Id.</returns>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status202Accepted)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Post([FromBody] OrderDto order)
        {
            if (order == null) return BadRequest();

            var toPublish = new OrderDto
            {
                Id =Guid.NewGuid(),
                CustomerId = order.CustomerId,
                Amount = order.Amount,
                CreatedAt = order.CreatedAt == default ? DateTimeOffset.UtcNow : order.CreatedAt
            };
            await _publisher.PublishOrderAsync(toPublish);

            return Accepted(new { toPublish.Id });
        }
    }
}