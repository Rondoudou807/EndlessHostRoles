using System.Collections.Generic;
using AmongUs.GameOptions;
using EHR.Modules;

namespace EHR.Roles;

public class Rondoudou : RoleBase
{
    private const int Id = 712400;
    private static readonly List<byte> PlayerIdList = [];
    private static readonly HashSet<byte> AffectedPlayers = [];
    private static readonly Dictionary<byte, int> EffectVersions = [];

    private static OptionItem AbilityCooldown;
    private static OptionItem EffectDuration;
    private static OptionItem VisionLevel;

    public override bool IsEnable => PlayerIdList.Count > 0;

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Rondoudou);

        AbilityCooldown = new FloatOptionItem(Id + 1, "AbilityCooldown", new(2.5f, 120f, 0.5f), 30f, TabGroup.ImpostorRoles)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Rondoudou])
            .SetValueFormat(OptionFormat.Seconds);

        EffectDuration = new FloatOptionItem(Id + 2, "RondoudouDuration", new(1f, 60f, 1f), 20f, TabGroup.ImpostorRoles)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Rondoudou])
            .SetValueFormat(OptionFormat.Seconds);

        VisionLevel = new FloatOptionItem(Id + 3, "RondoudouVision", new(0.05f, 1f, 0.05f), 0.15f, TabGroup.ImpostorRoles)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Rondoudou])
            .SetValueFormat(OptionFormat.Multiplier);
    }

    public override void Init()
    {
        PlayerIdList.Clear();
        AffectedPlayers.Clear();
        EffectVersions.Clear();
    }

    public override void Add(byte playerId)
    {
        PlayerIdList.Add(playerId);
    }

    public override void Remove(byte playerId)
    {
        PlayerIdList.Remove(playerId);
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.ShapeshifterCooldown = AbilityCooldown.GetFloat();
        AURoleOptions.ShapeshifterDuration = 1f;
    }

    public override bool OnShapeshift(PlayerControl shapeshifter, PlayerControl target, bool shapeshifting)
    {
        if (!shapeshifting || !AmongUsClient.Instance.AmHost || !target || target.PlayerId == shapeshifter.PlayerId || !target.IsAlive())
            return false;

        byte targetId = target.PlayerId;
        int version = EffectVersions.GetValueOrDefault(targetId) + 1;
        EffectVersions[targetId] = version;
        AffectedPlayers.Add(targetId);

        Main.AllPlayerSpeed[targetId] = Main.MinSpeed;
        target.MarkDirtySettings();
        target.Notify(Translator.GetString("RondoudouAffected"));

        LateTask.New(() =>
        {
            if (!EffectVersions.TryGetValue(targetId, out int currentVersion) || currentVersion != version)
                return;

            AffectedPlayers.Remove(targetId);
            EffectVersions.Remove(targetId);

            PlayerControl affected = Utils.GetPlayerById(targetId);
            if (affected)
            {
                Main.AllPlayerSpeed[targetId] = Main.RealOptionsData.GetFloat(FloatOptionNames.PlayerSpeedMod);
                affected.MarkDirtySettings();
            }
        }, EffectDuration.GetFloat(), "RondoudouEffect");

        Utils.NotifyRoles(SpecifySeer: shapeshifter, SpecifyTarget: target);
        return false;
    }

    public static bool IsSilenced(byte playerId) => AffectedPlayers.Contains(playerId);

    public static void SetBlindness(PlayerControl player, IGameOptions opt)
    {
        if (!AffectedPlayers.Contains(player.PlayerId))
            return;

        opt.SetVision(true);
        opt.SetFloat(FloatOptionNames.CrewLightMod, VisionLevel.GetFloat());
        opt.SetFloat(FloatOptionNames.ImpostorLightMod, VisionLevel.GetFloat());
    }
}
