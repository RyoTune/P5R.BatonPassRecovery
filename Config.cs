using P5R.BatonPassRecovery.Template.Configuration;
using System.ComponentModel;
using Reloaded.Mod.Interfaces.Structs;

namespace P5R.BatonPassRecovery.Configuration;

public class Config : Configurable<Config>
{
    [DisplayName("Log Level")]
    [DefaultValue(LogLevel.Information)]
    public LogLevel LogLevel { get; set; } = LogLevel.Information;
    
    [Category("Baton Pass HP Recovery %")]
    [DisplayName("Rank 1")]
    [DefaultValue(0)]
    [SliderControlParams(
        minimum: 0.0,
        maximum: 100.0,
        smallChange: 1.0,
        largeChange: 10.0,
        tickFrequency: 10,
        isSnapToTickEnabled: false,
        tickPlacement: SliderControlTickPlacement.BottomRight,
        showTextField: true,
        isTextFieldEditable: false)]
    public int HP_Level_1 { get; set; } = 0;

    [Category("Baton Pass HP Recovery %")]
    [DisplayName("Rank 2")]
    [DefaultValue(10)]
    [SliderControlParams(
        minimum: 0.0,
        maximum: 100.0,
        smallChange: 1.0,
        largeChange: 10.0,
        tickFrequency: 10,
        isSnapToTickEnabled: false,
        tickPlacement: SliderControlTickPlacement.BottomRight,
        showTextField: true,
        isTextFieldEditable: false)]
    public int HP_Level_2 { get; set; } = 10;

    [Category("Baton Pass HP Recovery %")]
    [DisplayName("Rank 3")]
    [DefaultValue(15)]
    [SliderControlParams(
        minimum: 0.0,
        maximum: 100.0,
        smallChange: 1.0,
        largeChange: 10.0,
        tickFrequency: 10,
        isSnapToTickEnabled: false,
        tickPlacement: SliderControlTickPlacement.BottomRight,
        showTextField: true,
        isTextFieldEditable: false)]
    public int HP_Level_3 { get; set; } = 15;

    [Category("Baton Pass SP Recovery")]
    [DisplayName("Turn 1")]
    [DefaultValue(2)]
    public int SP_Level_1 { get; set; } = 2;

    [Category("Baton Pass SP Recovery")]
    [DisplayName("Turn 2")]
    [DefaultValue(4)]
    public int SP_Level_2 { get; set; } = 4;

    [Category("Baton Pass SP Recovery")]
    [DisplayName("Turn 3")]
    [DefaultValue(6)]
    public int SP_Level_3 { get; set; } = 6;
}

/// <summary>
/// Allows you to override certain aspects of the configuration creation process (e.g. create multiple configurations).
/// Override elements in <see cref="ConfiguratorMixinBase"/> for finer control.
/// </summary>
public class ConfiguratorMixin : ConfiguratorMixinBase
{
}