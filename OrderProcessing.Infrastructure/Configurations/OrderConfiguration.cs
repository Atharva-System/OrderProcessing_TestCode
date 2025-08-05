using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderProcessing.Domain.Entities;
using OrderProcessing.Domain.ValueObjects;

namespace OrderProcessing.Infrastructure.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");
        
        builder.HasKey(o => o.Id);
        
        builder.Property(o => o.Id)
            .ValueGeneratedOnAdd();

        // Configure OrderNumber as a value object
        builder.OwnsOne(o => o.OrderNumber, on =>
        {
            on.Property(x => x.Value)
                .HasColumnName("OrderNumber")
                .HasMaxLength(30)
                .IsRequired();
                
            on.HasIndex(x => x.Value)
                .IsUnique()
                .HasDatabaseName("IX_Orders_OrderNumber");
        });

        // Configure invoice details
        builder.Property(o => o.InvoiceEmailAddress)
            .HasColumnName("InvoiceEmailAddress")
            .HasMaxLength(255)
            .IsRequired();

        builder.OwnsOne(o => o.InvoiceAddress, ia =>
        {
            ia.Property(x => x.Value)
                .HasColumnName("InvoiceAddress")
                .HasMaxLength(500)
                .IsRequired();
        });

        builder.OwnsOne(o => o.InvoiceCreditCardNumber, cc =>
        {
            cc.Property(x => x.Value)
                .HasColumnName("InvoiceCreditCardNumber")
                .HasMaxLength(25)
                .IsRequired();
        });

        builder.Property(o => o.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(o => o.Notes)
            .HasMaxLength(1000);

        builder.Property(o => o.CreatedAt)
            .IsRequired();

        builder.Property(o => o.UpdatedAt);

        // Ignore computed properties
        builder.Ignore(o => o.TotalAmount);
        builder.Ignore(o => o.Items);
        
        // Configure OrderItems as owned entities in separate table
        builder.OwnsMany<OrderItem>("_items", oi =>
        {
            oi.ToTable("OrderItems");
            oi.WithOwner().HasForeignKey("OrderId");
            oi.Property<Guid>("Id").ValueGeneratedOnAdd();
            oi.HasKey("Id");
            
            oi.Property(x => x.ProductId)
                .HasMaxLength(50)
                .IsRequired();
                
            oi.Property(x => x.ProductName)
                .HasMaxLength(200)
                .IsRequired();
                
            oi.Property(x => x.ProductAmount)
                .IsRequired();
                
            oi.Property(x => x.ProductPrice)
                .HasColumnType("decimal(18,2)")
                .IsRequired();
                
            oi.Ignore(x => x.TotalPrice); // Computed property
        });
    }
}
