using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;

namespace BombsiteAnnouncer;
public class Config : BasePluginConfig
{
    [JsonPropertyName("show-announcer-delay")]
    public float ShowAnnouncerDelay { get; set; } = 5.0f;
    [JsonPropertyName("announcer-visible-for-time")]
    public float AnnouncerVisibleForTime { get; set; } = 10.0f;
    [JsonPropertyName("bombsite-A-img")]
    public string BombsiteAimg { get; set; } = "https://i.imgur.com/Vjyuiqb.png";
    [JsonPropertyName("bombsite-B-img")]
    public string BombsiteBimg { get; set; } = "https://i.imgur.com/WIC4VHx.png";
}
public partial class BombsiteAnnouncer : BasePlugin, IPluginConfig<Config>
{
    public override string ModuleName => "BombsiteAnnouncer";
    public override string ModuleAuthor => "audio_brutalci";
    public override string ModuleDescription => "Simple bombsite announcer";
    public override string ModuleVersion => "V. 0.0.1";

    public required Config Config { get; set; }
    public bool bombsiteAnnouncer;
    public string? _site;
    public string? bombsite;
    public string? message;
    public string? color;
    public int ctNum;
    public int ttNum;

    public override void Load(bool hotReload)
    {
        Logger.LogInformation("BombsiteAnnouncer Plugin has started!");

        RegisterListener<Listeners.OnTick>(() =>
        {
            if (bombsiteAnnouncer == true)
            {
                foreach (var player in Utilities.GetPlayers().Where(player => player is { IsValid: true, IsBot: false }))
                {
                    if (IsValid(player) && IsConnected(player))
                    {
                        OnTick(player);
                    }
                }
            }
        });
    }
    public void OnConfigParsed(Config config)
    {
        Config = config;
    }
    private void OnTick(CCSPlayerController player)
    {
        ctNum = GetCurrentNumPlayers(CsTeam.CounterTerrorist);
        ttNum = GetCurrentNumPlayers(CsTeam.Terrorist);
        if (player.Team == CsTeam.CounterTerrorist)
        {
            color = "green";
            message = "RETAKE";
        }
        else
        {
            color = "red";
            message = "DEFEND";
        }
        player.PrintToCenterHtml(
        $"<font class='fontSize-l' color='{color}'>{message} <font color='white'>SITE</font> <font color='{color}'>{bombsite}</font><br>" +
        $"<img src='{_site}'><br><br>" +
        $"<font class='fontSize-m' color='white'>{ttNum}</font> <font class='fontSize-m'color='red'>T   </font><font class='fontSize-m' color='white'> vs.</font>   <font class='fontSize-m' color='white'> {ctNum}   </font><font class='fontSize-m' color='blue'>CT</font>"
        );
    }
    //---- P L U G I N - H O O O K S ----
    [GameEventHandler]
    public HookResult OnBombPlanted(EventBombPlanted @event, GameEventInfo info)
    {
        var c4list = Utilities.FindAllEntitiesByDesignerName<CC4>("weapon_c4");
        var c4 = c4list.FirstOrDefault();
        var site = new CBombTarget(NativeAPI.GetEntityFromIndex(@event.Site));
        _site = "";
        bombsite = "";
        if (site.IsBombSiteB)
        {
            _site = $"{Config.BombsiteBimg}";
            bombsite = "B";
        }
        if (!site.IsBombSiteB)
        {
            _site = $"{Config.BombsiteAimg}";
            bombsite = "A";
        }
        ShowAnnouncer();
        Logger.LogInformation($"Bomb Planted on [{bombsite}]");
        return HookResult.Continue;
    }
    [GameEventHandler]
    public HookResult OnBombDefused(EventBombDefused @event, GameEventInfo info)
    {
        bombsiteAnnouncer = false;
        return HookResult.Continue;
    }
    [GameEventHandler]
    public HookResult OnBombDetonate(EventBombExploded @event, GameEventInfo info)
    {
        bombsiteAnnouncer = false;
        return HookResult.Continue;
    }
    [GameEventHandler]
    public HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
    {
        bombsiteAnnouncer = false;
        return HookResult.Continue;
    }
    [GameEventHandler]
    public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        bombsiteAnnouncer = false;
        return HookResult.Continue;
    }
    //---- P L U G I N - H E L P E R S ----
    static bool IsValid(CCSPlayerController? player)
    {
        return player != null && player.IsValid && !player.IsBot && player.PlayerPawn.IsValid;
    }
    static bool IsConnected(CCSPlayerController? player)
    {
        return player?.Connected == PlayerConnectedState.PlayerConnected;
    }
    static bool IsAlive(CCSPlayerController player)
    {
        return player.PawnIsAlive;
    }
    public void ShowAnnouncer()
    {
        AddTimer(Config.ShowAnnouncerDelay, () =>
        {
            bombsiteAnnouncer = true;
            AddTimer(Config.AnnouncerVisibleForTime, () => { bombsiteAnnouncer = false; });
        });
    }
    // Credits B3none
    public static int GetCurrentNumPlayers(CsTeam? csTeam = null)
    {
        var players = 0;

        foreach (var player in Utilities.GetPlayers()
                     .Where(player => IsAlive(player) && IsValid(player) && IsConnected(player)))
        {
            if (csTeam == null || player.Team == csTeam)
            {
                players++;
            }
        }

        return players;
    }
}
