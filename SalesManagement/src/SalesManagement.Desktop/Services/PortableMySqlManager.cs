using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using Microsoft.Extensions.Configuration;
using MySqlConnector;

namespace SalesManagement.Desktop.Services;

public class PortableMySqlManager
{
    private static Process? _mysqlProcess;
    private readonly string _baseDir;
    private readonly string _mysqlDir;
    private readonly string _binDir;
    private readonly string _dataDir;
    private readonly string _iniFile;
    private readonly int _port;
    private readonly string _connectionString;

    public PortableMySqlManager(IConfiguration configuration)
    {
        _baseDir = AppDomain.CurrentDomain.BaseDirectory;
        _mysqlDir = Path.Combine(_baseDir, "mysql");
        _binDir = Path.Combine(_mysqlDir, "bin");
        _dataDir = Path.Combine(_mysqlDir, "data");
        _iniFile = Path.Combine(_mysqlDir, "my.ini");

        var connString = configuration.GetConnectionString("DefaultConnection") 
            ?? "Server=localhost;Port=3307;Database=sales_management;User=root;Password=;CharSet=utf8mb4;";

        var builder = new MySqlConnectionStringBuilder(connString);
        _port = (int)builder.Port;
        _connectionString = connString;
    }

    public bool IsPortableInstalled => File.Exists(Path.Combine(_binDir, "mysqld.exe"));

    public async Task EnsureDatabaseReadyAsync()
    {
        if (IsPortableInstalled)
        {
            await StartPortableServerIfNeededAsync();
        }

        await InitializeSchemaAndSeedAsync();
    }

    private async Task StartPortableServerIfNeededAsync()
    {
        // 1. Check if MySQL is already running on the configured port
        if (IsPortListening("127.0.0.1", _port))
        {
            return; // Already running (either as Windows service or previous instance)
        }

        // 2. Initialize data directory if first run
        if (!Directory.Exists(_dataDir) || Directory.GetFileSystemEntries(_dataDir).Length == 0)
        {
            Directory.CreateDirectory(_dataDir);
            var initPsi = new ProcessStartInfo
            {
                FileName = Path.Combine(_binDir, "mysqld.exe"),
                Arguments = $"--defaults-file=\"{_iniFile}\" --initialize-insecure --basedir=\"{_mysqlDir}\" --datadir=\"{_dataDir}\"",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true
            };

            using var initProc = Process.Start(initPsi);
            if (initProc != null)
            {
                await initProc.WaitForExitAsync();
            }
        }

        // 3. Start portable mysqld.exe process in background
        var startPsi = new ProcessStartInfo
        {
            FileName = Path.Combine(_binDir, "mysqld.exe"),
            Arguments = $"--defaults-file=\"{_iniFile}\" --basedir=\"{_mysqlDir}\" --datadir=\"{_dataDir}\"",
            CreateNoWindow = true,
            UseShellExecute = false
        };

        _mysqlProcess = Process.Start(startPsi);

        // 4. Wait for server to listen (up to 15 seconds)
        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.ElapsedMilliseconds < 15000)
        {
            if (IsPortListening("127.0.0.1", _port))
            {
                return;
            }
            await Task.Delay(500);
        }
    }

    private async Task InitializeSchemaAndSeedAsync()
    {
        var builder = new MySqlConnectionStringBuilder(_connectionString)
        {
            Database = "" // Connect without specifying database first
        };

        try
        {
            await using var connection = new MySqlConnection(builder.ConnectionString);
            await connection.OpenAsync();

            // Check if sales_management database exists
            await using (var checkDbCmd = new MySqlCommand("SELECT COUNT(*) FROM information_schema.schemata WHERE schema_name = 'sales_management';", connection))
            {
                var dbCount = Convert.ToInt64(await checkDbCmd.ExecuteScalarAsync());
                if (dbCount == 0)
                {
                    await using var createDbCmd = new MySqlCommand("CREATE DATABASE IF NOT EXISTS `sales_management` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;", connection);
                    await createDbCmd.ExecuteNonQueryAsync();
                }
            }

            // Check if tables already exist in sales_management
            await connection.ChangeDatabaseAsync("sales_management");
            bool tablesExist;
            await using (var checkTablesCmd = new MySqlCommand("SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'sales_management';", connection))
            {
                var tableCount = Convert.ToInt64(await checkTablesCmd.ExecuteScalarAsync());
                tablesExist = tableCount > 0;
            }

            if (!tablesExist)
            {
                // Run 01_schema.sql
                string schemaPath = Path.Combine(_baseDir, "database", "01_schema.sql");
                if (File.Exists(schemaPath))
                {
                    string schemaSql = await File.ReadAllTextAsync(schemaPath);
                    var script = new MySqlScript(connection, schemaSql);
                    await script.ExecuteAsync();
                }

                // Run 02_seed_data.sql
                string seedPath = Path.Combine(_baseDir, "database", "02_seed_data.sql");
                if (File.Exists(seedPath))
                {
                    string seedSql = await File.ReadAllTextAsync(seedPath);
                    var script = new MySqlScript(connection, seedSql);
                    await script.ExecuteAsync();
                }
            }
        }
        catch
        {
            // If server connection fails, let DbContext raise a clear error to user UI
        }
    }

    public static void ShutdownPortableServer()
    {
        if (_mysqlProcess != null && !_mysqlProcess.HasExited)
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string adminPath = Path.Combine(baseDir, "mysql", "bin", "mysqladmin.exe");
                if (File.Exists(adminPath))
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = adminPath,
                        Arguments = "-P 3307 -u root shutdown",
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };
                    using var proc = Process.Start(psi);
                    proc?.WaitForExit(3000);
                }

                if (!_mysqlProcess.HasExited)
                {
                    _mysqlProcess.Kill(entireProcessTree: true);
                }
            }
            catch { }
        }
    }

    private static bool IsPortListening(string host, int port)
    {
        try
        {
            using var client = new TcpClient();
            var result = client.BeginConnect(host, port, null, null);
            bool success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(400));
            if (!success) return false;
            client.EndConnect(result);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
