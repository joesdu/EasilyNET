using EasilyNET.Core.Domains;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;

namespace EasilyNET.Migrate.Console.Test.Model;

/// <summary>
/// </summary>
public class User : Entity<Guid>
{
    public string Name { get; set; } = default!;

    public int Age { get; }

    /// <summary>
    /// </summary>
    public ICollection<Role> Roles { get; set; } = new List<Role>();
}

/// <summary>
/// </summary>
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Name).IsRequired().HasMaxLength(50);
        builder.HasMany(o => o.Roles).WithOne(r => r.User).HasForeignKey(r => r.UserId);
        builder.HasIndex(o => o.Name);
        builder.HasIndex(o => o.Age);
        builder.ToTable("User");
    }
}