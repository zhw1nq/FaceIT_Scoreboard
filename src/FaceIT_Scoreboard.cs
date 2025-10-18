using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
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
    public List<string> Commands { get; set; } = new() { "css_faceit", "css_fl" };

    [JsonPropertyName("CacheExpiryHours")]
    public int CacheExpiryHours { get; set; } = 24;

    [JsonPropertyName("MaxConcurrentRequests")]
    public int MaxConcurrentRequests { get; set; } = 5;

    [JsonPropertyName("RequestTimeoutSeconds")]
    public int RequestTimeoutSeconds { get; set; } = 10;

    [JsonPropertyName("ConfigVersion")]
    public override int Version { get; set; } = 3;
}

public class PlayerData
{
    public bool ShowFaceitLevel { get; set; }
    public int FaceitLevel { get; set; }
    public DateTime LastFetch { get; set; }
    public bool IsProcessing { get; set; }
}

public class FaceitPlayer
{
    [JsonPropertyName("games")]
    public Dictionary<string, GameData>? Games { get; set; }
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
    public override string ModuleDescription => "Displays FaceIT levels on the scoreboard";
    public override string ModuleVersion => "1.1.0";

    public FaceitConfig Config { get; set; } = new();

    private HttpClient? _httpClient;
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

    private string _dataPath = "";
    private readonly HashSet<ulong> _dirtyPlayers = new();

    public void OnConfigParsed(FaceitConfig config)
    {
        Config = config;

        if (string.IsNullOrEmpty(Config.FaceitApiKey))
        {
            Server.PrintToConsole("[FaceIT] Warning: API key is not configured!");
        }

        _httpClient?.Dispose();
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(Config.RequestTimeoutSeconds)
        };
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {Config.FaceitApiKey}");
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

        _apiSemaphore?.Dispose();
        _apiSemaphore = new SemaphoreSlim(Config.MaxConcurrentRequests, Config.MaxConcurrentRequests);

