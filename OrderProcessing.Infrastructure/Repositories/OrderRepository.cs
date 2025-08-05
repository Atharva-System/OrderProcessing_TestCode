using Microsoft.EntityFrameworkCore;
using OrderProcessing.Domain.Entities;
using OrderProcessing.Domain.Interfaces;
using OrderProcessing.Domain.ValueObjects;
using OrderProcessing.Infrastructure.Data;

namespace OrderProcessing.Infrastructure.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly OrderProcessingDbContext _context;

    public OrderRepository(OrderProcessingDbContext context)
    {
        _context = context;
    }

    public async Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .Include("_items")
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
    }

    public async Task<Order?> GetByOrderNumberAsync(OrderNumber orderNumber, CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .Include("_items")
            .FirstOrDefaultAsync(o => o.OrderNumber!.Value == orderNumber.Value, cancellationToken);
    }

    public async Task<IEnumerable<Order>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .Include("_items")
            .ToListAsync(cancellationToken);
    }

    public async Task<Order> AddAsync(Order order, CancellationToken cancellationToken = default)
    {
        _context.Orders.Add(order);
        await _context.SaveChangesAsync(cancellationToken);
        return order;
    }

    public async Task UpdateAsync(Order order, CancellationToken cancellationToken = default)
    {
        _context.Orders.Update(order);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Order order, CancellationToken cancellationToken = default)
    {
        _context.Orders.Remove(order);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(OrderNumber orderNumber, CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .AnyAsync(o => o.OrderNumber!.Value == orderNumber.Value, cancellationToken);
    }
}
