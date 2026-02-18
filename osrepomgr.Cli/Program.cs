// See https://aka.ms/new-console-template for more information

using System.IO.Compression;
using Microsoft.Data.Sqlite;
using SharpCompress.Compressors.BZip2;
using SharpCompress.Compressors.LZMA;
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

            // Create destination folder if it doesn't exist
            if(!Directory.Exists(destination))
            {
                try
                {
                    Directory.CreateDirectory(destination);
                    Console.WriteLine($"Created destination folder: {destination}");
                }
                catch(Exception ex)
                {
                    Console.Error.WriteLine($"Error: Failed to create destination folder: {ex.Message}");
                    Environment.Exit(1);
                }
            }
            else
                Console.WriteLine($"Destination folder already exists: {destination}");

            // Create progress bar for creating folders
            AnsiConsole.Progress()
                       .AutoClear(true)
                       .Columns(new ProgressBarColumn(),
                                new PercentageColumn(),
                                new RemainingTimeColumn(),
                                new TaskDescriptionColumn
                                {
                                    Alignment = Justify.Left
                                })
                       .Start(ctx =>
                        {
                            ProgressTask task = ctx.AddTask("[cyan]Creating folders[/]");

                            try
                            {
                                // First, get count of rows in the os_{id}_folders table
                                var folderTableName = $"os_{dbId}_folders";
                                var countQuery      = $"SELECT COUNT(*) FROM {folderTableName}";

                                long totalFolders = 0;

                                using(SqliteCommand countCmd = connection.CreateCommand())
                                {
                                    countCmd.CommandText = countQuery;
                                    totalFolders         = (long?)countCmd.ExecuteScalar() ?? 0;
                                }

                                Console.WriteLine($"Found {totalFolders} folders to create");

                                // Set progress bar to determinate with total count
                                task.MaxValue = totalFolders;

                                // Query folders from the os_{id}_folders table
                                var query =
                                    $"SELECT id, path, creation, access, modification, attributes FROM {folderTableName}";

                                using(SqliteCommand cmd = connection.CreateCommand())
                                {
                                    cmd.CommandText = query;

                                    using(SqliteDataReader reader = cmd.ExecuteReader())
                                    {
                                        while(reader.Read())
                                        {
                                            int folderId =
                                                reader["id"] != DBNull.Value ? Convert.ToInt32(reader["id"]) : 0;

                                            string path = reader["path"]?.ToString() ?? "";

                                            DateTime? creation = reader["creation"] != DBNull.Value
                                                                     ? Convert.ToDateTime(reader["creation"])
                                                                     : null;

                                            DateTime? access = reader["access"] != DBNull.Value
                                                                   ? Convert.ToDateTime(reader["access"])
                                                                   : null;

                                            DateTime? modification =
                                                reader["modification"] != DBNull.Value
                                                    ? Convert.ToDateTime(reader["modification"])
                                                    : null;

                                            int attributes = reader["attributes"] != DBNull.Value
                                                                 ? Convert.ToInt32(reader["attributes"])
                                                                 : 0;

                                            // Create folder in destination
                                            string fullPath = Path.Combine(destination, path);

                                            try
                                            {
                                                // Create the directory if it doesn't exist
                                                Directory.CreateDirectory(fullPath);

                                                // Apply creation time
                                                if(creation.HasValue)
                                                    Directory.SetCreationTime(fullPath, creation.Value);

                                                // Apply access time
                                                if(access.HasValue) Directory.SetLastAccessTime(fullPath, access.Value);

                                                // Apply modification time
                                                if(modification.HasValue)
                                                    Directory.SetLastWriteTime(fullPath, modification.Value);

                                                // Apply attributes if provided (excluding ReadOnly)
                                                if(attributes != 0)
                                                {
                                                    var attrs = (FileAttributes)attributes;

                                                    // Remove readonly attribute
                                                    attrs &= ~FileAttributes.ReadOnly;
                                                    if(attrs != 0) File.SetAttributes(fullPath, attrs);
                                                }
                                            }
                                            catch(Exception ex)
                                            {
                                                AnsiConsole.WriteException(ex);
                                            }

                                            task.Increment(1);
                                        }
                                    }
                                }
                            }
                            catch(Exception ex)
                            {
                                AnsiConsole.WriteException(ex);
                            }
                        });

            // Check if symlinks table exists
            var symlinksTableName = $"os_{dbId}_symlinks";

            var checkSymlinksQuery =
                $"SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='{symlinksTableName}'";

            long symlinksTableExists = 0;

            try
            {
                using(SqliteCommand checkCmd = connection.CreateCommand())
                {
                    checkCmd.CommandText = checkSymlinksQuery;
                    symlinksTableExists  = (long?)checkCmd.ExecuteScalar() ?? 0;
                }

                if(symlinksTableExists > 0)
                {
                    Console.WriteLine($"Found symlinks table: {symlinksTableName}");

                    // Create progress bar for processing symlinks
                    AnsiConsole.Progress()
                               .AutoClear(true)
                               .Columns(new ProgressBarColumn(),
                                        new PercentageColumn(),
                                        new RemainingTimeColumn(),
                                        new TaskDescriptionColumn
                                        {
                                            Alignment = Justify.Left
                                        })
                               .Start(ctx =>
                                {
                                    ProgressTask task = ctx.AddTask("[magenta]Processing symlinks[/]");

                                    try
                                    {
                                        // Get count of rows in the os_{id}_symlinks table
                                        var countQuery = $"SELECT COUNT(*) FROM {symlinksTableName}";

                                        long totalSymlinks = 0;

                                        using(SqliteCommand countCmd = connection.CreateCommand())
                                        {
                                            countCmd.CommandText = countQuery;
                                            totalSymlinks        = (long?)countCmd.ExecuteScalar() ?? 0;
                                        }

                                        Console.WriteLine($"Found {totalSymlinks} symlinks to process");

                                        // Set progress bar to determinate with total count
                                        task.MaxValue = totalSymlinks;

                                        // Query symlinks from the os_{id}_symlinks table
                                        var query = $"SELECT * FROM {symlinksTableName}";

                                        using(SqliteCommand cmd = connection.CreateCommand())
                                        {
                                            cmd.CommandText = query;

                                            using(SqliteDataReader reader = cmd.ExecuteReader())
                                            {
                                                while(reader.Read())
                                                {
                                                    string path   = reader.GetString(0);
                                                    string target = reader.GetString(1);

                                                    string fullSymlinkPath = Path.Combine(destination, path);

                                                    try
                                                    {
                                                        // Ensure parent directory exists
                                                        string? parentDir = Path.GetDirectoryName(fullSymlinkPath);

                                                        if(!string.IsNullOrEmpty(parentDir) &&
                                                           !Directory.Exists(parentDir))
                                                            Directory.CreateDirectory(parentDir);

                                                        // Create symbolic link
                                                        File.CreateSymbolicLink(fullSymlinkPath, target);
                                                    }
                                                    catch(Exception ex)
                                                    {
                                                        Console.Error
                                                               .WriteLine($"Failed to create symlink {fullSymlinkPath} -> {target}: {ex.Message}");
                                                    }

                                                    task.Increment(1);
                                                }
                                            }
                                        }
                                    }
                                    catch(Exception ex)
                                    {
                                        AnsiConsole.WriteException(ex);
                                    }
                                });
                }
            }
            catch(Exception ex)
            {
                Console.Error.WriteLine($"Error: Failed to check symlinks table: {ex.Message}");
            }

            // Process files from os_{id}_files table
            var filesTableName = $"os_{dbId}";

            Console.WriteLine($"Found files table: {filesTableName}");

            // Get count of files to process
            long totalFiles = 0;

            using(SqliteCommand countCmd = connection.CreateCommand())
            {
                countCmd.CommandText = $"SELECT COUNT(*) FROM {filesTableName}";
                totalFiles           = (long?)countCmd.ExecuteScalar() ?? 0;
            }

            if(totalFiles <= 0) return;

            Console.WriteLine($"Found {totalFiles} files to process");

            // Create progress bar for total file processing
            AnsiConsole.Progress()
                       .AutoClear(true)
                       .Columns(new ProgressBarColumn(),
                                new PercentageColumn(),
                                new RemainingTimeColumn(),
                                new TaskDescriptionColumn
                                {
                                    Alignment = Justify.Left
                                })
                       .Start(ctx =>
                        {
                            ProgressTask filesTask = ctx.AddTask("[green]Extracting files[/]", maxValue: totalFiles);

                            ProgressTask decompressionTask = ctx.AddTask("[yellow]Decompressing[/]");

                            try
                            {
                                var query =
                                    $"SELECT path, sha256, length, creation, access, modification, attributes FROM {filesTableName}";

                                using SqliteCommand cmd = connection.CreateCommand();

                                cmd.CommandText = query;

                                using SqliteDataReader reader = cmd.ExecuteReader();

                                while(reader.Read())
                                {
                                    string filePath = reader["path"]?.ToString()   ?? "";
                                    string sha256   = reader["sha256"]?.ToString() ?? "";

                                    long fileSize = reader["length"] != DBNull.Value ? (long)reader["length"] : 0;

                                    var     createdStr = reader["creation"]?.ToString();
                                    var     accessStr  = reader["access"]?.ToString();
                                    var     modifyStr  = reader["modification"]?.ToString();
                                    object? attrObj    = reader["attributes"];

                                    int attributes = attrObj != DBNull.Value ? Convert.ToInt32(attrObj) : 0;

                                    filesTask.Description =
                                        $"[green]extracting:[/] [purple]{Path.GetFileName(filePath)}[/]";

                                    try
                                    {
                                        // Find the compressed file in the repository
                                        string? compressed = FindCompressedFile(repositoryPath, sha256);

                                        if(string.IsNullOrEmpty(compressed))
                                        {
                                            Console.Error
                                                   .WriteLine($"Failed to find compressed file for {filePath} (SHA256: {sha256})");

                                            filesTask.Increment(1);

                                            continue;
                                        }

                                        // Determine compression format from extension
                                        string ext = Path.GetExtension(compressed).ToLowerInvariant().TrimStart('.');

                                        // Create output directory
                                        string  fullFilePath = Path.Combine(destination, filePath);
                                        string? outputDir    = Path.GetDirectoryName(fullFilePath);

                                        if(!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
                                            Directory.CreateDirectory(outputDir);

                                        // Decompress file with progress tracking
                                        DecompressFile(compressed, fullFilePath, ext, fileSize, decompressionTask);

                                        // Apply timestamps and attributes
                                        try
                                        {
                                            if(!string.IsNullOrEmpty(createdStr) &&
                                               DateTime.TryParse(createdStr, out DateTime createdTime))
                                                File.SetCreationTime(fullFilePath, createdTime);

                                            if(!string.IsNullOrEmpty(modifyStr) &&
                                               DateTime.TryParse(modifyStr, out DateTime modifyTime))
                                                File.SetLastWriteTime(fullFilePath, modifyTime);

                                            if(!string.IsNullOrEmpty(accessStr) &&
                                               DateTime.TryParse(accessStr, out DateTime accessTime))
                                                File.SetLastAccessTime(fullFilePath, accessTime);

                                            // Apply attributes (including ReadOnly)
                                            if(attributes != 0)
                                            {
                                                var attrs = (FileAttributes)attributes;
                                                File.SetAttributes(fullFilePath, attrs);
                                            }
                                        }
                                        catch(Exception ex)
                                        {
                                            Console.Error
                                                   .WriteLine($"Failed to apply metadata to {fullFilePath}: {ex.Message}");
                                        }
                                    }
                                    catch(Exception ex)
                                    {
                                        Console.Error.WriteLine($"Failed to extract file {filePath}: {ex.Message}");
                                    }

                                    filesTask.Increment(1);
                                }
                            }
                            catch(Exception ex)
                            {
                                AnsiConsole.WriteException(ex);
                            }
                        });

            Console.WriteLine("Finished successfully!");
        }
        catch(Exception ex)
        {
            Console.Error.WriteLine($"Error: Failed to open database: {ex.Message}");
            Environment.Exit(1);
        }
    }

    static string? FindCompressedFile(string repositoryPath, string sha256)
    {
        // Pattern: Repository/a/a/a/a/a/aaaaa24f2b309959c0e5c7d70a274f677237ca6b086f0f08b2e911200e0c0c56.{ext}
        if(sha256.Length < 5) return null;

        string folderPath = Path.Combine(repositoryPath,
                                         sha256[0].ToString(),
                                         sha256[1].ToString(),
                                         sha256[2].ToString(),
                                         sha256[3].ToString(),
                                         sha256[4].ToString());

        if(!Directory.Exists(folderPath)) return null;

        // Look for files starting with the SHA256 hash
        string[] files = Directory.GetFiles(folderPath, $"{sha256}.*");

        return files.Length > 0 ? files[0] : null;
    }

    static void DecompressFile(string       compressedPath, string outputPath, string compressionFormat, long fileSize,
                               ProgressTask decompressionTask)
    {
        const int bufferSize = 65536; // 64KB chunks

        decompressionTask.MaxValue = fileSize;

        switch(compressionFormat)
        {
            case "gz":
                DecompressGzip(compressedPath, outputPath, bufferSize, decompressionTask);

                break;

            case "bz2":
                DecompressBzip2(compressedPath, outputPath, bufferSize, decompressionTask);

                break;

            case "lzma":
                DecompressLzma(compressedPath, outputPath, bufferSize, decompressionTask);

                break;

            case "lz":
                DecompressLzip(compressedPath, outputPath, bufferSize, decompressionTask);

                break;

            default:
                throw new NotSupportedException($"Unsupported compression format: {compressionFormat}");
        }
    }

    static void DecompressGzip(string compressedPath, string outputPath, int bufferSize, ProgressTask fileTask)
    {
        using FileStream fileStream = File.OpenRead(compressedPath);

        using var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress);

        using FileStream outputStream = File.Create(outputPath);

        var buffer = new byte[bufferSize];
        int bytesRead;

        while((bytesRead = gzipStream.Read(buffer, 0, bufferSize)) > 0)
        {
            outputStream.Write(buffer, 0, bytesRead);
            fileTask.Increment(bytesRead);
        }
    }

    static void DecompressLzip(string compressedPath, string outputPath, int bufferSize, ProgressTask fileTask)
    {
        using FileStream fileStream = File.OpenRead(compressedPath);

        using var lzipStream = new LZipStream(fileStream, SharpCompress.Compressors.CompressionMode.Decompress);

        using FileStream outputStream = File.Create(outputPath);

        var buffer = new byte[bufferSize];
        int bytesRead;

        while((bytesRead = lzipStream.Read(buffer, 0, bufferSize)) > 0)
        {
            outputStream.Write(buffer, 0, bytesRead);
            fileTask.Increment(bytesRead);
        }
    }

    static void DecompressBzip2(string compressedPath, string outputPath, int bufferSize, ProgressTask fileTask)
    {
        using FileStream fileStream = File.OpenRead(compressedPath);

        using var bzip2Stream =
            new BZip2Stream(fileStream, SharpCompress.Compressors.CompressionMode.Decompress, false);

        using FileStream outputStream = File.Create(outputPath);

        var buffer = new byte[bufferSize];
        int bytesRead;

        while((bytesRead = bzip2Stream.Read(buffer, 0, bufferSize)) > 0)
        {
            outputStream.Write(buffer, 0, bytesRead);
            fileTask.Increment(bytesRead);
        }
    }

    static void DecompressLzma(string compressedPath, string outputPath, int bufferSize, ProgressTask fileTask)
    {
        using FileStream fileStream = File.OpenRead(compressedPath);
        var              properties = new byte[5];
        fileStream.ReadExactly(properties, 0, 5);
        fileStream.Seek(8, SeekOrigin.Current);

        using var lzmaStream = new LzmaStream(properties, fileStream);

        using FileStream outputStream = File.Create(outputPath);

        var buffer = new byte[bufferSize];
        int bytesRead;

        while((bytesRead = lzmaStream.Read(buffer, 0, bufferSize)) > 0)
        {
            outputStream.Write(buffer, 0, bytesRead);
            fileTask.Increment(bytesRead);
        }
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

    static void ShowHelp()
    {
        var helpPanel = new Panel("[yellow]Usage:[/] osrepomgr [OPTIONS] <db-id> <destination>\n" +
                                  "[yellow]Arguments:[/]\n  <[cyan]db-id[/]> Database ID (integer)\n  <[cyan]destination[/]> Destination path\n" +
                                  "[yellow]Options:[/]\n  [cyan]--repository, -r[/] <path>  Repository folder\n  [cyan]--database, -d[/] <path>     Database file")
        {
            Header = new PanelHeader("[bold magenta]Help[/]"),
            Border = BoxBorder.Rounded
        };

        AnsiConsole.Write(helpPanel);
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