using DibBase.Extensions;
using DibBase.ModelBase;
using DibBase.Models;
using DibBase.Obfuscation;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Linq.Expressions;
using System.Text.Json;

namespace DibBase.Infrastructure;

public class Repository<T>(DbContext context) where T : Entity
{
    readonly DbContext _context = context;

    public async Task RegisterEvent(object obj, CancellationToken ct)
    {
        var e = new Event()
        {
            CreatedAt = DateTime.Now,
            Payload = JsonSerializer.Serialize(obj),
            Name = obj.GetType().FullName ?? obj.GetType().Name,
            IsPublished = false
        };

        await _context.Set<Event>().AddAsync(e, ct);
    }

    public async Task<T?> GetById(long id, CancellationToken ct)
    {
        IQueryable<T> query = _context.Set<T>();
        if (typeof(ISoftDelete).IsAssignableFrom(typeof(T)))
            query = query.Cast<ISoftDelete>().Where(e => !e.IsDeleted).Cast<T>();

        var entity = await query.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (entity != null) entity = FillDsIds(entity);

        return entity;
    }

    public async Task<List<T>> GetByIds(IEnumerable<long> ids, CancellationToken ct)
    {
        IQueryable<T> query = _context.Set<T>();
        if (typeof(ISoftDelete).IsAssignableFrom(typeof(T)))
            query = query.Cast<ISoftDelete>().Where(e => !e.IsDeleted).Cast<T>();

        var results = await query.Where(e => ids.Contains(e.Id)).ToListAsync(ct);
        return results.Select(FillDsIds).ToList();
    }

    public async Task<List<T>> GetAll(int skip = 0, int take = 1000, Expression<Func<T, bool>>? restrict = null, CancellationToken ct = default)
    {
        IQueryable<T> query = _context.Set<T>();
        if (typeof(ISoftDelete).IsAssignableFrom(typeof(T)))
            query = query.Cast<ISoftDelete>().Where(e => !e.IsDeleted).Cast<T>();

        if (restrict != null) query = query.Where(restrict);

        return (await query.Skip(skip).Take(take).ToListAsync(ct)).Select(FillDsIds).ToList();
    }

    public async Task InsertAsync(T entity, CancellationToken ct)
    {
        var timeStamp = DateTime.Now;
        Repository<T>.SetTimestamps(entity, timeStamp);
        await AuditChanges(entity, timeStamp, ct);
        await _context.Set<T>().AddAsync(entity, ct);
    }

    public async Task UpdateAsync(T entity, CancellationToken ct)
    {
        var timeStamp = DateTime.Now;
        Repository<T>.SetUpdatedTimestamp(entity, timeStamp);

        entity = SetIdsFromDsIds(entity);

        await AuditChanges(entity, timeStamp, ct);
        _context.Entry(entity).State = EntityState.Modified;
    }

    public async Task DeleteAsync(long id, CancellationToken ct)
    {
        var entity = await GetById(id, ct);
        if (entity is ISoftDelete sd)
        {
            sd.IsDeleted = true;
            await UpdateAsync(entity, ct);
        }
        else if (entity != null)
            _context.Set<T>().Remove(entity);
    }

    public async Task CommitAsync(CancellationToken ct) => await _context.SaveChangesAsync(ct);

    static void SetTimestamps(T entity, DateTime timeStamp)
    {
        if (entity is ITimeStamped tsEntity)
        {
            tsEntity.CreatedAt = timeStamp;
            tsEntity.UpdatedAt = timeStamp;
        }
    }

    static void SetUpdatedTimestamp(T entity, DateTime timeStamp)
    {
        if (entity is ITimeStamped tsEntity)
            tsEntity.UpdatedAt = timeStamp;
    }

    async Task AuditChanges(T entity, DateTime timeStamp, CancellationToken ct)
    {
        if (entity is not IAudited a)
            return;
        
        var entry = _context.Entry(entity);
        var changedProperties = entry.Properties.Where(p => p.IsModified).ToList();
        foreach (var prop in changedProperties)
        {
            if (!a.GetFieldsToAudit().Contains(prop.Metadata.Name))
                continue;

            var audit = new Audit()
            {
                ChangedType = entity.GetTypeName(),
                ChangedId = entity.Id,
                ChangedField = prop.Metadata.Name,
                OldValue = prop.OriginalValue?.ToString(),
                NewValue = prop.CurrentValue?.ToString(),
                ChangedAt = timeStamp
            };
            await _context.Set<Audit>().AddAsync(audit, ct);
        }
    }

    T FillDsIds(T entity)
    {
        var props = entity.GetType().GetProperties().Where(x => x.IsDefined(typeof(DsGuidAttribute), false));
        foreach (var p in props)
        {
            var attributeData = p.GetCustomAttributesData().FirstOrDefault(x => x.AttributeType == typeof(DsGuidAttribute));
            if (attributeData != null)
            {
                var navigationProperty = attributeData.ConstructorArguments.FirstOrDefault().Value?.ToString();
                if (string.IsNullOrEmpty(navigationProperty)) break;
                
                if (p.PropertyType == typeof(Guid))
                {
                    var refId = (long?)_context.Entry(entity).Property($"{navigationProperty}Id").CurrentValue;
                    if (refId != null && navigationProperty != null)
                        p.SetValue(entity, ((long)refId).Obfuscate(navigationProperty));
                }
            }
        }

        return entity;
    }

    T SetIdsFromDsIds(T entity)
    {
        var props = entity.GetType().GetProperties().Where(x => x.IsDefined(typeof(DsGuidAttribute), false));
        foreach (var p in props)
        {
            var attributeData = p.GetCustomAttributesData().FirstOrDefault(x => x.AttributeType == typeof(DsGuidAttribute));
            if (attributeData != null)
            {
                var navigationProperty = attributeData.ConstructorArguments.FirstOrDefault().Value?.ToString();
                if (string.IsNullOrEmpty(navigationProperty)) break;
                
                if (p.PropertyType == typeof(Guid))
                {
                    var value = (Guid?)p.GetValue(entity);
                    _context.Entry(entity).Property($"{navigationProperty}Id").CurrentValue = value?.Deobfuscate().Id;
                }
            }
        }

        return entity;
    }
}
