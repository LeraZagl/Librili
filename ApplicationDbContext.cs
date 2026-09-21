using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MyLibrary.Models;

namespace MyLibrary.Data
{
    // Если у вас уже есть свой ApplicationDbContext (созданный мастером VS
    // с Individual Accounts), НЕ создавайте новый файл — просто добавьте
    // в существующий класс строку "public DbSet<Book> Books => Set<Book>();"
    // и блок OnModelCreating ниже.
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Book> Books => Set<Book>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Book>()
                .HasOne(b => b.User)
                .WithMany(u => u.Books)
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Book>()
                .HasIndex(b => b.UserId);
        }
    }
}