        _dataPath = Path.Combine(ModuleDirectory, "data", "faceit_data.json");
    }

    public override void Load(bool hotReload)
    {
        RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnectFull);
        RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
        RegisterListener<Listeners.OnTick>(OnTick);

        if (Config.Commands.Count > 0)
        {
            foreach (var cmd in Config.Commands)
            {
                if (!string.IsNullOrWhiteSpace(cmd))
                {
                    AddCommand(cmd, "Toggle Faceit level display", OnFaceitCommand);
                }
            }
        }

        _ = LoadPlayerDataAsync();
        AddTimer(60.0f, SaveDirtyPlayers, CounterStrikeSharp.API.Modules.Timers.TimerFlags.REPEAT);

        Server.PrintToConsole($"[FaceIT] v{ModuleVersion} loaded");
    }

    public override void Unload(bool hotReload)
    {
        RemoveListener<Listeners.OnTick>(OnTick);
        SaveDirtyPlayers();

        _httpClient?.Dispose();
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
        var playerData = _playerData.GetOrAdd(steamId, _ => new PlayerData
        {
            ShowFaceitLevel = Config.DefaultStatus,
            FaceitLevel = 0,
            LastFetch = DateTime.MinValue,
            IsProcessing = false
        });

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
        if (player?.IsValid == true && !player.IsBot)
        {
            _dirtyPlayers.Add(player.SteamID);
        }

        return HookResult.Continue;
    }

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

        player.PrintToChat($" {ChatColors.Gold}[FaceIT] {ChatColors.Silver}Level display {color}{status}");

        if (playerData.ShowFaceitLevel)
        {
            if (playerData.FaceitLevel > 0)
            {
                ApplyPlayerCoin(player, playerData.FaceitLevel);
            }
            else if (ShouldFetchLevel(playerData))
            {
                _ = FetchPlayerFaceitLevelAsync(steamId);
            }
        }
        else
        {
            RemovePlayerCoin(player);
        }

        _dirtyPlayers.Add(steamId);
    }

    private void OnTick()
    {
        foreach (var player in Utilities.GetPlayers())
        {
            if (player?.IsValid != true || player.IsBot || player.PlayerPawn?.Value == null)
                continue;

            if (_playerData.TryGetValue(player.SteamID, out var data) && data.ShowFaceitLevel && data.FaceitLevel > 0)
            {
                ApplyPlayerCoin(player, data.FaceitLevel);
            }
        }
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
                _dirtyPlayers.Add(steamId);
            }
        }
        catch
        {
            if (_playerData.TryGetValue(steamId, out var playerData))
                playerData.IsProcessing = false;
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
            var level = await FetchFromFaceitApiAsync(steamId, "cs2");

            if (level == 0 && Config.UseCSGO)
            {
                level = await FetchFromFaceitApiAsync(steamId, "csgo");
            }

            return level;
        }
        catch
        {
            return 0;
        }
    }

    private async Task<int> FetchFromFaceitApiAsync(ulong steamId, string game)
    {
        if (_httpClient == null)
            return 0;

        try
        {
            var url = $"https://open.faceit.com/data/v4/players?game={game}&game_player_id={steamId}";
            using var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return 0;

            var json = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrEmpty(json))
                return 0;

            var player = JsonSerializer.Deserialize<FaceitPlayer>(json, JsonOptions);

            return player?.Games?.TryGetValue(game, out var gameData) == true ? gameData.SkillLevel : 0;
        }
        catch
        {
            return 0;
        }
    }

    private void ApplyPlayerCoin(CCSPlayerController player, int faceitLevel)
    {
        try
        {
            if (player?.IsValid != true || player.PlayerPawn?.Value == null || player.InventoryServices == null)
                return;

            var coinId = faceitLevel > 0 && FaceitLevelCoins.TryGetValue(faceitLevel, out var coin) ? coin : 0;

            // Xóa tất cả medals hiện tại trước khi đè
            for (int i = 0; i < 6; i++)
            {
                player.InventoryServices.Rank[i] = (MedalRank_t)0;
            }

            // Đè FaceIT medal vào slot 5
            player.InventoryServices.Rank[5] = (MedalRank_t)coinId;
            Utilities.SetStateChanged(player, "CCSPlayerController", "m_pInventoryServices");
        }
        catch
        {
        }
    }

    private void RemovePlayerCoin(CCSPlayerController player)
    {
        try
        {
            if (player?.IsValid != true || player.PlayerPawn?.Value == null || player.InventoryServices == null)
                return;

            player.InventoryServices.Rank[5] = (MedalRank_t)0;
            Utilities.SetStateChanged(player, "CCSPlayerController", "m_pInventoryServices");
        }
        catch
        {
        }
    }

    private void SaveDirtyPlayers()
    {
        if (_dirtyPlayers.Count > 0)
        {
            var toSave = new HashSet<ulong>(_dirtyPlayers);
            _dirtyPlayers.Clear();
            _ = SavePlayerDataBatchAsync(toSave);
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
                Directory.CreateDirectory(directory!);

            var allData = new Dictionary<ulong, PlayerData>();

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
                catch
                {
                }
            }

            foreach (var steamId in steamIds)
            {
                if (_playerData.TryGetValue(steamId, out var playerData))
                {
                    allData[steamId] = new PlayerData
                    {
                        ShowFaceitLevel = playerData.ShowFaceitLevel,
                        FaceitLevel = playerData.FaceitLevel,
                        LastFetch = playerData.LastFetch,
                        IsProcessing = false
                    };
                }
            }

            var json = JsonSerializer.Serialize(allData, JsonOptions);
            await File.WriteAllTextAsync(_dataPath, json);
        }
        catch
        {
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
                return;

            var json = await File.ReadAllTextAsync(_dataPath);
            if (string.IsNullOrEmpty(json))
                return;

            var allData = JsonSerializer.Deserialize<Dictionary<ulong, PlayerData>>(json, JsonOptions);

            if (allData != null)
            {
                foreach (var (steamId, data) in allData)
                {
                    data.IsProcessing = false;
                    _playerData.TryAdd(steamId, data);
                }
            }
        }
        catch
        {
        }
    }
}