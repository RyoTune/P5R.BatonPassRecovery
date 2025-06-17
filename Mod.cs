#if DEBUG
using System.Diagnostics;
#endif
using System.Runtime.InteropServices;
using System.Text.Json;
using Reloaded.Mod.Interfaces;
using P5R.BatonPassRecovery.Template;
using P5R.BatonPassRecovery.Configuration;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.Enums;
using Reloaded.Hooks.Definitions.X64;
using Reloaded.Mod.Interfaces.Internal;
using static Reloaded.Hooks.Definitions.X64.FunctionAttribute;
using IReloadedHooks = Reloaded.Hooks.ReloadedII.Interfaces.IReloadedHooks;

namespace P5R.BatonPassRecovery;

public unsafe class Mod : ModBase
{
    private readonly IModLoader _modLoader;
    private readonly IReloadedHooks? _hooks;
    private readonly ILogger _log;
    private readonly IMod _owner;

    private Config _config;
    private readonly IModConfig _modConfig;
    
    [Function(Register.rax, Register.rax, true)]
    private delegate ushort GetBatonSpAdd(int batonPassLevel);
    private IReverseWrapper<GetBatonSpAdd>? _batonSpWrapper;
    private IAsmHook? _batonSpHook;

    [Function(Register.rax, Register.rax, true)]
    private delegate ushort GetBatonHpAdd(int batonPassLevel);
    private IReverseWrapper<GetBatonSpAdd>? _batonHpWrapper;
    private IAsmHook? _batonHpHook;

    private IAsmHook? _removeHpBatonCheckHook;
    private IAsmHook? _removeHpCheckHook;
    private IAsmHook? _hpMathCheck;
    private IAsmHook? _hpRecoverMath;

    private int* _batonHpRecovery1;
    private int* _batonHpRecovery2;
    private int* _batonHpRecovery3;

    private bool _configLoaded;

    public Mod(ModContext context)
    {
        _modLoader = context.ModLoader;
        _hooks = context.Hooks;
        _log = context.Logger;
        _owner = context.Owner;
        _config = context.Configuration;
        _modConfig = context.ModConfig;
#if DEBUG
        Debugger.Launch();
#endif
        Project.Initialize(_modConfig, _modLoader, _log, true);
        Log.LogLevel = _config.LogLevel;
        
        ScanHooks.Add("Baton Pass SP Hook", "44 8B C8 85 C0 74", (hooks, result) =>
        {
            
            var patch = new string[]
            {
                "use64",
                Utilities.PushCallerRegisters,
                hooks.Utilities.GetAbsoluteCallMnemonics(GetBatonPassSpAddImpl, out _batonSpWrapper),
                Utilities.PopCallerRegisters,
                "add r15d, eax",
                hooks.Utilities.GetAbsoluteJumpMnemonics(result + 0xD4, true)
            };

            _batonSpHook = hooks.CreateAsmHook(patch, result).Activate();
        });

        _batonHpRecovery1 = (int*)Marshal.AllocHGlobal(sizeof(int));
        _batonHpRecovery2 = (int*)Marshal.AllocHGlobal(sizeof(int));
        _batonHpRecovery3 = (int*)Marshal.AllocHGlobal(sizeof(int));
        ApplyHpSettings();

        ScanHooks.Add("Baton Pass HP Hooks", "0F 84 ?? ?? ?? ?? E8 ?? ?? ?? ?? 48 8B 4D ?? 48 8B 49", (hooks, result) =>
        {
            var removeHpBatonCheckAddress = result - 0x2E;
            string[] removeHpBatonCheckPatch =
            [
                "use64",
                "mov r15d, ebx"
            ];

            _removeHpBatonCheckHook = hooks.CreateAsmHook(removeHpBatonCheckPatch, removeHpBatonCheckAddress, AsmHookBehaviour.DoNotExecuteOriginal).Activate();

            var removeHpBatonCheck = new string[] { "use64", "test rax, rax" };
            _removeHpCheckHook = hooks.CreateAsmHook(removeHpBatonCheck, result).Activate();

            var hpMathCheckAddress = result + 0x18;
            var hpMathCheckPatch = new string[]
            {
                "use64",
                $"mov rcx, {(nint)_batonHpRecovery1}",
                "cmp r15d, 0",
                "je getRecoverValue",
                "cmp r15d, 1",
                $"mov rcx, {(nint)_batonHpRecovery2}",
                "je getRecoverValue",
                $"mov rcx, {(nint)_batonHpRecovery3}",
                "getRecoverValue:",
                "mov ecx, [rcx]",
                "imul ecx, eax",
                "mov eax, 0x51eb851f"
            };

            _hpMathCheck = hooks.CreateAsmHook(hpMathCheckPatch, hpMathCheckAddress, AsmHookBehaviour.DoNotExecuteOriginal).Activate();

            var hpRecoverMathAddress = result + 0x6C;
            var hpMRecoverMathPatch = new string[]
            {
                "use64",
                $"mov rcx, {(nint)_batonHpRecovery1}",
                "cmp r15d, 0",
                "je getRecoverValue",
                "cmp r15d, 1",
                $"mov rcx, {(nint)_batonHpRecovery2}",
                "je getRecoverValue",
                $"mov rcx, {(nint)_batonHpRecovery3}",
                "getRecoverValue:",
                "mov ecx, [rcx]",
                "imul ecx, eax",
                "mov esi, 0x8",
            };

            _hpRecoverMath = hooks.CreateAsmHook(hpMRecoverMathPatch, hpRecoverMathAddress, AsmHookBehaviour.DoNotExecuteOriginal).Activate();
        });

        _modLoader.ModLoaded += OnModLoaded;
    }

