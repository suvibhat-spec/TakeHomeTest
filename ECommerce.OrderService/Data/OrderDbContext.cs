using ECommerce.OrderService.Model;
using ECommerce.OrderService.Models;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.OrderService.Data {
    public class OrderDbContext(DbContextOptions<OrderDbContext> dbContextOptions): DbContext(dbContextOptions)
    {
        public DbSet<Order> Orders => Set<Order>();

        public DbSet<UserRef> UserRefs => Set<UserRef>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Order>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.Property(x => x.UserId)
                    .IsRequired();

                entity.Property(x => x.Product)
                    .IsRequired();

                entity.Property(x => x.Quantity)
                    .IsRequired();

                entity.Property(x => x.Price)
                    .IsRequired();

            });

            modelBuilder.Entity<UserRef>(entity =>
            {
                entity.HasKey(x => x.Id);
            });
        }
    }
}