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
    public float ShowAnnouncerDelay { get; set; } = 0.1f;
    [JsonPropertyName("announcer-visible-for-time")]
    public float AnnouncerVisibleForTime { get; set; } = 10.0f;
    [JsonPropertyName("remove-bomb-planted-message")]
    public bool RemoveDefaultMsg { get; set; } = true;
    [JsonPropertyName("bombsite-A-img")]
    public string BombsiteAimg { get; set; } = "https://raw.githubusercontent.com/audiomaster99/CS2BombsiteAnnouncer/main/.github/workflows/new-A.png";
    [JsonPropertyName("bombsite-B-img")]
    public string BombsiteBimg { get; set; } = "https://raw.githubusercontent.com/audiomaster99/CS2BombsiteAnnouncer/main/.github/workflows/new-B.png";
}
public partial class BombsiteAnnouncer : BasePlugin, IPluginConfig<Config>
{
    public override string ModuleName => "BombsiteAnnouncer";
    public override string ModuleAuthor => "audio_brutalci";
    public override string ModuleDescription => "Simple bombsite announcer";
    public override string ModuleVersion => "V. 0.0.3";

    public required Config Config { get; set; }
    public bool bombsiteAnnouncer;
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
                Utilities.GetPlayers().Where(player => IsValid(player) && IsConnected(player)).ToList().ForEach(p => OnTick(p));
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

        // determine site image
        string siteImage = bombsite == "B" ? Config.BombsiteBimg : bombsite == "A" ? Config.BombsiteAimg : "";
        if (siteImage == "")
        {
            Logger.LogWarning($"Unknown bombsite value: {bombsite}");
        }

        if (player.Team == CsTeam.CounterTerrorist)
        {
            color = "green";
            message = Localizer["phrases.retake"];
        }
        else
        {
            color = "red";
            message = Localizer["phrases.defend"];
        }

        player.PrintToCenterHtml(
            $"<font class='fontSize-l' color='{color}'>{message} <font color='white'>SITE</font> <font color='{color}'>{bombsite}</font><br>" +
            $"<img src='{siteImage}'><br><br>" +
            $"<font class='fontSize-m' color='white'>{ttNum}</font> <font class='fontSize-m'color='red'>{Localizer["phrases.terrorist"]}   </font><font class='fontSize-m' color='white'> {Localizer["phrases.versus"]}</font>   <font class='fontSize-m' color='white'> {ctNum}   </font><font class='fontSize-m' color='blue'>{Localizer["phrases.cterrorist"]}</font>"
        );
    }

    //---- P L U G I N - H O O O K S ----
    [GameEventHandler(HookMode.Pre)]
    public HookResult OnBombPlanted(EventBombPlanted @event, GameEventInfo info)
    {

        CBombTarget site = new CBombTarget(NativeAPI.GetEntityFromIndex(@event.Site));

        //bombsite = site.IsBombSiteB ? "B" : "A";
        bombsite = (@event.Site == 1) ? "B" : "A";

        ShowAnnouncer();
        Logger.LogInformation($"Bomb Planted on [{bombsite}]");

        // remove bomb planted message
        if (Config.RemoveDefaultMsg && @event != null)
        {
            return HookResult.Handled;
        }
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
        return player?.IsValid == true && player.PlayerPawn?.IsValid == true && !player.IsBot && !player.IsHLTV;
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
        return Utilities.GetPlayers().Count(player => IsAlive(player) && IsValid(player) && IsConnected(player) && (csTeam == null || player.Team == csTeam));
    }
}
