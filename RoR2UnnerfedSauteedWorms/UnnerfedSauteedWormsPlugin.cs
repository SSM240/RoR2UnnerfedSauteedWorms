using BepInEx;
using BepInEx.Configuration;
using R2API;
using RiskOfOptions;
using RiskOfOptions.Options;
using RoR2;
using RoR2.Items;
using System.IO;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.XR;

namespace RoR2UnnerfedSauteedWorms;

[BepInPlugin(PluginGUID, PluginName, PluginVersion)]
public class UnnerfedSauteedWormsPlugin : BaseUnityPlugin
{
    public const string PluginGUID = PluginAuthor + "." + PluginName;
    public const string PluginAuthor = "SSM24";
    public const string PluginName = "UnnerfedSauteedWorms";
    public const string PluginVersion = "1.0.0";

    public static ConfigEntry<bool> DisableCooldown;
    public static ConfigEntry<bool> DisableChanceDecay;
    public static ConfigEntry<bool> DisableWormLimit;

    public void Awake()
    {
        Log.Init(Logger);

        DisableCooldown = Config.Bind("General", "Disable Cooldown", true, 
            "Disables the 1-second cooldown between worm procs");
        DisableChanceDecay = Config.Bind("General", "Disable Chance Decay", true,
            "If true, proc chance is not reduced by the number of active worms");
        DisableWormLimit = Config.Bind("General", "Disable Worm Limit", true,
            "Disables the limit of 8 active worms at once");

        ModSettingsManager.AddOption(new CheckBoxOption(DisableCooldown));
        ModSettingsManager.AddOption(new CheckBoxOption(DisableChanceDecay));
        ModSettingsManager.AddOption(new CheckBoxOption(DisableWormLimit));

        // create icon from file
        // mostly taken from https://github.com/Vl4dimyr/CaptainShotgunModes/blob/fdf828e/RiskOfOptionsMod.cs#L36-L48
        // i have NO clue what this code is doing but it seems to work so... cool?
        try
        {
            using Stream stream = File.OpenRead(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Info.Location), "icon.png"));
            Texture2D texture = new Texture2D(0, 0);
            byte[] imgData = new byte[stream.Length];

            stream.Read(imgData, 0, (int)stream.Length);

            if (ImageConversion.LoadImage(texture, imgData))
            {
                ModSettingsManager.SetModIcon(
                    Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0, 0))
                );
            }
        }
        catch (FileNotFoundException)
        {
        }

        On.RoR2.Items.WyrmOnHitBehavior.TryFire += On_WyrmOnHitBehavior_TryFire;
        On.RoR2.Items.WyrmOnHitBehavior.SolveForProcChance += On_WyrmOnHitBehavior_SolveForProcChance;
        On.RoR2.CharacterMaster.GetDeployableSameSlotLimit += On_CharacterMaster_GetDeployableSameSlotLimit;
    }

    // for ScriptEngine compat
    public void OnDestroy()
    {
        On.RoR2.Items.WyrmOnHitBehavior.TryFire -= On_WyrmOnHitBehavior_TryFire;
        On.RoR2.Items.WyrmOnHitBehavior.SolveForProcChance -= On_WyrmOnHitBehavior_SolveForProcChance;
        On.RoR2.CharacterMaster.GetDeployableSameSlotLimit -= On_CharacterMaster_GetDeployableSameSlotLimit;
    }

    private static bool On_WyrmOnHitBehavior_TryFire(
        On.RoR2.Items.WyrmOnHitBehavior.orig_TryFire orig, WyrmOnHitBehavior self, ref DamageInfo damageInfo)
    {
        bool result = orig(self, ref damageInfo);
        if (DisableCooldown.Value)
        {
            self._timer = 0f;
        }
        return result;
    }

    private float On_WyrmOnHitBehavior_SolveForProcChance(
        On.RoR2.Items.WyrmOnHitBehavior.orig_SolveForProcChance orig, WyrmOnHitBehavior self, CharacterMaster master)
    {
        if (DisableChanceDecay.Value)
        {
            return 10f;
        }
        return orig(self, master);
    }

    private int On_CharacterMaster_GetDeployableSameSlotLimit(
        On.RoR2.CharacterMaster.orig_GetDeployableSameSlotLimit orig, CharacterMaster self, DeployableSlot slot)
    {
        if (slot == DeployableSlot.WyrmOnHit && DisableWormLimit.Value)
        {
            return int.MaxValue;
        }
        return orig(self, slot);
    }
}
