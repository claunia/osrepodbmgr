// See https://aka.ms/new-console-template for more information

using Microsoft.Data.Sqlite;

namespace osrepomgr.Cli;

file static class Program
{
    static void Main(string[] args)
    {
        string? repositoryPath = null;
        string? databasePath = null;

        // Parse command line arguments
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i].ToLower())
            {
                case "--repository":
                case "-r":
                    if (i + 1 < args.Length)
                    {
                        repositoryPath = args[++i];
                    }

                    break;

                case "--database":
                case "-d":
                    if (i + 1 < args.Length)
                    {
                        databasePath = args[++i];
                    }

                    break;
            }
        }

        // Validate arguments
        if (string.IsNullOrEmpty(repositoryPath) || string.IsNullOrEmpty(databasePath))
        {
            ShowHelp();
            Environment.Exit(1);
        }

        // Validate repository path exists as a folder
        if (!Directory.Exists(repositoryPath))
        {
            Console.Error.WriteLine($"Error: Repository folder does not exist: {repositoryPath}");
            ShowHelp();
            Environment.Exit(1);
        }

        // Validate database path exists as a file
        if (!File.Exists(databasePath))
        {
            Console.Error.WriteLine($"Error: Database file does not exist: {databasePath}");
            ShowHelp();
            Environment.Exit(1);
        }

        Console.WriteLine("osrepomgr CLI Application");
        Console.WriteLine($"Repository: {repositoryPath}");
        Console.WriteLine($"Database: {databasePath}");

        // Open SQLite database
        try
        {
            string connectionString = $"Data Source={databasePath};Mode=ReadOnly";
            using var connection = new SqliteConnection(connectionString);

            connection.Open();
            Console.WriteLine("Successfully opened SQLite database");

            // Validate osrepodbmgr table exists and has correct version
            ValidateDatabase(connection);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: Failed to open database: {ex.Message}");
            Environment.Exit(1);
        }
    }

    static void ShowHelp()
    {
        Console.WriteLine("osrepomgr CLI Application");
        Console.WriteLine();
        Console.WriteLine("Usage: osrepomgr [OPTIONS]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --repository, -r <path>  Path to the repository folder (required)");
        Console.WriteLine("  --database, -d <path>    Path to the database file (required)");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  osrepomgr --repository /path/to/repo --database /path/to/db.db");
        Console.WriteLine("  osrepomgr -r /path/to/repo -d /path/to/db.db");
        Console.WriteLine("  osrepomgr -d /path/to/db.db -r /path/to/repo");
    }

    static void ValidateDatabase(SqliteConnection connection)
    {
        try
        {
            // Check if osrepodbmgr table exists
            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT COUNT(*) FROM sqlite_master
                WHERE type='table' AND name='osrepodbmgr'";
            var tableExists = ((long?)command.ExecuteScalar()) ?? 0;

            if (tableExists == 0)
            {
                Console.Error.WriteLine("Error: osrepodbmgr table not found in database");
                Environment.Exit(1);
            }

            // Count rows in osrepodbmgr table
            using var countCommand = connection.CreateCommand();
            countCommand.CommandText = "SELECT COUNT(*) FROM osrepodbmgr";
            var rowCount = ((long?)countCommand.ExecuteScalar()) ?? 0;

            if (rowCount != 1)
            {
                Console.Error.WriteLine($"Error: osrepodbmgr table should contain exactly 1 row, but found {rowCount}");
                Environment.Exit(1);
            }

            // Get the version value
            using var versionCommand = connection.CreateCommand();
            versionCommand.CommandText = "SELECT version FROM osrepodbmgr LIMIT 1";
            var version = versionCommand.ExecuteScalar();

            if (version == null)
            {
                Console.Error.WriteLine("Error: version column not found or is NULL in osrepodbmgr table");
                Environment.Exit(1);
            }

            if (!int.TryParse(version.ToString(), out int versionValue) || versionValue != 1)
            {
                Console.Error.WriteLine($"Error: osrepodbmgr table version must be 1, but found {version}");
                Environment.Exit(1);
            }

            Console.WriteLine($"OS Repo DB Manager version: {versionValue}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: Failed to validate database: {ex.Message}");
            Environment.Exit(1);
        }
    }
}
