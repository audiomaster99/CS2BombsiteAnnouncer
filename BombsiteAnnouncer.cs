using System;
using System.Linq;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Utils;

namespace BombsiteAnnouncer
{
    public class Config : BasePluginConfig
    {
        [JsonPropertyName("show-announcer-delay")]
        public float ShowAnnouncerDelay { get; set; } = 0.1f;

        [JsonPropertyName("announcer-visible-for-time")]
        public float AnnouncerVisibleForTime { get; set; } = 5.0f;

        [JsonPropertyName("remove-bomb-planted-message")]
        public bool RemoveDefaultMsg { get; set; } = true;

        [JsonPropertyName("bombsite-A-img")]
        public string BombsiteAimg { get; set; } = "https://i.ibb.co/LQTms8L/sidea.png";

        [JsonPropertyName("bombsite-B-img")]
        public string BombsiteBimg { get; set; } = "https://i.ibb.co/SmjktzK/sideb.png";
    }

    public partial class BombsiteAnnouncer : BasePlugin, IPluginConfig<Config>
    {
        public override string ModuleName => "BombsiteAnnouncer";
        public override string ModuleAuthor => "audio_brutalci";
        public override string ModuleDescription => "Simple bombsite announcer";
        public override string ModuleVersion => "V. 0.0.2";

        public Config Config { get; set; }

        public BombsiteAnnouncer()
        {
            Config = new Config(); // Initialize Config in the constructor
        }

        public bool BombsiteAnnouncerActive;
        public string? Bombsite;
        public string? Message;
        public string? Color;
        public string? Color2;
        public int CTNum;
        public int TTNum;

        public override void Load(bool hotReload)
        {
            RegisterListener<Listeners.OnTick>(() =>
            {
                if (BombsiteAnnouncerActive)
                {
                    foreach (var player in Utilities.GetPlayers().Where(player => IsValid(player) && IsConnected(player)))
                    {
                        OnTick(player);
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
            CTNum = GetCurrentNumPlayers(CsTeam.CounterTerrorist);
            TTNum = GetCurrentNumPlayers(CsTeam.Terrorist);

            // Determine site image
            string siteImage = Bombsite == "B" ? Config.BombsiteBimg : Config.BombsiteAimg;

            if (player.Team == CsTeam.CounterTerrorist)
            {
                Color = "green";
                Color2 = "red";
                Message = "RETAKE ON";
            }
            else
            {
                Color = "red";
                Color2 = "green";
                Message = "DEFEND THE";
            }

            player.PrintToCenterHtml(
                $"<font class='fontSize-l' color='{Color}'>{Message} <font color='{Color2}'>{Bombsite}</font> <font color='white'>SITE</font><br>" +
                $"<img src='{siteImage}'><br><br>" +
                $"<font class='fontSize-m' color='white'>{TTNum}</font> <font class='fontSize-m'color='red'>T   </font><font class='fontSize-m' color='white'> VS</font>   <font class='fontSize-m' color='white'> {CTNum}   </font><font class='fontSize-m' color='blue'>CT</font>"
            );
        }

        [GameEventHandler(HookMode.Pre)]
        public HookResult OnBombPlanted(EventBombPlanted @event, GameEventInfo info)
        {
            var c4list = Utilities.FindAllEntitiesByDesignerName<CC4>("weapon_c4");
            var c4 = c4list.FirstOrDefault();
            var site = new CBombTarget(NativeAPI.GetEntityFromIndex(@event.Site));

            Bombsite = site.IsBombSiteB ? "B" : "A";
            ShowAnnouncer();

            // Remove bomb planted message
            if (Config.RemoveDefaultMsg && @event != null)
            {
                return HookResult.Handled;
            }
            return HookResult.Continue;
        }

        [GameEventHandler]
        public HookResult OnBombDefused(EventBombDefused @event, GameEventInfo info)
        {
            BombsiteAnnouncerActive = false;
            return HookResult.Continue;
        }

        [GameEventHandler]
        public HookResult OnBombDetonate(EventBombExploded @event, GameEventInfo info)
        {
            BombsiteAnnouncerActive = false;
            return HookResult.Continue;
        }

        [GameEventHandler]
        public HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
        {
            BombsiteAnnouncerActive = false;
            return HookResult.Continue;
        }

        [GameEventHandler]
        public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
        {
            BombsiteAnnouncerActive = false;
            return HookResult.Continue;
        }

        // Plugin helpers
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
                BombsiteAnnouncerActive = true;
                AddTimer(Config.AnnouncerVisibleForTime, () => { BombsiteAnnouncerActive = false; });
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
}
