using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;

namespace BombsiteAnnouncer;

public partial class BombsiteAnnouncer : BasePlugin
{
    public override string ModuleName => "BombsiteAnnouncer";
    public override string ModuleAuthor => "audio_brutalci";
    public override string ModuleDescription => "Simple bombsite announcer";
    public override string ModuleVersion => "V. 0.0.1";

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
    private void OnTick(CCSPlayerController player)
    {
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
        $"<font class='fontSize-m' color='white'>{ttNum}</font> <font class='fontSize-m'color='red'>TT   </font><font class='fontSize-m' color='white'> vs.</font>   <font class='fontSize-m' color='white'> {ctNum}   </font><font class='fontSize-m' color='blue'>CT</font>"
        );
    }
    //---- P L U G I N - H O O O K S ----
    [GameEventHandler]
    public HookResult OnBombPlanted(EventBombPlanted @event, GameEventInfo info)
    {
        ctNum = GetCurrentNumPlayers(CsTeam.CounterTerrorist);
        ttNum = GetCurrentNumPlayers(CsTeam.Terrorist);
        var site = new CBombTarget(NativeAPI.GetEntityFromIndex(@event.Site));
        _site = "";
        bombsite = "";
        if (site.IsBombSiteB)
        {
            _site = $"https://i.imgur.com/WIC4VHx.png";
            bombsite = "B";
        }
        else
        {
            _site = $"https://i.imgur.com/Vjyuiqb.png";
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

    //---- P L U G I N - H E L P E R S ----
    static bool IsValid(CCSPlayerController? player)
    {
        return player != null && player.IsValid && !player.IsBot && player.PlayerPawn.IsValid;
    }
    static bool IsConnected(CCSPlayerController? player)
    {
        return player?.Connected == PlayerConnectedState.PlayerConnected;
    }
    public void ShowAnnouncer()
    {
        bombsiteAnnouncer = true;
        AddTimer(10.0f, () => { bombsiteAnnouncer = false; });
    }
    // Credits B3none
    public static int GetCurrentNumPlayers(CsTeam? csTeam = null)
    {
        var players = 0;

        foreach (var player in Utilities.GetPlayers()
                     .Where(player => IsValid(player) && IsConnected(player)))
        {
            if (csTeam == null || player.Team == csTeam)
            {
                players++;
            }
        }

        return players;
    }
}
