using DibBase.ModelBase;
using DibBase.Models;
using DibBase.Obfuscation;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace DibBase.Infrastructure;

public class DibContext : DbContext
{
    public DibContext() : base()
    {
        Database.EnsureCreated();
    }

    public DbSet<Event> Events { get; set; }
    public DbSet<Audit> Audits { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        var allEntities = modelBuilder.Model.GetEntityTypes().Where(x => x.Name != nameof(DsGuid));
        foreach (var e in allEntities)
        {
            modelBuilder.Entity(e.ClrType).HasKey(nameof(Entity.Id));
            modelBuilder.Entity(e.ClrType).Property(nameof(Entity.Id)).IsRequired();
            modelBuilder.Entity(e.ClrType).Ignore(nameof(Entity.Guid));

            var propertiesToIgnore = e.ClrType.GetProperties().Where(p => p.IsDefined(typeof(DsGuidAttribute), false)).Select(p => p.Name);
            foreach (var propertyName in propertiesToIgnore)
                modelBuilder.Entity(e.ClrType).Ignore(propertyName);
        }

        var entitiesImplementingIName = modelBuilder.Model.GetEntityTypes().Where(e => typeof(INamed).IsAssignableFrom(e.ClrType));
        foreach (var e in entitiesImplementingIName)
            modelBuilder.Entity(e.ClrType).HasIndex(nameof(INamed.Name)).IsUnique();

        var entitiesImplementingITimestamped = modelBuilder.Model.GetEntityTypes().Where(e => typeof(ITimeStamped).IsAssignableFrom(e.ClrType));
        foreach (var e in entitiesImplementingITimestamped)
        {
            modelBuilder.Entity(e.ClrType).Property(nameof(ITimeStamped.CreatedAt)).IsRequired();
            modelBuilder.Entity(e.ClrType).Property(nameof(ITimeStamped.UpdatedAt)).IsRequired();
        }

        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }

    private object DsGuid()
    {
        throw new NotImplementedException();
    }
}
