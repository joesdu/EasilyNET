using EasilyNET.Core.Domains;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EasilyNET.Migrate.Console.Test.Model;

/// <summary>
/// </summary>
public sealed class Role : Entity<Guid>
{
    public string Name { get; set; } = default!;

    public User? User { get; set; }

    public Guid? UserId { get; set; }
}

/// <summary>
/// </summary>
public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Name).IsRequired().HasMaxLength(50);
        builder.ToTable("Role");
    }
}