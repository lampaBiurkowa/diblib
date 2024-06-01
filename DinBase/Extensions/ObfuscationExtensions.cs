using DibBase.ModelBase;
using DibBase.Obfuscation;

namespace DibBase.Extensions;

public static class ObfuscationExtensions
{
    public static string GetTypeName(this Entity entity) => entity.GetType().Name;
    // public static Guid Obfuscate(this Entity entity) => IdObfuscator.ObfuscateId(entity.Id, entity.GetTypeName());
    public static ObfuscatedFields Deobfuscate(this Guid guid) => IdObfuscator.DecodeId(guid);
    public static Guid Obfuscate(this long id, string entityType) => IdObfuscator.ObfuscateId(id, entityType);
    public static Guid Obfuscate(this Entity entity)
    {
        string typeName;

        if (entity is IDerivedKey derivedKeyEntity)
            typeName = GetParentTypeName(derivedKeyEntity);
        else
            typeName = entity.GetTypeName();

        return IdObfuscator.ObfuscateId(entity.Id, typeName);
    }

    static string GetParentTypeName(IDerivedKey derivedKeyEntity)
    {
        var parentType = derivedKeyEntity.GetType().BaseType;
        if (parentType != null && typeof(Entity).IsAssignableFrom(parentType))
            return parentType.Name;

        throw new InvalidOperationException("Parent type is not valid.");
    }
}
