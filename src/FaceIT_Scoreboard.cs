using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FaceIT_Scoreboard;

public class FaceitConfig : BasePluginConfig
{
    [JsonPropertyName("FaceitApiKey")]
    public string FaceitApiKey { get; set; } = "";

    [JsonPropertyName("UseCSGO")]
    public bool UseCSGO { get; set; } = false;

    [JsonPropertyName("DefaultStatus")]
    public bool DefaultStatus { get; set; } = true;

    [JsonPropertyName("Commands")]
    public List<string> Commands { get; set; } = new() { "!faceit", "!fl" };

    [JsonPropertyName("CacheExpiryHours")]
    public int CacheExpiryHours { get; set; } = 24;

    [JsonPropertyName("MaxConcurrentRequests")]
    public int MaxConcurrentRequests { get; set; } = 10;

    [JsonPropertyName("RequestTimeoutSeconds")]
    public int RequestTimeoutSeconds { get; set; } = 10;

    [JsonPropertyName("ConfigVersion")]
    public override int Version { get; set; } = 2;
}

public class PlayerData
{
    public bool ShowFaceitLevel { get; set; }
    public int FaceitLevel { get; set; }
    public DateTime LastFetch { get; set; }
    public string? FaceitId { get; set; }
    public bool IsProcessing { get; set; }
}

public class FaceitPlayer
{
    [JsonPropertyName("player_id")]
    public string PlayerId { get; set; } = "";

    [JsonPropertyName("games")]
    public Dictionary<string, GameData> Games { get; set; } = new();
}

public class GameData
{
    [JsonPropertyName("skill_level")]
    public int SkillLevel { get; set; }
}

[MinimumApiVersion(147)]
public partial class FaceIT_Scoreboard : BasePlugin, IPluginConfig<FaceitConfig>
{
    public override string ModuleName => "FaceIT_Scoreboard";
    public override string ModuleAuthor => "zhw1nq";
    public override string ModuleDescription => "Displays FaceIT levels on the scoreboard – know who's carrying in a blink.";
    public override string ModuleVersion => "1.0.1";

    public FaceitConfig Config { get; set; } = new();

    private static readonly HttpClient _httpClient = new();
    private readonly ConcurrentDictionary<ulong, PlayerData> _playerData = new();
    private SemaphoreSlim? _apiSemaphore;
    private readonly SemaphoreSlim _fileSemaphore = new(1, 1);

    private static readonly Dictionary<int, int> FaceitLevelCoins = new()
    {
        { 1, 1088 }, { 2, 1087 }, { 3, 1032 }, { 4, 1055 }, { 5, 1041 },
        { 6, 1074 }, { 7, 1039 }, { 8, 1067 }, { 9, 1061 }, { 10, 1017 }
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private CounterStrikeSharp.API.Modules.Timers.Timer? _updateTimer;
    private CounterStrikeSharp.API.Modules.Timers.Timer? _saveTimer;
    private string _dataPath = "";
    private readonly ConcurrentQueue<ulong> _saveQueue = new();

    public FaceIT_Scoreboard()
    {
        // Initialize with default value, will be recreated in OnConfigParsed
        _apiSemaphore = new SemaphoreSlim(10, 10);
    }

    public void OnConfigParsed(FaceitConfig config)
    {
        Config = config;

        if (string.IsNullOrEmpty(Config.FaceitApiKey))
        {
            Logger.LogWarning("Faceit API key is not configured!");
        }

        // Configure HTTP client
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {Config.FaceitApiKey}");
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        _httpClient.Timeout = TimeSpan.FromSeconds(Config.RequestTimeoutSeconds);

        // Recreate semaphore with the correct capacity
        _apiSemaphore?.Dispose();
        _apiSemaphore = new SemaphoreSlim(Config.MaxConcurrentRequests, Config.MaxConcurrentRequests);

        _dataPath = Path.Combine(ModuleDirectory, "data", "faceit_data.json");
    }

    public override void Load(bool hotReload)
    {
        RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnectFull);
        RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);

        // Register commands efficiently
        foreach (var command in Config.Commands)
        {
            AddCommand(command, "Toggle Faceit level display", OnFaceitCommand);
        }

        // Setup optimal timers
        _updateTimer = AddTimer(2.0f, UpdatePlayerCoins, TimerFlags.REPEAT);
        _saveTimer = AddTimer(30.0f, ProcessSaveQueue, TimerFlags.REPEAT);

        // Load player data from file (if exists)
        _ = LoadPlayerDataAsync();

