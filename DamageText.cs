using System.Drawing;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Core.Attributes.Registration;

namespace DamageText;

public class DamageTextConfig : BasePluginConfig
{
    [JsonPropertyName("ConfigVersion")]
    public override int Version { get; set; } = 3;

    [JsonPropertyName("NormalSize")]
    public int NormalSize { get; set; } = 80;

    [JsonPropertyName("NormalHeadshotSize")]
    public int NormalHeadshotSize { get; set; } = 90;

    [JsonPropertyName("KillSize")]
    public int KillSize { get; set; } = 120;

    [JsonPropertyName("KillHeadshotSize")]
    public int KillHeadshotSize { get; set; } = 120;

    [JsonPropertyName("NormalColor")]
    public string NormalColor { get; set; } = "#ffffff";

    [JsonPropertyName("NormalHeadshotColor")]
    public string NormalHeadshotColor { get; set; } = "#ffff00";

    [JsonPropertyName("KillColor")]
    public string KillColor { get; set; } = "#ff0000";

    [JsonPropertyName("KillHeadshotColor")]
    public string KillHeadshotColor { get; set; } = "#ff0000";
    
    [JsonPropertyName("TextDisplayDuration")]
    public float TextDisplayDuration { get; set; } = 0.5f;

    [JsonPropertyName("EnableShadow")]
    public bool EnableShadow { get; set; } = false;
}

public class DamageTextPlugin : BasePlugin, IPluginConfig<DamageTextConfig>
{
    public override string ModuleName => "Damage Text";
    public override string ModuleVersion => "1.1.0";
    public override string ModuleAuthor => "Kento";
    public override string ModuleDescription => "Show damage text like RPG games :D";
    public DamageTextConfig Config { get; set; } = new();
    private Random random = new Random();
    public override void Load(bool hotReload)
    {
    }
    public override void Unload(bool hotReload)
    {
    }
    public void OnConfigParsed(DamageTextConfig config)
    {
        Config = config;
    }
    [GameEventHandler]
    public HookResult OnPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
    {
        CCSPlayerController? victim = @event.Userid;
        int damage = @event.DmgHealth;
        CCSPlayerController? attacker = @event.Attacker;
        bool headshot = @event.Hitgroup == 1;
        bool kill = @event.Health <= 0; 

        if (
            victim == null || victim.PlayerPawn.Value == null || 
            victim.PlayerPawn.Value.AbsOrigin == null || victim.PlayerPawn.Value.AbsRotation == null
        ) return HookResult.Continue;

        if (
            attacker == null || attacker.PlayerPawn.Value == null ||
            attacker.PlayerPawn.Value.AbsOrigin == null || attacker.PlayerPawn.Value.AbsRotation == null
        ) return HookResult.Continue;

        //Ensure text is towards attacker, to avoid it spawning behind victim.
        var offset = 40;
        var attackerOrigin = attacker.PlayerPawn.Value!.AbsOrigin;
        var victimOrigin = victim.PlayerPawn.Value!.AbsOrigin;
        float deltaX = attackerOrigin.X - victimOrigin.X;
        float deltaY = attackerOrigin.Y - victimOrigin.Y;
        float deltaZ = attackerOrigin.Z - victimOrigin.Z;
        float distance = (float)Math.Sqrt(deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ);
        float newX = victimOrigin.X + (deltaX / distance) * offset;
        float newY = victimOrigin.Y + (deltaY / distance) * offset;
        float newZ = victimOrigin.Z + (deltaZ / distance) * offset;

        Vector position = new Vector
        {
            X = newX + (float)GetRandomDouble(10, 15, false),
            Y = newY + (float)GetRandomDouble(10, 15, false),
            Z = newZ + (float)GetRandomDouble(20, 80)
        };

        QAngle angle = new QAngle
        {
            X = victim.PlayerPawn.Value.AbsRotation.X + 0.0f,
            Z = victim.PlayerPawn.Value.AbsRotation.Z + 90.0f,
            Y = (attacker == null || attacker.PlayerPawn.Value == null) ? 
                (float)GetRandomDouble(0, 360) :
                attacker.PlayerPawn.Value.EyeAngles.Y - 90f
        };

        int size;
        string color;
        if (kill)
        {
            if (headshot)
            {
                color = Config.KillHeadshotColor;
                size = Config.KillHeadshotSize;
            }
            else
            {
                color = Config.KillColor;
                size = Config.KillSize;
            }
        }
        else
        {
            if (headshot)
            {
                color = Config.NormalHeadshotColor;
                size = Config.NormalHeadshotSize;
            }
            else
            {
                color = Config.NormalColor;
                size = Config.NormalSize;
            }
        }

        ShowDamageText(
            damage.ToString(),
            color,
            size,
            position,
            angle
        );

        if (Config.EnableShadow)
        {
            float shadowX = position.X + (deltaX / distance) * -1;
            float shadowY = position.Y + (deltaY / distance) * -1;
            float shadowZ = position.Z + (deltaZ / distance) * -1;

            ShowDamageText(
                damage.ToString(),
                "#080808",
                size,
                new Vector(shadowX, shadowY, shadowZ - (size * 0.005f)),
                angle
            );
        }

        return HookResult.Continue;
    }
    public double GetRandomDouble(double min, double max, bool positive = true)
    { 
        if (positive  || random.Next(0, 2) == 0)
        {
            // 從 min 到 max 的範圍選擇
            return min + random.NextDouble() * (max - min); 
        }
        else
        {
            // 從 -min 到 -max 的範圍選擇
            return -min + random.NextDouble() * (-max - -min); 
        }
    }
    private void ShowDamageText (string text, string color, int size, Vector position, QAngle angle)
    {
        var entity = Utilities.CreateEntityByName<CPointWorldText>("point_worldtext");
        if (entity == null) return;

        entity.DispatchSpawn();
        entity.MessageText = text;
        entity.Enabled = true;
        entity.Color = ColorTranslator.FromHtml(color);
        entity.FontSize = size;
        entity.Fullbright = true;
        entity.WorldUnitsPerPx = 0.1f;
        entity.DepthOffset = 0.0f;
        entity.JustifyHorizontal = PointWorldTextJustifyHorizontal_t.POINT_WORLD_TEXT_JUSTIFY_HORIZONTAL_CENTER;
        entity.JustifyVertical = PointWorldTextJustifyVertical_t.POINT_WORLD_TEXT_JUSTIFY_VERTICAL_CENTER;
        entity.Teleport(position, angle, new Vector(0,0,0));

        AddTimer(Config.TextDisplayDuration, entity.Remove);
    }
}
