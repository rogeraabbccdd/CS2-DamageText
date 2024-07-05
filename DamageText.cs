using System.Drawing;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Core.Attributes.Registration;

namespace DamageText;

public class DamageTextConfig : BasePluginConfig
{
    [JsonPropertyName("NormalSize")]
    public int NormalSize { get; set; } = 80;

    [JsonPropertyName("KillSize")]
    public int KillSize { get; set; } = 120;

    [JsonPropertyName("NormalColor")]
    public string NormalColor { get; set; } = "#ffffff";

    [JsonPropertyName("KillColor")]
    public string KillColor { get; set; } = "#ff0000";
    
    [JsonPropertyName("TextDisplayDuration")]
    public float TextDisplayDuration { get; set; } = 0.5f;
}

public class DamageTextPlugin : BasePlugin, IPluginConfig<DamageTextConfig>
{
    public override string ModuleName => "Damage Text";
    public override string ModuleVersion => "1.0.0";
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
        int health = @event.Health;
        // CCSPlayerController? attacker = @event.Attacker;
        // string weapon = @event.Weapon;

        if (
            victim == null || victim.PlayerPawn.Value == null || 
            victim.PlayerPawn.Value.AbsOrigin == null || victim.PlayerPawn.Value.AbsRotation == null
        ) return HookResult.Continue;

        Vector position = new Vector
        {
            X = victim.PlayerPawn.Value.AbsOrigin.X + (float)GetRandomDouble(10, 15, false),
            Y = victim.PlayerPawn.Value.AbsOrigin.Y + (float)GetRandomDouble(10, 15, false),
            Z = victim.PlayerPawn.Value.AbsOrigin.Z + (float)GetRandomDouble(20, 40)
        };

        QAngle angle = new QAngle
        {
            X = victim.PlayerPawn.Value.AbsRotation.X + 0.0f,
            Z = victim.PlayerPawn.Value.AbsRotation.Z + 90.0f,
            Y = (float)GetRandomDouble(0, 360)
        };
        ;

        ShowDamageText(
            damage.ToString(),
            health == 0 ? Config.KillColor : Config.NormalColor,
            health == 0 ? Config.KillSize : Config.NormalSize,
            position,
            angle
        );

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
        entity.Teleport(position, angle, new Vector(0,0,0));
        
        entity.DispatchSpawn();

        AddTimer(Config.TextDisplayDuration, entity.Remove);
    }
}