        Logger.LogInformation($"{ModuleName} v{ModuleVersion} loaded successfully!");
    }

    public override void Unload(bool hotReload)
    {
        _updateTimer?.Kill();
        _saveTimer?.Kill();

        // Save all pending data
        ProcessSaveQueue();

        _apiSemaphore?.Dispose();
        _fileSemaphore.Dispose();
    }

    [GameEventHandler]
    public HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player?.IsValid != true || player.IsBot)
            return HookResult.Continue;

        var steamId = player.SteamID;

        // Initialize or get player data
        var playerData = _playerData.GetOrAdd(steamId, _ => new PlayerData
        {
            ShowFaceitLevel = Config.DefaultStatus,
            FaceitLevel = 0,
            LastFetch = DateTime.MinValue,
            IsProcessing = false
        });

        // Fetch Faceit level data if needed (don't wait for result)
        if (ShouldFetchLevel(playerData))
        {
            _ = FetchPlayerFaceitLevelAsync(steamId);
        }

        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player?.IsValid != true || player.IsBot)
            return HookResult.Continue;

        // Add player to save queue
        _saveQueue.Enqueue(player.SteamID);

        return HookResult.Continue;
    }

    [ConsoleCommand("css_faceit")]
    [ConsoleCommand("css_fl")]
    [CommandHelper(minArgs: 0, usage: "", whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnFaceitCommand(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (player?.IsValid != true || player.IsBot)
            return;

        var steamId = player.SteamID;
        var playerData = _playerData.GetOrAdd(steamId, _ => new PlayerData
        {
            ShowFaceitLevel = Config.DefaultStatus,
            FaceitLevel = 0,
            LastFetch = DateTime.MinValue,
            IsProcessing = false
        });

        playerData.ShowFaceitLevel = !playerData.ShowFaceitLevel;
        var status = playerData.ShowFaceitLevel ? "enabled" : "disabled";
        var color = playerData.ShowFaceitLevel ? ChatColors.Green : ChatColors.Red;

        player.PrintToChat($" {ChatColors.Gold}[FaceIT_Scoreboard] {ChatColors.Silver}>> FaceIT level display {color}{status}!");

        if (playerData.ShowFaceitLevel)
        {
            if (playerData.FaceitLevel > 0)
            {
                ApplyPlayerCoin(player, playerData.FaceitLevel);
            }
            else if (!playerData.IsProcessing && ShouldFetchLevel(playerData))
            {
                _ = FetchPlayerFaceitLevelAsync(steamId);
            }
        }
        else
        {
            ApplyPlayerCoin(player, 0);
        }

        // Add player to save queue
        _saveQueue.Enqueue(steamId);
    }

    private bool ShouldFetchLevel(PlayerData playerData)
    {
        return !playerData.IsProcessing &&
               (playerData.FaceitLevel == 0 ||
                DateTime.Now - playerData.LastFetch > TimeSpan.FromHours(Config.CacheExpiryHours));
    }

    private async Task FetchPlayerFaceitLevelAsync(ulong steamId)
    {
        if (_apiSemaphore == null || !await _apiSemaphore.WaitAsync(100))
            return;

        try
        {
            if (!_playerData.TryGetValue(steamId, out var playerData) || playerData.IsProcessing)
                return;

            playerData.IsProcessing = true;

            var faceitLevel = await GetFaceitLevelFromApiAsync(steamId);

            playerData.FaceitLevel = faceitLevel;
            playerData.LastFetch = DateTime.Now;
            playerData.IsProcessing = false;

            if (faceitLevel > 0)
            {
                Logger.LogDebug($"Fetched FaceIT level {faceitLevel} for Steam ID {steamId}");

                if (playerData.ShowFaceitLevel)
                {
                    Server.NextFrame(() =>
                    {
                        var player = Utilities.GetPlayerFromSteamId(steamId);
                        if (player?.IsValid == true && !player.IsBot)
                        {
                            ApplyPlayerCoin(player, faceitLevel);
                        }
                    });
                }
            }

            // Add player to save queue
            _saveQueue.Enqueue(steamId);
        }
        catch (Exception ex)
        {
            if (_playerData.TryGetValue(steamId, out var playerData))
                playerData.IsProcessing = false;

            Logger.LogError(ex, $"Error fetching FaceIT level for Steam ID {steamId}");
        }
        finally
        {
            _apiSemaphore.Release();
        }
    }

    private async Task<int> GetFaceitLevelFromApiAsync(ulong steamId)
    {
        try
        {
            // Try CS2 first
            var level = await FetchFromFaceitApiAsync(steamId, "cs2");

            // If no CS2 data, try CSGO
            if (level == 0 && Config.UseCSGO)
            {
                level = await FetchFromFaceitApiAsync(steamId, "csgo");
            }

            return level;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"Error calling FaceIT API for Steam ID {steamId}");
            return 0;
        }
    }

    private async Task<int> FetchFromFaceitApiAsync(ulong steamId, string game)
    {
        try
        {
            var url = $"https://open.faceit.com/data/v4/players?game={game}&game_player_id={steamId}";

            using var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode != System.Net.HttpStatusCode.NotFound)
                    Logger.LogWarning($"FaceIT API returned {response.StatusCode} for Steam ID {steamId}");
                return 0;
            }

            var jsonContent = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrEmpty(jsonContent))
                return 0;

            var player = JsonSerializer.Deserialize<FaceitPlayer>(jsonContent, JsonOptions);

            return player?.Games?.TryGetValue(game, out var gameData) == true
                ? gameData.SkillLevel
                : 0;
        }
        catch (TaskCanceledException)
        {
            Logger.LogWarning($"Request timeout for Steam ID {steamId}");
            return 0;
        }
        catch (JsonException ex)
        {
            Logger.LogError(ex, $"JSON parsing error for Steam ID {steamId}");
            return 0;
        }
        catch (HttpRequestException ex)
        {
            Logger.LogError(ex, $"HTTP request error for Steam ID {steamId}");
            return 0;
        }
    }

    private void UpdatePlayerCoins()
    {
        try
        {
            var players = Utilities.GetPlayers();
            var playersToUpdate = players.Where(p =>
                p?.IsValid == true &&
                !p.IsBot &&
                _playerData.TryGetValue(p.SteamID, out var data) &&
                data.ShowFaceitLevel &&
                data.FaceitLevel > 0).ToList();

            foreach (var player in playersToUpdate)
            {
                if (_playerData.TryGetValue(player.SteamID, out var playerData))
                {
                    ApplyPlayerCoin(player, playerData.FaceitLevel);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error in UpdatePlayerCoins");
        }
    }

    private void ApplyPlayerCoin(CCSPlayerController player, int faceitLevel)
    {
        try
        {
            if (player?.IsValid != true || player.PlayerPawn?.Value == null)
                return;

            var inventoryServices = player.InventoryServices;
            if (inventoryServices == null)
                return;

            var coinId = faceitLevel > 0 && FaceitLevelCoins.TryGetValue(faceitLevel, out var coin) ? coin : 0;

            inventoryServices.Rank[5] = (MedalRank_t)coinId;
            Utilities.SetStateChanged(player, "CCSPlayerController", "m_pInventoryServices");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"Error setting coin for player {player?.PlayerName}");
        }
    }

    private void ProcessSaveQueue()
    {
        if (_saveQueue.IsEmpty)
            return;

        var steamIdsToSave = new HashSet<ulong>();
        while (_saveQueue.TryDequeue(out var steamId))
        {
            steamIdsToSave.Add(steamId);
        }

        if (steamIdsToSave.Count > 0)
        {
            _ = SavePlayerDataBatchAsync(steamIdsToSave);
        }
    }

    private async Task SavePlayerDataBatchAsync(HashSet<ulong> steamIds)
    {
        if (!await _fileSemaphore.WaitAsync(5000))
            return;

        try
        {
            var directory = Path.GetDirectoryName(_dataPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory!);
            }

            var allData = new Dictionary<ulong, PlayerData>();

            // Load existing data
            if (File.Exists(_dataPath))
            {
                try
                {
                    var existingJson = await File.ReadAllTextAsync(_dataPath);
                    if (!string.IsNullOrEmpty(existingJson))
                    {
                        allData = JsonSerializer.Deserialize<Dictionary<ulong, PlayerData>>(existingJson, JsonOptions) ?? new();
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error reading existing player data");
                }
            }

            // Update data for players in the list
            foreach (var steamId in steamIds)
            {
                if (_playerData.TryGetValue(steamId, out var playerData))
                {
                    allData[steamId] = new PlayerData
                    {
                        ShowFaceitLevel = playerData.ShowFaceitLevel,
                        FaceitLevel = playerData.FaceitLevel,
                        LastFetch = playerData.LastFetch,
                        FaceitId = playerData.FaceitId,
                        IsProcessing = false // No need to save processing state
                    };
                }
            }

            // Write data to file
            var json = JsonSerializer.Serialize(allData, JsonOptions);
            await File.WriteAllTextAsync(_dataPath, json);

            Logger.LogDebug($"Saved data for {steamIds.Count} players");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error saving player data batch");
        }
        finally
        {
            _fileSemaphore.Release();
        }
    }

    private async Task LoadPlayerDataAsync()
    {
        try
        {
            if (!File.Exists(_dataPath))
            {
                Logger.LogInformation("No existing player data found");
                return;
            }

            var json = await File.ReadAllTextAsync(_dataPath);
            if (string.IsNullOrEmpty(json))
                return;

            var allData = JsonSerializer.Deserialize<Dictionary<ulong, PlayerData>>(json, JsonOptions);

            if (allData != null)
            {
                foreach (var (steamId, data) in allData)
                {
                    data.IsProcessing = false; // Reset processing state
                    _playerData.TryAdd(steamId, data);
                }
                Logger.LogInformation($"Loaded {allData.Count} player records");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading player data");
        }
    }
}