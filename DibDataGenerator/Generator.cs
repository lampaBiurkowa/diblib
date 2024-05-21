using DibBase.ModelBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace DibDataGenerator;

public class EntityPopulator
{
    static Dictionary<string, long> uniqueIntSequence = new();

    public static async Task Build(DbContext dbContext)
    {
        var entityTypes = dbContext.Model.GetEntityTypes()
            .Where(t => typeof(Entity).IsAssignableFrom(t.ClrType) && t.ClrType != typeof(Entity)).ToList();

        foreach (var entityType in entityTypes)
        {
            var dbSetMethod = typeof(DbContext).GetMethods().FirstOrDefault(x => x.Name == nameof(dbContext.Set) && x.GetParameters().Length == 0)?.MakeGenericMethod(entityType.ClrType);
            var dbSet = dbSetMethod?.Invoke(dbContext, null);

            for (int i = 0; i < 10; i++)
            {
                var entity = Activator.CreateInstance(entityType.ClrType);
                PopulateRandomData(entity, entityType, dbContext);
                dbSet?.GetType().GetMethod("Add")?.Invoke(dbSet, new[] { entity });
                dbContext.SaveChanges();
            }
        }
    }

    static void PopulateRandomData(object entity, IEntityType entityType, DbContext dbContext)
    {
        var random = new Random();

        var props = entityType.GetProperties();
        foreach (var property in props)
        {
            if (property.IsPrimaryKey())
                continue;

            if (property.IsForeignKey())
            {
                var subproperty = ((RuntimeProperty)property).ForeignKeys[0].PrincipalEntityType;
                var dbSetMethod = typeof(DbContext).GetMethods().FirstOrDefault(x => x.Name == nameof(dbContext.Set) && x.GetParameters().Length == 0)?.MakeGenericMethod(subproperty.ClrType);
                var dbSet = dbSetMethod?.Invoke(dbContext, null);
                var subentity = Activator.CreateInstance(subproperty.ClrType);
                PopulateRandomData(subentity, dbContext.Model.FindEntityType(subproperty.ClrType), dbContext);
                dbSet?.GetType().GetMethod("Add")?.Invoke(dbSet, new[] { subentity });
                dbContext.SaveChanges();
                var propertyInfo = entity.GetType().GetProperty(property.Name);
                propertyInfo?.SetValue(entity, ((Entity)subentity).Id);
            }
            else
            {
                var randomValue = GenerateRandomValue(property.ClrType);
                if (HasUniqueConstraint(property, dbContext) && property.ClrType == typeof(Int64))
                {
                    var key = $"{entity.GetType()}:{property.Name}";
                    if (!uniqueIntSequence.ContainsKey(key))
                        uniqueIntSequence.Add(key, 1);

                    randomValue = uniqueIntSequence[key];
                    uniqueIntSequence[key]++;
                }
                
                var propertyInfo = entity.GetType().GetProperty(property.Name);
                propertyInfo?.SetValue(entity, randomValue);
            }
        }
    }

    static object GenerateRandomValue(Type propertyType)
    {
        var random = new Random();
        if (propertyType == typeof(string))
        {
            return Guid.NewGuid().ToString().Substring(0, 8);
        }
        else if (propertyType == typeof(int) || propertyType == typeof(long) || propertyType == typeof(short) || propertyType == typeof(byte))
        {
            return random.Next(1, 100);
        }
        else if (propertyType == typeof(DateTime))
        {
            return DateTime.Now.AddDays(random.Next(-365, 365));
        }
        else if (propertyType == typeof(bool))
        {
            return random.Next(2) == 1 ? true : false;
        }
        else if (propertyType == typeof(float))
        {
            return random.Next(1, 10000) / 100f;
        }
        else if (propertyType == typeof(double))
        {
            return (double)(random.Next(1, 10000) / 100f);
        }

        return null;
    }

    static bool HasUniqueConstraint(IProperty property, DbContext dbContext)
    {
        var entityType = dbContext.Model.GetEntityTypes().FirstOrDefault(t => t.ClrType == property.DeclaringType.ClrType);
        var uniqueConstraint = entityType?.FindIndex(property);
        return uniqueConstraint != null;
    }
}