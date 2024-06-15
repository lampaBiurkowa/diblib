using System.Linq.Expressions;
using DibBase.ModelBase;
using Microsoft.EntityFrameworkCore;

namespace DibBase.Extensions;

public static class IQueryableExtensions
{
    public static IQueryable<T> ApplyExpand<T>(
        this IQueryable<T> query,
        IEnumerable<Expression<Func<T, object?>>> expand) where T : Entity
    {
        foreach (var e in expand)
            query = query.Include(e);

        return query;
    }
}