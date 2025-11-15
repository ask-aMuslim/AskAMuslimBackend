namespace AskAMuslimBackend.Application.FunctionalTests;

public static class TestDatabaseFactory
{
    public static async Task<ITestDatabase> CreateAsync()
    {
        // Prefer Testcontainers when available, otherwise fallback to local SQL using appsettings.json.
        // You can force Testcontainers by setting env var USE_TESTCONTAINERS=true.
        var useTestcontainers = Environment.GetEnvironmentVariable("USE_TESTCONTAINERS");

        if (string.Equals(useTestcontainers, "true", StringComparison.OrdinalIgnoreCase))
        {
            var forced = new SqlTestcontainersTestDatabase();
            await forced.InitialiseAsync();
            return forced;
        }

        try
        {
            var containerDb = new SqlTestcontainersTestDatabase();
            await containerDb.InitialiseAsync();
            return containerDb;
        }
        catch
        {
            var localDb = new SqlTestDatabase();
            await localDb.InitialiseAsync();
            return localDb;
        }
    }
}
