using AuthService.Constants;
using AuthService.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Data;

public class AuthServiceDbContext(DbContextOptions<AuthServiceDbContext> options)
    : IdentityDbContext<AuthServiceUser>(options)
{
    public DbSet<AuthError> AuthErrors => Set<AuthError>();
    public DbSet<AuthServiceUserAppSettings> AspNetUsersAppSettings =>
        Set<AuthServiceUserAppSettings>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<AuthServiceUserAppSettings>().ToTable("AspNetUsersAppSettings");

        builder
            .Entity<AuthServiceUser>()
            .HasOne(u => u.AppSettings)
            .WithOne(s => s.User)
            .HasForeignKey<AuthServiceUserAppSettings>(s => s.Id)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .Entity<AuthServiceUserAppSettings>()
            .Property(s => s.PreferredLanguageCode)
            .HasDefaultValue(PreferredLanguage.English);

        builder
            .Entity<AuthServiceUserAppSettings>()
            .Property(s => s.ColorThemeCode)
            .HasDefaultValue(ColorTheme.Light);
    }
}
