// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using Microsoft.Data.SqlClient;

namespace cCoder.Assets.UI.Tests.Infrastructure;

internal sealed class AcceptanceDatabaseScope(
    params string[] connectionStrings) : IAsyncDisposable
{
    public ValueTask DisposeAsync()
    {
        SqlConnection.ClearAllPools();

        foreach (string connectionString in connectionStrings)
        {
            DropDatabase(connectionString: connectionString);
        }

        return ValueTask.CompletedTask;
    }

    private static void DropDatabase(string connectionString)
    {
        SqlConnectionStringBuilder builder = new(connectionString)
        {
            Encrypt = true,
            TrustServerCertificate = true
        };

        string databaseName = builder.InitialCatalog;

        if (string.IsNullOrWhiteSpace(value: databaseName)
            || !databaseName.Contains(
                value: "assets-ui-acceptance",
                comparisonType: StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Refusing to drop non-acceptance database '{databaseName}'.");
        }

        builder.InitialCatalog = "master";

        using SqlConnection connection = new(builder.ConnectionString);
        connection.Open();

        using SqlCommand command = connection.CreateCommand();

        command.CommandText = """
            IF DB_ID(@databaseName) IS NOT NULL
            BEGIN
                DECLARE @sql nvarchar(max) =
                    N'ALTER DATABASE [' + REPLACE(@databaseName, ']', ']]')
                    + N'] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE ['
                    + REPLACE(@databaseName, ']', ']]') + N']';
                EXEC(@sql);
            END
            """;

        _ = command.Parameters.AddWithValue(
            parameterName: "@databaseName",
            value: databaseName);

        command.ExecuteNonQuery();
    }
}