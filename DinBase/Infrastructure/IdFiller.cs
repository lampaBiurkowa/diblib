using System.Collections;
using DibBase.Extensions;
using DibBase.ModelBase;
using DibBase.Obfuscation;
using Microsoft.EntityFrameworkCore;

namespace DibBase.Infrastructure;

public static class IdFiller
{
    public static void FillDsIds(Entity entity, DbContext ctx)
    {
        entity.Guid = entity.Obfuscate();
        
        var props = entity.GetType().GetProperties().Where(x => x.IsDefined(typeof(DsGuidAttribute), false));
        foreach (var p in props)
        {
            var attributeData = p.GetCustomAttributesData().FirstOrDefault(x => x.AttributeType == typeof(DsGuidAttribute));
            if (attributeData != null)
            {
                var navigationProperty = attributeData.ConstructorArguments.FirstOrDefault().Value?.ToString();
                if (string.IsNullOrEmpty(navigationProperty)) break;
                
                if (p.PropertyType == typeof(Guid) || (p.PropertyType == typeof(Guid?)))
                {
                    var refId = (long?)ctx.Entry(entity).Property($"{navigationProperty}Id").CurrentValue;
                    if (refId != null)
                    {
                        var navigationPropertyType = entity.GetType().GetProperty(navigationProperty)?.PropertyType;
                        if (navigationPropertyType != null)
                        {
                            var typeForObfuscation = navigationPropertyType;
                            if (typeof(IDerivedKey).IsAssignableFrom(navigationPropertyType))
                            {
                                var parentType = GetParentType(navigationPropertyType);
                                if (parentType != null)
                                    typeForObfuscation = parentType;
                            }

                            p.SetValue(entity, ((long)refId).Obfuscate(typeForObfuscation.Name));
                        }
                    }
                }
            }
        }

        var listProps = entity.GetType().GetProperties().Where(x => x.IsDefined(typeof(DsGuidListAttribute), false));
        foreach (var p in listProps)
            if (typeof(IEnumerable).IsAssignableFrom(p.PropertyType) && p.PropertyType.IsGenericType)
                if (typeof(Entity).IsAssignableFrom(p.PropertyType.GenericTypeArguments[0]))
                    if (p.GetValue(entity) is IEnumerable<Entity> collection)
                        foreach (var item in collection)
                            FillDsIds(item, ctx);

        var nestedProps = entity.GetType().GetProperties().Where(x => typeof(Entity).IsAssignableFrom(x.PropertyType));
        foreach (var p in nestedProps)
        {
            var nestedEntity = p.GetValue(entity);
            if (nestedEntity != null && nestedEntity is Entity nestedEntityInstance)
            {
                FillDsIds(nestedEntityInstance, ctx);
                p.SetValue(entity, nestedEntityInstance);
            }
        }
    }

    public static void SetIdsFromDsIds(Entity entity, DbContext ctx)
    {
        var props = entity.GetType().GetProperties().Where(x => x.IsDefined(typeof(DsGuidAttribute), false));
        foreach (var p in props)
        {
            var attributeData = p.GetCustomAttributesData().FirstOrDefault(x => x.AttributeType == typeof(DsGuidAttribute));
            if (attributeData != null)
            {
                var navigationProperty = attributeData.ConstructorArguments.FirstOrDefault().Value?.ToString();
                if (string.IsNullOrEmpty(navigationProperty)) break;
                
                if (p.PropertyType == typeof(Guid) || (p.PropertyType == typeof(Guid?) && p.GetValue(entity) != null))
                {
                    var value = (Guid?)p.GetValue(entity);
                    if (value != default(Guid))
                        ctx.Entry(entity).Property($"{navigationProperty}Id").CurrentValue = value?.Deobfuscate().Id;
                }
            }
        }

        var nestedProps = entity.GetType().GetProperties().Where(x => typeof(Entity).IsAssignableFrom(x.PropertyType));
        foreach (var p in nestedProps)
        {
            var nestedEntity = p.GetValue(entity);
            if (nestedEntity != null && nestedEntity is Entity nestedEntityInstance)
            {
                SetIdsFromDsIds(nestedEntityInstance, ctx);
                p.SetValue(entity, nestedEntityInstance);
            }
        }
    }

    static Type? GetParentType(Type entityType)
    {
        var parentType = entityType.BaseType;
        if (parentType != null && typeof(Entity).IsAssignableFrom(parentType))
            return parentType;

        return null;
    }
}