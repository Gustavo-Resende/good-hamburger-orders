using GoodHamburger.Domain.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoodHamburger.Infrastructure.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Subtotal).HasColumnType("numeric(18,2)");
        builder.Property(x => x.Discount).HasColumnType("numeric(18,2)");
        builder.Property(x => x.Total).HasColumnType("numeric(18,2)");

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);

        builder.HasMany(x => x.Items)
               .WithOne()
               .HasForeignKey("OrderId")
               .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Items).AutoInclude();
    }
}
