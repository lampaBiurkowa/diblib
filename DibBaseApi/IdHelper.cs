using DibBase.ModelBase;
using DibBase.Obfuscation;

namespace DibBaseApi;

public static class IdHelper
{
    public static T HidePrivateId<T>(T entity) where T : Entity
    {
        entity.Id = default;

        var props = entity.GetType().GetProperties()
            .Where(prop => Attribute.IsDefined(prop, typeof(DsLongAttribute)));

        foreach (var p in props)
            p.SetValue(entity, default);

        var entityProps = entity.GetType().GetProperties()
            .Where(prop => typeof(Entity).IsAssignableFrom(prop.PropertyType) && prop.PropertyType != typeof(Entity));

        foreach (var p in entityProps)
            if (p.GetValue(entity) is Entity nestedEntity)
                HidePrivateId(nestedEntity);

        return entity;
    }
}