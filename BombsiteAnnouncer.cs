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
    [JsonPropertyName("show-site-info-text")]
    public bool SiteText { get; set; } = true;
    [JsonPropertyName("show-site-info-image")]
    public bool SiteImage { get; set; } = true;
    [JsonPropertyName("show-player-counter")]
    public bool PlayerCounter { get; set; } = true;
    [JsonPropertyName("ConfigVersion")]
    public override int Version { get; set; } = 2;
}
public partial class BombsiteAnnouncer : BasePlugin, IPluginConfig<Config>
{
    public override string ModuleName => "BombsiteAnnouncer";
    public override string ModuleAuthor => "audio_brutalci";
    public override string ModuleDescription => "Simple bombsite announcer";
    public override string ModuleVersion => "V. 0.0.4";

    public required Config Config { get; set; }
    public bool bombsiteAnnouncer;
    public bool isRetakesEnabled;
    public string? bombsite;
    public string? message;
    public string? color;
    public string? siteTextString;
    public string? siteImageString;
    public string? playerCounterString;
    public string? siteImage;
    public string? breakLine;
    public int ctNum;
    public int ttNum;

    public override void Load(bool hotReload)
    {
        Logger.LogInformation("BombsiteAnnouncer Plugin has started!");

        AddTimer(0.1f, () => { IsRetakesPluginInstalled(); });

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
        ctNum = GetCurrentNumPlayers(CsTeam.CounterTerrorist);
        ttNum = GetCurrentNumPlayers(CsTeam.Terrorist);
        HandleCustomizedMessage();
        player.PrintToCenterHtml(siteTextString + siteImageString + playerCounterString);
    }

    public void ShowAnnouncer()
    {
        AddTimer(Config.ShowAnnouncerDelay, () =>
        {
            bombsiteAnnouncer = true;
            AddTimer(Config.AnnouncerVisibleForTime, () => { bombsiteAnnouncer = false; });
        });
    }

    public void HandleCustomizedMessage()
    {
        siteTextString = Config.SiteText ? $"<font class='fontSize-l' color='{color}'>{message} <font color='white'>{Localizer["phrases.site"]}</font> <font color='{color}'>{bombsite}</font>{breakLine}" : "";
        siteImageString = Config.SiteImage ? $"<img src='{siteImage}'>  {breakLine}" : "";
        playerCounterString = Config.PlayerCounter ? $"<font class='fontSize-m' color='white'>{ttNum}</font> <font class='fontSize-m'color='red'>{Localizer["phrases.terrorist"]}   </font><font class='fontSize-m' color='white'> {Localizer["phrases.versus"]}</font>   <font class='fontSize-m' color='white'> {ctNum}   </font><font class='fontSize-m' color='blue'>{Localizer["phrases.cterrorist"]}</font>" : "";

        //fix bad looking message if some lines are not displayed
        //caused by line-break
        if (!Config.SiteText && !Config.PlayerCounter && Config.SiteImage) { breakLine = ""; }
        else if (Config.SiteText && !Config.PlayerCounter && !Config.SiteImage) { breakLine = ""; }
        else { breakLine = "<br>"; }
    }

    public void GetSiteImage()
    {
        siteImage = bombsite == "B" ? Config.BombsiteBimg : bombsite == "A" ? Config.BombsiteAimg : "";
        if (siteImage == "")
        {
            Logger.LogWarning($"Unknown bombsite value: {bombsite}");
        }
    }
    //---- P L U G I N - H O O O K S ----
    [GameEventHandler(HookMode.Pre)]
    public HookResult OnBombPlanted(EventBombPlanted @event, GameEventInfo info)
    {

        CCSPlayerController player = @event.Userid;
        CBombTarget site = new CBombTarget(NativeAPI.GetEntityFromIndex(@event.Site));

        if (isRetakesEnabled == true)
        {
            bombsite = (@event.Site == 1) ? "B" : "A";
        }
        else bombsite = site.IsBombSiteB ? "B" : "A";

        GetSiteImage();
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
    public static int GetCurrentNumPlayers(CsTeam? csTeam = null)
    {
        return Utilities.GetPlayers().Count(player => IsAlive(player) && IsValid(player) && IsConnected(player) && (csTeam == null || player.Team == csTeam));
    }
    public void IsRetakesPluginInstalled()
    {
        string? path = Directory.GetParent(ModuleDirectory)?.FullName;
        if (Directory.Exists(path + "/RetakesPlugin"))
        {
            Logger.LogInformation("RETAKES MODE ENABLED");
            isRetakesEnabled = true;
        }
        else isRetakesEnabled = false;
    }
}
