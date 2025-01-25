using System.Data.Common;
using DibBase.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DibBase.Extensions;

public static class IServiceCollectionExtensions
{
    public static async Task<bool> MigratePostgresIfRequested<T>(
        this IServiceCollection services,
        IConfiguration conf,
        string dbName,
        DbConnection connection
    ) where T : DbContext
    {   
        var dbOptions = conf.GetSection(DsDbLibOptions.SECTION).Get<DsDbLibOptions>();
        if (dbOptions != null && dbOptions.Migrate)
        {
            await connection.OpenAsync();
            using var command = connection.CreateCommand();
            command.CommandText = $"SELECT 1 FROM pg_database WHERE datname = '{dbName}'";
            var exists = await command.ExecuteScalarAsync() != null;
            if (!exists)
            {
                command.CommandText = $"CREATE DATABASE \"{dbName}\"";
                await command.ExecuteNonQueryAsync();
            }
            await connection.CloseAsync();
            using var scope = services.BuildServiceProvider().CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<T>();
            var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
            if (pendingMigrations.Any())
                await context.Database.MigrateAsync();
            return true;
        }

        return dbOptions != null && dbOptions.Migrate;
    }
}