    private void OnModLoaded(IModV1 mod, IModConfigV1 modConfig)
    {
        var modDir = _modLoader.GetDirectoryForModId(modConfig.ModId);
        var batonConfigFile = Path.Join(modDir, "baton-pass.json");
        if (!_configLoaded && File.Exists(batonConfigFile))
        {
            try
            {
                var config = JsonSerializer.Deserialize<Config>(File.ReadAllText(batonConfigFile));
                if (config == null)
                {
                    return;
                }

                _config.HP_Level_1 = _config.HP_Level_1;
                _config.HP_Level_2 = _config.HP_Level_2;
                _config.HP_Level_3 = _config.HP_Level_3;
                _config.SP_Level_1 = _config.SP_Level_1;
                _config.SP_Level_2 = _config.SP_Level_2;
                _config.SP_Level_3 = _config.SP_Level_3;
                ApplyHpSettings();

                _configLoaded = true;
                Log.Information($"Using mod baton config: {modConfig.ModName}");
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Failed to load baton config from: {modConfig.ModName}");
            }
        }
    }

    private ushort GetBatonPassSpAddImpl(int batonPassLevel)
    {
        ushort spIncrease = 0;
        switch (batonPassLevel)
        {
            case 0:
                spIncrease = (ushort)_config.SP_Level_1;
                break;
            case 1:
                spIncrease = (ushort)_config.SP_Level_2;
                break;
            case 2:
                spIncrease = (ushort)_config.SP_Level_3;
                break;
        }

        Log.Debug($"Baton Pass LV: {batonPassLevel + 1}");
        Log.Debug($"SP: +{spIncrease}");
        return spIncrease;
    }

    private void ApplyHpSettings()
    {
        *_batonHpRecovery1 = _config.HP_Level_1;
        *_batonHpRecovery2 = _config.HP_Level_2;
        *_batonHpRecovery3 = _config.HP_Level_3;
    }

    #region Standard Overrides

    public override void ConfigurationUpdated(Config configuration)
    {
        // Apply settings from configuration.
        // ... your code here.
        _config = configuration;
        _log.WriteLine($"[{_modConfig.ModId}] Config Updated: Applying");
        Log.LogLevel = _config.LogLevel;
        ApplyHpSettings();
    }

    #endregion

    #region For Exports, Serialization etc.

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public Mod()
    {
    }
#pragma warning restore CS8618

    #endregion
}