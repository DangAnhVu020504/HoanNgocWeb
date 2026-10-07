using Serilog;
using Serilog.Events;
using TechPos.Data.Firebird;
using TechPos.Data.MySql;
using TechPos.Shared;
using TechPos.Sync.Core;
using TechPos.Sync.Service;

var builder = Host.CreateApplicationBuilder(args);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/sync-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();
builder.Logging.AddSerilog();
builder.Services.AddSingleton(Serilog.Log.Logger);

var dbOptions = builder.Configuration.GetSection(DatabaseOptions.Section).Get<DatabaseOptions>() ?? new();

// DI
var fbFactory = new FirebirdConnectionFactory(new(
    dbOptions.FirebirdDatabasePath, dbOptions.FirebirdClientLibrary, dbOptions.FirebirdUser, dbOptions.FirebirdPassword));
var myFactory = new MySqlConnectionFactory(dbOptions.MySqlConnectionString);
builder.Services.AddSingleton(myFactory);
builder.Services.AddSingleton(fbFactory);

builder.Services.AddSingleton<SyncMetaRepository.MySqlConnectionFactoryLike>(new MySqlMetaAdapter(myFactory));
builder.Services.AddSingleton<SyncMetaRepository>();
builder.Services.AddSingleton<IFirebirdSyncSource>(new FirebirdSyncSourceAdapter(fbFactory));
builder.Services.AddSingleton<IMySqlSyncTarget>(new MySqlTargetAdapter(myFactory));
builder.Services.AddSingleton<WatermarkSyncEngine>();
builder.Services.AddSingleton<ReconcileEngine>();
builder.Services.AddSingleton<MediaExtractor>();
builder.Services.AddSingleton<SyncPolicySeeder>();
builder.Services.AddSingleton<OutboxRepository>();
builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<TechPos.Shared.FeatureFlagOptions>>().Value);
builder.Services.AddSingleton<TwoWaySyncEngine>();
builder.Services.AddHostedService<SyncWorker>();

builder.Services.AddWindowsService(o => o.ServiceName = "TechPosSync");

var host = builder.Build();
host.Run();

// ---------- Adapters: Data.MySql / Data.Firebird → Sync.Core interfaces ----------

internal sealed class MySqlMetaAdapter(MySqlConnectionFactory factory) : SyncMetaRepository.MySqlConnectionFactoryLike
{
    public async Task<MySqlConnector.MySqlConnection> OpenAsync(CancellationToken ct = default)
        => await factory.OpenAsync(ct);
}

internal sealed class MySqlTargetAdapter(MySqlConnectionFactory factory) : IMySqlSyncTarget
{
    public async Task<MySqlConnector.MySqlConnection> OpenAsync(CancellationToken ct = default)
        => await factory.OpenAsync(ct);
}

internal sealed class FirebirdSyncSourceAdapter(FirebirdConnectionFactory factory) : IFirebirdSyncSource
{
    public async Task<System.Data.Common.DbConnection> OpenAsync(CancellationToken ct = default)
        => await factory.OpenAsync(ct);
}
