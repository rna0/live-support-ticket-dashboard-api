using System.Collections.Concurrent;
using System.Reflection;

namespace LiveSupportDashboard.Infrastructure.Services;

public interface ISqlQueryLoader
{
    string GetQuery(string category, string queryName);
    Task<string> GetQueryAsync(string category, string queryName);
}

public sealed class SqlQueryLoader : ISqlQueryLoader
{
    private readonly ConcurrentDictionary<string, string> _queryCache = new();
    private readonly string _baseQueriesPath;

    public SqlQueryLoader()
    {
        // Get the base directory where the application is running
        var assemblyLocation = Assembly.GetExecutingAssembly().Location;
        var assemblyDirectory = Path.GetDirectoryName(assemblyLocation)!;

        // Navigate to the Infrastructure/Queries folder
        _baseQueriesPath = Path.Combine(assemblyDirectory, "Infrastructure", "Queries");

        // If not found in bin folder, try the source folder structure
        if (!Directory.Exists(_baseQueriesPath))
        {
            var sourceRoot = FindSourceRoot(assemblyDirectory);
            if (sourceRoot != null)
            {
                _baseQueriesPath = Path.Combine(sourceRoot, "Infrastructure", "Queries");
            }
        }
    }

    public string GetQuery(string category, string queryName)
    {
        var cacheKey = $"{category}.{queryName}";

        return _queryCache.GetOrAdd(cacheKey, _ =>
        {
            var queryPath = Path.Combine(_baseQueriesPath, category, $"{queryName}.sql");

            if (File.Exists(queryPath))
                return File.ReadAllText(queryPath);
            throw new FileNotFoundException($"SQL query file not found: {queryPath}");
        });
    }

    public async Task<string> GetQueryAsync(string category, string queryName)
    {
        var cacheKey = $"{category}.{queryName}";

        if (_queryCache.TryGetValue(cacheKey, out var cachedQuery))
        {
            return cachedQuery;
        }

        var queryPath = Path.Combine(_baseQueriesPath, category, $"{queryName}.sql");

        if (!File.Exists(queryPath))
        {
            throw new FileNotFoundException($"SQL query file not found: {queryPath}");
        }

        var query = await File.ReadAllTextAsync(queryPath);
        _queryCache.TryAdd(cacheKey, query);

        return query;
    }

    private static string? FindSourceRoot(string startPath)
    {
        var current = new DirectoryInfo(startPath);

        while (current != null)
        {
            // Look for Infrastructure/Queries folder or .csproj file to identify source root
            if (Directory.Exists(Path.Combine(current.FullName, "Infrastructure", "Queries")) ||
                current.GetFiles("*.csproj").Length != 0)
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        return null;
    }
}
