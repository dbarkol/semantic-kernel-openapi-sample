using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Sprockets.Orders.Api.Models;

namespace Sprockets.Orders.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        /// <summary>
        /// Retrieves all orders.
        /// </summary>
        /// <returns>A list of orders.</returns>
        /// <response code="200">Returns the list of orders.</response>
        /// <response code="404">If no orders are found.</response>        
        [HttpGet]
        [SwaggerOperation(OperationId="GetAllOrders", Summary = "Get all orders", Description = "Returns a list of orders.")]
        [ProducesResponseType(typeof(IEnumerable<Order>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Produces("application/json")]
        public IActionResult GetOrders()
        {
            var orders = new[]
            {
                new { Id = 1, Product = "Widget", Quantity = 10 },
                new { Id = 2, Product = "Gadget", Quantity = 5 }
            };

            if (orders.Length == 0)
            {
                return NotFound();
            }

            return Ok(orders);
        }

        /// <summary>
        /// Creates new orders.
        /// </summary>
        /// <param name="orders">The collection of orders to create.</param>
        /// <returns>The created orders.</returns>
        /// <response code="201">Returns the newly created orders.</response>
        /// <response code="400">If the orders collection is null or empty.</response>
        [HttpPost]
        [SwaggerOperation(OperationId="CreateOrders", Summary = "Creates new orders", Description = "Creates a new collection of orders.")]
        [ProducesResponseType(typeof(IEnumerable<Order>), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Produces("application/json")]
        public IActionResult CreateOrders([FromBody] IEnumerable<Order> orders)
        {
            if (orders == null || !orders.Any())
            {
                return BadRequest("Orders collection is null or empty.");
            }

            // Here you would typically add the orders to a database or other storage
            // For this example, we'll just return the orders that were passed in

            return CreatedAtAction(nameof(GetOrders), new { }, orders);
        }

        // /// <summary>
        // /// Retrieves a specific order by ID.
        // /// </summary>
        // /// <param name="id">The ID of the order to retrieve.</param>
        // /// <returns>The order with the specified ID.</returns>
        // /// <response code="200">Returns the order with the specified ID.</response>
        // /// <response code="404">If the order is not found.</response>
        [HttpGet("{id}")]
        [SwaggerOperation(OperationId="GetOrderById", Summary = "Retrieves a specific order by ID", Description = "Gets the details of a specific order by its ID.")]
        [ProducesResponseType(typeof(Order), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Produces("application/json")]
        public IActionResult GetOrder(int id)
        {
            var order = new { Id = id, Product = "Widget", Quantity = 10 };

            if (order == null)
            {
                return NotFound();
            }

            return Ok(order);
        }        
    }
}