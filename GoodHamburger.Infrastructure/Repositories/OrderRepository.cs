using Ardalis.Specification.EntityFrameworkCore;
using GoodHamburger.Application.Interfaces;
using GoodHamburger.Domain.Orders;
using GoodHamburger.Infrastructure.Data;

namespace GoodHamburger.Infrastructure.Repositories;

public class OrderRepository : RepositoryBase<Order>, IOrderRepository
{
    public OrderRepository(AppDbContext dbContext) : base(dbContext) { }
}
