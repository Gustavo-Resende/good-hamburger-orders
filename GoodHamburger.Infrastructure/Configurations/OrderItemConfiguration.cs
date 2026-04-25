using GoodHamburger.Domain.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoodHamburger.Infrastructure.Configurations;

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.Property<Guid>("Id");
        builder.HasKey("Id");

        builder.Property(x => x.MenuItemId).IsRequired();
        builder.Property(x => x.Name).IsRequired();
        builder.Property(x => x.Price).HasColumnType("numeric(18,2)");
        builder.Property(x => x.Category).IsRequired();
    }
}
