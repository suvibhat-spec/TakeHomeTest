using ECommerce.UserService.Model;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.UserService.Data {
    public class UserDbContext(DbContextOptions<UserDbContext> dbContextOptions): DbContext(dbContextOptions)
    {
        public DbSet<User> Users => Set<User>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.Property(x => x.Name)
                    .IsRequired();

                entity.Property(x => x.Email)
                    .IsRequired();
            });
        }
    }
}