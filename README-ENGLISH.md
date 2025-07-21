# 🎯 FaceIT Scoreboard Plugin

[![Source 2](https://img.shields.io/badge/Source%202-orange?style=for-the-badge&logo=valve&logoColor=white)](https://developer.valvesoftware.com/wiki/Source_2)
[![CounterStrikeSharp](https://img.shields.io/badge/CounterStrikeSharp-blue?style=for-the-badge&logo=counter-strike&logoColor=white)](https://github.com/roflmuffin/CounterStrikeSharp)
[![.NET](https://img.shields.io/badge/.NET-8.0-purple?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-11.0-green?style=for-the-badge&logo=csharp&logoColor=white)](https://docs.microsoft.com/en-us/dotnet/csharp/)

[![Stars](https://img.shields.io/github/stars/zhw1nq/FaceIT_Scoreboard?style=social)](https://github.com/zhw1nq/FaceIT_Scoreboard/stargazers)
[![Forks](https://img.shields.io/github/forks/zhw1nq/FaceIT_Scoreboard?style=social)](https://github.com/zhw1nq/FaceIT_Scoreboard/network/members)
[![Watchers](https://img.shields.io/github/watchers/zhw1nq/FaceIT_Scoreboard?style=social)](https://github.com/zhw1nq/FaceIT_Scoreboard/watchers)

📖 **[README.md](README.md)** | **[README-EN.md](README-EN.md)**

> **Display FaceIT levels on the scoreboard – know who's carrying at a glance.**

This CounterStrikeSharp plugin enhances your CS2 server by displaying FaceIT skill levels directly on the scoreboard using custom coins/medals. Players can toggle FaceIT level display on/off and the plugin efficiently stores data to minimize API calls.

## ✨ Features

- 🏆 **FaceIT Level Display**: Show FaceIT skill levels (1-10) as custom coins on the scoreboard
- ⚡ **Real-time Updates**: Automatically fetch and update player FaceIT levels
- 🔄 **Player Control**: Toggle FaceIT level display on/off with simple commands
- 💾 **Smart Caching**: Efficient caching system to reduce API calls and improve performance
- 🎮 **Multi-game Support**: Supports both FaceIT CS2 and CSGO data
- ⚙️ **Configurable**: Multiple configuration options for customization
- 💿 **Persistent Data**: Player preferences saved across server restarts

## 🎨 FaceIT Level Coins

The plugin uses custom coin IDs to represent different FaceIT skill levels:

| Level | Coin ID | Description |
|-------|---------|-------------|
| 1     | 1088    | Level 1     |
| 2     | 1087    | Level 2     |
| 3     | 1032    | Level 3     |
| 4     | 1055    | Level 4     |
| 5     | 1041    | Level 5     |
| 6     | 1074    | Level 6     |
| 7     | 1039    | Level 7     |
| 8     | 1067    | Level 8     |
| 9     | 1061    | Level 9     |
| 10    | 1017    | Level 10    |

## 📋 Requirements

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) (Minimum API Version: 147)
- CS2 Dedicated Server
- FaceIT API Key

## 🔧 Installation

1. **Download** and install the latest release
2. **Configure** the config file (`addons/counterstrikesharp/configs/plugins/FaceIT_Scoreboard/FaceIT_Scoreboard.json`)
3. **Download** the workshop collection located in the root folder of the current releases and install WITHOUT COMPILATION
   
   **Note**: Place the files not in `content/csgo_addons/****`, but in the path `game/csgo_addons/****`

4. **Restart** the server or use `css_plugins reload`

## ⚙️ Configuration

The plugin creates a configuration file at:
```
/game/csgo/addons/counterstrikesharp/configs/plugins/FaceIT_Scoreboard/FaceIT_Scoreboard.json
```

### Default Configuration

```json
{
  "FaceitApiKey": "",
  "UseCSGO": false,
  "DefaultStatus": true,
  "Commands": ["!faceit", "!fl"],
  "CacheExpiryHours": 24,
  "MaxConcurrentRequests": 10,
  "RequestTimeoutSeconds": 10,
  "ConfigVersion": 2
}
```

### Configuration Options

| Option | Type | Description | Default |
|--------|------|-------------|---------|
| `FaceitApiKey` | string | Your FaceIT API key (**Required**) | `""` |
| `UseCSGO` | boolean | Fallback to CSGO data if CS2 not found | `false` |
| `DefaultStatus` | boolean | Default FaceIT level display for new players | `true` |
| `Commands` | array | Commands to toggle FaceIT display | `["!faceit", "!fl"]` |
| `CacheExpiryHours` | integer | Hours before reloading player data | `24` |
| `MaxConcurrentRequests` | integer | Maximum concurrent API requests | `10` |
| `RequestTimeoutSeconds` | integer | API request timeout | `10` |

### 🔑 Getting FaceIT API Key

1. Visit [FaceIT Developer Portal](https://developers.faceit.com/)
2. Log in with your FaceIT account
3. Create a new application
4. Copy the API key to your config file

## 🎮 Commands

### Player Commands

| Command | Alias | Description |
|---------|-------|-------------|
| `!faceit` | `!fl` | Toggle FaceIT level display |

### Console Commands

| Command | Description |
|---------|-------------|
| `css_faceit` | Toggle FaceIT level display (console) |
| `css_fl` | Toggle FaceIT level display (console) |

## 📁 File Structure

```
addons/counterstrikesharp/plugins/FaceIT_Scoreboard/
├── FaceIT_Scoreboard.dll          # Main plugin file
├── FaceIT_Scoreboard.pdb          # Debug symbols
└── data/
    └── faceit_data.json           # Player data cache
```

## 🐛 Troubleshooting

### Common Issues

1. **FaceIT levels not showing**
   - Check if FaceIT API key is configured correctly
   - Verify player has FaceIT account linked to Steam ID
   - Check server console for API errors

2. **Plugin not loading**
   - Ensure minimum CounterStrikeSharp version (147) is met
   - Verify file permissions
   - Check for conflicting plugins

3. **Performance issues**
   - Reduce `MaxConcurrentRequests` value
   - Increase `CacheExpiryHours` to reduce API calls
   - Monitor server resources

## 🙏 Credits

- **Original Idea**: Based on the idea from [Pisex's cs2-faceit-level](https://github.com/Pisex/cs2-faceit-level)
- **Framework**: [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) by roflmuffin
- **API**: [FaceIT Data API](https://developers.faceit.com/)

## 💬 Support & Community

- **Discord Support**: [@vhming_](https://discord.com/users/vhming_)
- **CounterStrikeSharp Community**: [Join Discord](https://discord.gg/eA9QTuNYkp)

---

<div align="center">
<i>Made with ❤️ for the CS2 community</i>
</div>
