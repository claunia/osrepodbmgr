// See https://aka.ms/new-console-template for more information

using Microsoft.Data.Sqlite;
using Spectre.Console;

namespace osrepomgr.Cli;

file static class Program
{
    static void Main(string[] args)
    {
        string? repositoryPath = null;
        string? databasePath   = null;
        int?    dbId           = null;
        string? destination    = null;
        var     positionalArgs = new List<string>();

        // Parse command line arguments - first pass: named arguments
        int positionalStartIndex = args.Length;

        for(var i = 0; i < args.Length; i++)
        {
            string arg = args[i].ToLower();

            if(arg == "--repository" || arg == "-r")
            {
                if(i + 1 < args.Length) repositoryPath = args[++i];
            }
            else if(arg == "--database" || arg == "-d")
            {
                if(i + 1 < args.Length) databasePath = args[++i];
            }
            else if(arg.StartsWith("-"))
            {
                // Unknown flag - treat as error
                Console.Error.WriteLine($"Error: Unknown option: {arg}");
                ShowHelp();
                Environment.Exit(1);
            }
            else
            {
                // First non-flag argument marks the start of positional arguments
                if(positionalStartIndex == args.Length) positionalStartIndex = i;
            }
        }

        // Second pass: collect positional arguments (from first non-flag onwards)
        for(int i = positionalStartIndex; i < args.Length; i++) positionalArgs.Add(args[i]);

        // Process positional arguments: db-id (integer) and destination (string)
        // db-id is the second-to-last argument, destination is the last argument
        if(positionalArgs.Count >= 2)
        {
            string secondToLast = positionalArgs[positionalArgs.Count - 2];
            string last         = positionalArgs[positionalArgs.Count - 1];

            if(int.TryParse(secondToLast, out int parsedDbId))
            {
                dbId        = parsedDbId;
                destination = last;
            }
        }

        // Validate arguments
        if(string.IsNullOrEmpty(repositoryPath) ||
           string.IsNullOrEmpty(databasePath)   ||
           dbId == null                         ||
           string.IsNullOrEmpty(destination))
        {
            ShowHelp();
            Environment.Exit(1);
        }

        // Validate repository path exists as a folder
        if(!Directory.Exists(repositoryPath))
        {
            Console.Error.WriteLine($"Error: Repository folder does not exist: {repositoryPath}");
            ShowHelp();
            Environment.Exit(1);
        }

        // Validate database path exists as a file
        if(!File.Exists(databasePath))
        {
            Console.Error.WriteLine($"Error: Database file does not exist: {databasePath}");
            ShowHelp();
            Environment.Exit(1);
        }

        Console.WriteLine("osrepomgr CLI Application");
        Console.WriteLine($"Repository: {repositoryPath}");
        Console.WriteLine($"Database: {databasePath}");
        Console.WriteLine($"DB ID: {dbId}");
        Console.WriteLine($"Destination: {destination}");

        // Open SQLite database
        try
        {
            var       connectionString = $"Data Source={databasePath};Mode=ReadOnly";
            using var connection       = new SqliteConnection(connectionString);

            connection.Open();
            Console.WriteLine("Successfully opened SQLite database");

            // Validate osrepodbmgr table exists and has correct version
            ValidateDatabase(connection, dbId.Value);

            // Validate and display OS information
            ValidateOs(connection, dbId.Value);
        }
        catch(Exception ex)
        {
            Console.Error.WriteLine($"Error: Failed to open database: {ex.Message}");
            Environment.Exit(1);
        }
    }

    static void ShowHelp()
    {
        Console.WriteLine("osrepomgr CLI Application");
        Console.WriteLine();
        Console.WriteLine("Usage: osrepomgr [OPTIONS] <db-id> <destination>");
        Console.WriteLine();
        Console.WriteLine("Arguments:");
        Console.WriteLine("  <db-id>                   Database ID (integer, required)");
        Console.WriteLine("  <destination>             Destination path (string, required)");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --repository, -r <path>  Path to the repository folder (required)");
        Console.WriteLine("  --database, -d <path>    Path to the database file (required)");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  osrepomgr --repository /path/to/repo --database /path/to/db.db 123 /path/to/dest");
        Console.WriteLine("  osrepomgr -r /path/to/repo -d /path/to/db.db 456 /another/dest");
        Console.WriteLine("  osrepomgr -d /path/to/db.db -r /path/to/repo 789 /final/dest");
    }

    static void ValidateDatabase(SqliteConnection connection, int dbId)
    {
        try
        {
            // Check if osrepodbmgr table exists
            using SqliteCommand command = connection.CreateCommand();

            command.CommandText = @"
                SELECT COUNT(*) FROM sqlite_master
                WHERE type='table' AND name='osrepodbmgr'";

            long tableExists = (long?)command.ExecuteScalar() ?? 0;

            if(tableExists == 0)
            {
                Console.Error.WriteLine("Error: osrepodbmgr table not found in database");
                Environment.Exit(1);
            }

            // Count rows in osrepodbmgr table
            using SqliteCommand countCommand = connection.CreateCommand();
            countCommand.CommandText = "SELECT COUNT(*) FROM osrepodbmgr";
            long rowCount = (long?)countCommand.ExecuteScalar() ?? 0;

            if(rowCount != 1)
            {
                Console.Error.WriteLine($"Error: osrepodbmgr table should contain exactly 1 row, but found {rowCount}");
                Environment.Exit(1);
            }

            // Get the version value
            using SqliteCommand versionCommand = connection.CreateCommand();
            versionCommand.CommandText = "SELECT version FROM osrepodbmgr LIMIT 1";
            object? version = versionCommand.ExecuteScalar();

            if(version == null)
            {
                Console.Error.WriteLine("Error: version column not found or is NULL in osrepodbmgr table");
                Environment.Exit(1);
            }

            if(!int.TryParse(version.ToString(), out int versionValue) || versionValue != 1)
            {
                Console.Error.WriteLine($"Error: osrepodbmgr table version must be 1, but found {version}");
                Environment.Exit(1);
            }

            Console.WriteLine($"OS Repo DB Manager version: {versionValue}");
        }
        catch(Exception ex)
        {
            Console.Error.WriteLine($"Error: Failed to validate database: {ex.Message}");
            Environment.Exit(1);
        }
    }

    static void ValidateOs(SqliteConnection connection, int dbId)
    {
        try
        {
            // Check if the provided dbId exists in the oses table
            using SqliteCommand osIdCommand = connection.CreateCommand();

            osIdCommand.CommandText = @"
                SELECT developer, product, version, languages, architecture, machine, format,
                       description, oem, upgrade, ""update"", source, files, netinstall
                FROM oses WHERE id = @id";

            osIdCommand.Parameters.AddWithValue("@id", dbId);

            using SqliteDataReader reader = osIdCommand.ExecuteReader();

            if(!reader.Read())
            {
                Console.Error.WriteLine($"Error: OS ID {dbId} not found in oses table");
                Environment.Exit(1);
            }

            // Extract OS information
            string developer    = reader["developer"]?.ToString()    ?? "";
            string product      = reader["product"]?.ToString()      ?? "";
            string osVersion    = reader["version"]?.ToString()      ?? "";
            string languages    = reader["languages"]?.ToString()    ?? "";
            string architecture = reader["architecture"]?.ToString() ?? "";
            string machine      = reader["machine"]?.ToString()      ?? "";
            string format       = reader["format"]?.ToString()       ?? "";
            string description  = reader["description"]?.ToString()  ?? "";

            // SQLite stores booleans as integers (0 or 1), so convert them appropriately
            bool oem        = reader["oem"]        != DBNull.Value && Convert.ToInt32(reader["oem"])        != 0;
            bool upgrade    = reader["upgrade"]    != DBNull.Value && Convert.ToInt32(reader["upgrade"])    != 0;
            bool update     = reader["update"]     != DBNull.Value && Convert.ToInt32(reader["update"])     != 0;
            bool source     = reader["source"]     != DBNull.Value && Convert.ToInt32(reader["source"])     != 0;
            bool files      = reader["files"]      != DBNull.Value && Convert.ToInt32(reader["files"])      != 0;
            bool netinstall = reader["netinstall"] != DBNull.Value && Convert.ToInt32(reader["netinstall"]) != 0;

            // Build the features string
            var features = new List<string>();
            if(oem) features.Add("oem");
            if(upgrade) features.Add("upgrade");
            if(update) features.Add("update");
            if(source) features.Add("source");
            if(files) features.Add("files");
            if(netinstall) features.Add("netinstall");

            string featuresText = features.Count > 0 ? string.Join(", ", features) : "none";

            Console.WriteLine($"\nOS ID {dbId}:");

            // Display using Spectre.Console
            var table = new Table();
            table.AddColumn("Property");
            table.AddColumn("Value");

            table.AddRow(new Text("Developer"),    new Text(developer));
            table.AddRow(new Text("Product"),      new Text(product));
            table.AddRow(new Text("Version"),      new Text(osVersion));
            table.AddRow(new Text("Languages"),    new Text(languages));
            table.AddRow(new Text("Architecture"), new Text(architecture));
            table.AddRow(new Text("Machine"),      new Text(machine));
            table.AddRow(new Text("Format"),       new Text(format));
            table.AddRow(new Text("Description"),  new Text(description));
            table.AddRow(new Text("Features"),     new Text(featuresText));

            AnsiConsole.Write(table);
        }
        catch(Exception ex)
        {
            Console.Error.WriteLine($"Error: Failed to validate OS: {ex.Message}");
            Environment.Exit(1);
        }
    }
}