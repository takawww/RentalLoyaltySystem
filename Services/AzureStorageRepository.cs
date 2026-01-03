using Azure.Data.Tables;
using Microsoft.Extensions.Configuration;

namespace RentalLoyaltySystem.Services;

public class AzureStorageRepository
{
    private readonly IConfiguration _configuration;

    public AzureStorageRepository(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<T?> GetAsync<T>(string tableName, string partitionKey, string rowKey) where T : class, ITableEntity, new()
    {
        try
        {
            var tableClient = GetTableClient(tableName);
            var entity = await tableClient.GetEntityAsync<T>(partitionKey, rowKey);
            return entity.Value;
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task<List<T>> QueryAsync<T>(string tableName, string filter) where T : class, ITableEntity, new()
    {
        try
        {
            var tableClient = GetTableClient(tableName);
            var results = new List<T>();

            await foreach (var entity in tableClient.QueryAsync<T>(filter))
            {
                results.Add(entity);
            }

            return results;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Query error: {ex.Message}");
            return new List<T>();
        }
    }

    private TableClient GetTableClient(string tableName)
    {
        var accountName = _configuration["AzureStorage:AccountName"];
        var accountKey = _configuration["AzureStorage:AccountKey"];

        if (string.IsNullOrWhiteSpace(accountName) || string.IsNullOrWhiteSpace(accountKey))
        {
            throw new InvalidOperationException("Azure Storage account name or key is not configured.");
        }

        var tableUri = new Uri($"https://{accountName}.table.core.windows.net");
        var credential = new TableSharedKeyCredential(accountName, accountKey);
        return new TableClient(tableUri, tableName, credential);
    }
}
