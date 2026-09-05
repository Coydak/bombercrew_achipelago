using System.Collections.Generic;

namespace BC_archipelago.Archipelago;

/// <summary>
/// Defines the categories of Archipelago items this client knows how to handle.
/// The actual item IDs must match the item IDs defined in the Bomber Crew Archipelago world.
/// </summary>
public enum ItemCategory
{
    Unknown,
    Funds,
    Intel,
    BomberUpgrade,
    BomberUpgradeProgressive,
    CrewEquipment,
    CrewSkillXp,
    MissionUnlock,
    InstantRepair,
    InstantHeal,
}

/// <summary>
/// Describes a single Archipelago item effect.
/// </summary>
public readonly struct ItemDefinition
{
    public readonly long Id;
    public readonly string Name;
    public readonly ItemCategory Category;
    public readonly string Payload; // category-specific parameter (upgrade/equipment name, skill name, amount, etc.)

    public ItemDefinition(long id, string name, ItemCategory category, string payload = null)
    {
        Id = id;
        Name = name;
        Category = category;
        Payload = payload;
    }
}

/// <summary>
/// Maps Archipelago item IDs to in-game effects.
///
/// The BomberUpgradeProgressive, BomberUpgrade (livery only), and CrewEquipment entries
/// below were generated from the real bomber requirement slots
/// (BomberRequirements.GetRequirements()) and the real upgrade/gear catalogues
/// (BomberUpgradeCatalogueLoader / CrewmanGearCatalogueLoader), both dumped live from a
/// running game via UnityExplorer. See tools/gen_item_table.py for the generator.
///
/// Non-cosmetic bomber upgrades are progressive: one item per upgrade *line* (parallel
/// variants like Standard/Armoured/Light engines are separate lines), applied fleet-wide
/// to every slot of the matching type - receiving another copy moves that line up one
/// tier (see BomberCrew/ItemRewarder.cs ProgressiveLines for the tier tables). Payload is
/// "{lineId}:{tierCount}". Cosmetic Livery items have no tiering concept and stay as
/// individual per-slot BomberUpgrade items, same as before.
///
/// Not covered here: Funds/Intel/CrewSkillXp/MissionUnlock/InstantRepair/InstantHeal items,
/// which are a separate concern from the upgrade/equipment shops and still need real entries.
/// </summary>
public static class ItemTable
{
    public static readonly Dictionary<long, ItemDefinition> Items = new()
    {
        { 9300000, new ItemDefinition(9300000, "Progressive EngineStandard", ItemCategory.BomberUpgradeProgressive, "EngineStandard:5") },
        { 9300001, new ItemDefinition(9300001, "Progressive EngineArmoured", ItemCategory.BomberUpgradeProgressive, "EngineArmoured:5") },
        { 9300002, new ItemDefinition(9300002, "Progressive EngineLight", ItemCategory.BomberUpgradeProgressive, "EngineLight:3") },
        { 9300003, new ItemDefinition(9300003, "Progressive GunTurret303x2", ItemCategory.BomberUpgradeProgressive, "GunTurret303x2:3") },
        { 9300004, new ItemDefinition(9300004, "Progressive GunTurret303x2AmmoFeed", ItemCategory.BomberUpgradeProgressive, "GunTurret303x2AmmoFeed:3") },
        { 9300005, new ItemDefinition(9300005, "Progressive GunTurret303x4", ItemCategory.BomberUpgradeProgressive, "GunTurret303x4:2") },
        { 9300006, new ItemDefinition(9300006, "Progressive GunTurret303x4AmmoFeed", ItemCategory.BomberUpgradeProgressive, "GunTurret303x4AmmoFeed:1") },
        { 9300007, new ItemDefinition(9300007, "Progressive GunTurret50x4", ItemCategory.BomberUpgradeProgressive, "GunTurret50x4:2") },
        { 9300008, new ItemDefinition(9300008, "Progressive GunTurret50x4AmmoFeed", ItemCategory.BomberUpgradeProgressive, "GunTurret50x4AmmoFeed:1") },
        { 9300009, new ItemDefinition(9300009, "Progressive GunTurret50x2", ItemCategory.BomberUpgradeProgressive, "GunTurret50x2:3") },
        { 9300010, new ItemDefinition(9300010, "Progressive GunTurret50x2AmmoFeed", ItemCategory.BomberUpgradeProgressive, "GunTurret50x2AmmoFeed:3") },
        { 9300011, new ItemDefinition(9300011, "Progressive FuselageLightweight", ItemCategory.BomberUpgradeProgressive, "FuselageLightweight:5") },
        { 9300012, new ItemDefinition(9300012, "Progressive FuselageArmoured", ItemCategory.BomberUpgradeProgressive, "FuselageArmoured:7") },
        { 9300013, new ItemDefinition(9300013, "Progressive Electrical", ItemCategory.BomberUpgradeProgressive, "Electrical:5") },
        { 9300014, new ItemDefinition(9300014, "Progressive Hydraulic", ItemCategory.BomberUpgradeProgressive, "Hydraulic:4") },
        { 9300015, new ItemDefinition(9300015, "Progressive Radar", ItemCategory.BomberUpgradeProgressive, "Radar:6") },
        { 9300016, new ItemDefinition(9300016, "Progressive Extinguisher", ItemCategory.BomberUpgradeProgressive, "Extinguisher:4") },
        { 9300017, new ItemDefinition(9300017, "Progressive EquipmentRack", ItemCategory.BomberUpgradeProgressive, "EquipmentRack:3") },
        { 9300018, new ItemDefinition(9300018, "Progressive OxygenTank", ItemCategory.BomberUpgradeProgressive, "OxygenTank:3") },
        { 9300019, new ItemDefinition(9300019, "Progressive FuelTank", ItemCategory.BomberUpgradeProgressive, "FuelTank:3") },
        { 9300020, new ItemDefinition(9300020, "Progressive FuelTankSelfSealing", ItemCategory.BomberUpgradeProgressive, "FuelTankSelfSealing:1") },
        { 9300021, new ItemDefinition(9300021, "Progressive SurvivalDinghy", ItemCategory.BomberUpgradeProgressive, "SurvivalDinghy:3") },
        { 9300022, new ItemDefinition(9300022, "Progressive SurvivalPigeon", ItemCategory.BomberUpgradeProgressive, "SurvivalPigeon:3") },
        { 9300023, new ItemDefinition(9300023, "Livery_Base_Pumpkin (livery_base_texture)", ItemCategory.BomberUpgrade, "livery_base_texture:Livery_Base_Pumpkin") },
        { 9300024, new ItemDefinition(9300024, "Livery_Base_Festive (livery_base_texture)", ItemCategory.BomberUpgrade, "livery_base_texture:Livery_Base_Festive") },
        { 9300025, new ItemDefinition(9300025, "Livery_Base_GiftWrap (livery_base_texture)", ItemCategory.BomberUpgrade, "livery_base_texture:Livery_Base_GiftWrap") },
        { 9300026, new ItemDefinition(9300026, "Livery_Base_Pigeon (livery_base_texture)", ItemCategory.BomberUpgrade, "livery_base_texture:Livery_Base_Pigeon") },
        { 9300027, new ItemDefinition(9300027, "Livery_Base_SalmonPink (livery_base_texture)", ItemCategory.BomberUpgrade, "livery_base_texture:Livery_Base_SalmonPink") },
        { 9300028, new ItemDefinition(9300028, "Livery_Base_White (livery_base_texture)", ItemCategory.BomberUpgrade, "livery_base_texture:Livery_Base_White") },
        { 9300029, new ItemDefinition(9300029, "Livery_Base_Olive (livery_base_texture)", ItemCategory.BomberUpgrade, "livery_base_texture:Livery_Base_Olive") },
        { 9300030, new ItemDefinition(9300030, "Livery_Base_Yellow (livery_base_texture)", ItemCategory.BomberUpgrade, "livery_base_texture:Livery_Base_Yellow") },
        { 9300031, new ItemDefinition(9300031, "Livery_Base_Red (livery_base_texture)", ItemCategory.BomberUpgrade, "livery_base_texture:Livery_Base_Red") },
        { 9300032, new ItemDefinition(9300032, "Livery_Base_SeaBlue (livery_base_texture)", ItemCategory.BomberUpgrade, "livery_base_texture:Livery_Base_SeaBlue") },
        { 9300033, new ItemDefinition(9300033, "Livery_Base_IdentityCrisis (livery_base_texture)", ItemCategory.BomberUpgrade, "livery_base_texture:Livery_Base_IdentityCrisis") },
        { 9300034, new ItemDefinition(9300034, "Livery_Base_CurveDigital (livery_base_texture)", ItemCategory.BomberUpgrade, "livery_base_texture:Livery_Base_CurveDigital") },
        { 9300035, new ItemDefinition(9300035, "Livery_NoseArt_CurveDigital (livery_nose_art)", ItemCategory.BomberUpgrade, "livery_nose_art:Livery_NoseArt_CurveDigital") },
        { 9300036, new ItemDefinition(9300036, "Livery_NoseArt_RunnerDuck (livery_nose_art)", ItemCategory.BomberUpgrade, "livery_nose_art:Livery_NoseArt_RunnerDuck") },
        { 9300037, new ItemDefinition(9300037, "Livery_NoseArt_Skull (livery_nose_art)", ItemCategory.BomberUpgrade, "livery_nose_art:Livery_NoseArt_Skull") },
        { 9300038, new ItemDefinition(9300038, "Livery_NoseArt_8ball (livery_nose_art)", ItemCategory.BomberUpgrade, "livery_nose_art:Livery_NoseArt_8ball") },
        { 9300039, new ItemDefinition(9300039, "Livery_NoseArt_FlyingPig (livery_nose_art)", ItemCategory.BomberUpgrade, "livery_nose_art:Livery_NoseArt_FlyingPig") },
        { 9300040, new ItemDefinition(9300040, "Livery_NoseArt_TopHat (livery_nose_art)", ItemCategory.BomberUpgrade, "livery_nose_art:Livery_NoseArt_TopHat") },
        { 9300041, new ItemDefinition(9300041, "Livery_NoseArt_CatsEyes01 (livery_nose_art)", ItemCategory.BomberUpgrade, "livery_nose_art:Livery_NoseArt_CatsEyes01") },
        { 9300042, new ItemDefinition(9300042, "Livery_NoseArt_VampireBat (livery_nose_art)", ItemCategory.BomberUpgrade, "livery_nose_art:Livery_NoseArt_VampireBat") },
        { 9300043, new ItemDefinition(9300043, "Livery_NoseArt_Pumpkin (livery_nose_art)", ItemCategory.BomberUpgrade, "livery_nose_art:Livery_NoseArt_Pumpkin") },
        { 9300044, new ItemDefinition(9300044, "Livery_NoseArt_Custom4 (livery_nose_art)", ItemCategory.BomberUpgrade, "livery_nose_art:Livery_NoseArt_Custom4") },
        { 9300045, new ItemDefinition(9300045, "Livery_NoseArt_Custom1 (livery_nose_art)", ItemCategory.BomberUpgrade, "livery_nose_art:Livery_NoseArt_Custom1") },
        { 9300046, new ItemDefinition(9300046, "Livery_NoseArt_Custom2 (livery_nose_art)", ItemCategory.BomberUpgrade, "livery_nose_art:Livery_NoseArt_Custom2") },
        { 9300047, new ItemDefinition(9300047, "Livery_NoseArt_Custom3 (livery_nose_art)", ItemCategory.BomberUpgrade, "livery_nose_art:Livery_NoseArt_Custom3") },
        { 9300048, new ItemDefinition(9300048, "Livery_NoseArt_Custom0 (livery_nose_art)", ItemCategory.BomberUpgrade, "livery_nose_art:Livery_NoseArt_Custom0") },
        { 9300049, new ItemDefinition(9300049, "Livery_NoseArt_ProfilePic (livery_nose_art)", ItemCategory.BomberUpgrade, "livery_nose_art:Livery_NoseArt_ProfilePic") },
        { 9300050, new ItemDefinition(9300050, "Livery_NoseArt_ProfilePicRound (livery_nose_art)", ItemCategory.BomberUpgrade, "livery_nose_art:Livery_NoseArt_ProfilePicRound") },
        { 9300051, new ItemDefinition(9300051, "Livery_NoseArt_CompetitionArt01 (livery_nose_art)", ItemCategory.BomberUpgrade, "livery_nose_art:Livery_NoseArt_CompetitionArt01") },
        { 9300052, new ItemDefinition(9300052, "Livery_NoseArt_CompetitionArt02 (livery_nose_art)", ItemCategory.BomberUpgrade, "livery_nose_art:Livery_NoseArt_CompetitionArt02") },
        { 9300053, new ItemDefinition(9300053, "Livery_NoseArt_CompetitionArt03 (livery_nose_art)", ItemCategory.BomberUpgrade, "livery_nose_art:Livery_NoseArt_CompetitionArt03") },
        { 9300054, new ItemDefinition(9300054, "Livery_EngineArt_Teeth (livery_engine_art)", ItemCategory.BomberUpgrade, "livery_engine_art:Livery_EngineArt_Teeth") },
        { 9300055, new ItemDefinition(9300055, "Livery_EngineArt_Flames (livery_engine_art)", ItemCategory.BomberUpgrade, "livery_engine_art:Livery_EngineArt_Flames") },
        { 9300056, new ItemDefinition(9300056, "Livery_EngineArt_Lightning (livery_engine_art)", ItemCategory.BomberUpgrade, "livery_engine_art:Livery_EngineArt_Lightning") },
        { 9300057, new ItemDefinition(9300057, "Livery_EngineArt_AceOfSpades (livery_engine_art)", ItemCategory.BomberUpgrade, "livery_engine_art:Livery_EngineArt_AceOfSpades") },
        { 9300058, new ItemDefinition(9300058, "Livery_EngineArt_Skull (livery_engine_art)", ItemCategory.BomberUpgrade, "livery_engine_art:Livery_EngineArt_Skull") },
        { 9300059, new ItemDefinition(9300059, "Livery_EngineArt_Pumpkin (livery_engine_art)", ItemCategory.BomberUpgrade, "livery_engine_art:Livery_EngineArt_Pumpkin") },
        { 9300060, new ItemDefinition(9300060, "Livery_EngineArt_Custom2 (livery_engine_art)", ItemCategory.BomberUpgrade, "livery_engine_art:Livery_EngineArt_Custom2") },
        { 9300061, new ItemDefinition(9300061, "Livery_EngineArt_Custom4 (livery_engine_art)", ItemCategory.BomberUpgrade, "livery_engine_art:Livery_EngineArt_Custom4") },
        { 9300062, new ItemDefinition(9300062, "Livery_EngineArt_Custom3 (livery_engine_art)", ItemCategory.BomberUpgrade, "livery_engine_art:Livery_EngineArt_Custom3") },
        { 9300063, new ItemDefinition(9300063, "Livery_EngineArt_Custom1 (livery_engine_art)", ItemCategory.BomberUpgrade, "livery_engine_art:Livery_EngineArt_Custom1") },
        { 9300064, new ItemDefinition(9300064, "Livery_EngineArt_Custom0 (livery_engine_art)", ItemCategory.BomberUpgrade, "livery_engine_art:Livery_EngineArt_Custom0") },
        { 9300065, new ItemDefinition(9300065, "Livery_EngineArt_ProfilePic (livery_engine_art)", ItemCategory.BomberUpgrade, "livery_engine_art:Livery_EngineArt_ProfilePic") },
        { 9300066, new ItemDefinition(9300066, "Livery_EngineArt_ProfilePicRound (livery_engine_art)", ItemCategory.BomberUpgrade, "livery_engine_art:Livery_EngineArt_ProfilePicRound") },
        { 9300067, new ItemDefinition(9300067, "Livery_WingArt_Round01 (livery_wing_art)", ItemCategory.BomberUpgrade, "livery_wing_art:Livery_WingArt_Round01") },
        { 9300068, new ItemDefinition(9300068, "Livery_WingArt_Star01 (livery_wing_art)", ItemCategory.BomberUpgrade, "livery_wing_art:Livery_WingArt_Star01") },
        { 9300069, new ItemDefinition(9300069, "Livery_WingArt_AceOfSpades (livery_wing_art)", ItemCategory.BomberUpgrade, "livery_wing_art:Livery_WingArt_AceOfSpades") },
        { 9300070, new ItemDefinition(9300070, "Livery_WingArt_8ball (livery_wing_art)", ItemCategory.BomberUpgrade, "livery_wing_art:Livery_WingArt_8ball") },
        { 9300071, new ItemDefinition(9300071, "Livery_WingArt_Pumpkin (livery_wing_art)", ItemCategory.BomberUpgrade, "livery_wing_art:Livery_WingArt_Pumpkin") },
        { 9300072, new ItemDefinition(9300072, "Livery_WingArt_Custom0 (livery_wing_art)", ItemCategory.BomberUpgrade, "livery_wing_art:Livery_WingArt_Custom0") },
        { 9300073, new ItemDefinition(9300073, "Livery_WingArt_Custom1 (livery_wing_art)", ItemCategory.BomberUpgrade, "livery_wing_art:Livery_WingArt_Custom1") },
        { 9300074, new ItemDefinition(9300074, "Livery_WingArt_Custom2 (livery_wing_art)", ItemCategory.BomberUpgrade, "livery_wing_art:Livery_WingArt_Custom2") },
        { 9300075, new ItemDefinition(9300075, "Livery_WingArt_Custom3 (livery_wing_art)", ItemCategory.BomberUpgrade, "livery_wing_art:Livery_WingArt_Custom3") },
        { 9300076, new ItemDefinition(9300076, "Livery_WingArt_Custom4 (livery_wing_art)", ItemCategory.BomberUpgrade, "livery_wing_art:Livery_WingArt_Custom4") },
        { 9300077, new ItemDefinition(9300077, "Livery_WingArt_ProfilePic (livery_wing_art)", ItemCategory.BomberUpgrade, "livery_wing_art:Livery_WingArt_ProfilePic") },
        { 9300078, new ItemDefinition(9300078, "Livery_WingArt_ProfilePicRound (livery_wing_art)", ItemCategory.BomberUpgrade, "livery_wing_art:Livery_WingArt_ProfilePicRound") },
        { 9300079, new ItemDefinition(9300079, "Livery_WingArt_CompetitionArt01 (livery_wing_art)", ItemCategory.BomberUpgrade, "livery_wing_art:Livery_WingArt_CompetitionArt01") },
        { 9300080, new ItemDefinition(9300080, "Livery_WingArt_CompetitionArt02 (livery_wing_art)", ItemCategory.BomberUpgrade, "livery_wing_art:Livery_WingArt_CompetitionArt02") },
        { 9300081, new ItemDefinition(9300081, "Livery_WingArt_CompetitionArt03 (livery_wing_art)", ItemCategory.BomberUpgrade, "livery_wing_art:Livery_WingArt_CompetitionArt03") },
        { 9300082, new ItemDefinition(9300082, "Livery_MainText_White (livery_main_text)", ItemCategory.BomberUpgrade, "livery_main_text:Livery_MainText_White") },
        { 9300083, new ItemDefinition(9300083, "Livery_MainText_Black (livery_main_text)", ItemCategory.BomberUpgrade, "livery_main_text:Livery_MainText_Black") },
        { 9300084, new ItemDefinition(9300084, "Livery_MainText_Red (livery_main_text)", ItemCategory.BomberUpgrade, "livery_main_text:Livery_MainText_Red") },
        { 9300085, new ItemDefinition(9300085, "Livery_MainText_Yellow (livery_main_text)", ItemCategory.BomberUpgrade, "livery_main_text:Livery_MainText_Yellow") },
        { 9300086, new ItemDefinition(9300086, "HeadgearHatWinter01 (crew Headgear)", ItemCategory.CrewEquipment, "Headgear:HeadgearHatWinter01") },
        { 9300087, new ItemDefinition(9300087, "HeadgearHatWinter02 (crew Headgear)", ItemCategory.CrewEquipment, "Headgear:HeadgearHatWinter02") },
        { 9300088, new ItemDefinition(9300088, "HeadgearHatWinter03 (crew Headgear)", ItemCategory.CrewEquipment, "Headgear:HeadgearHatWinter03") },
        { 9300089, new ItemDefinition(9300089, "HeadgearCapService (crew Headgear)", ItemCategory.CrewEquipment, "Headgear:HeadgearCapService") },
        { 9300090, new ItemDefinition(9300090, "HeadgearBeretBlue (crew Headgear)", ItemCategory.CrewEquipment, "Headgear:HeadgearBeretBlue") },
        { 9300091, new ItemDefinition(9300091, "HeadgearHeadphones (crew Headgear)", ItemCategory.CrewEquipment, "Headgear:HeadgearHeadphones") },
        { 9300092, new ItemDefinition(9300092, "HeadgearCapPilot (crew Headgear)", ItemCategory.CrewEquipment, "Headgear:HeadgearCapPilot") },
        { 9300093, new ItemDefinition(9300093, "HeadgearCapPilotGoggles (crew Headgear)", ItemCategory.CrewEquipment, "Headgear:HeadgearCapPilotGoggles") },
        { 9300094, new ItemDefinition(9300094, "HeadgearCapPilotGogglesPhones (crew Headgear)", ItemCategory.CrewEquipment, "Headgear:HeadgearCapPilotGogglesPhones") },
        { 9300095, new ItemDefinition(9300095, "HeadgearHelmetLeatherFlying (crew Headgear)", ItemCategory.CrewEquipment, "Headgear:HeadgearHelmetLeatherFlying") },
        { 9300096, new ItemDefinition(9300096, "HeadgearHelmetLeatherFlyingGoggles (crew Headgear)", ItemCategory.CrewEquipment, "Headgear:HeadgearHelmetLeatherFlyingGoggles") },
        { 9300097, new ItemDefinition(9300097, "HeadgearHelmetMk1 (crew Headgear)", ItemCategory.CrewEquipment, "Headgear:HeadgearHelmetMk1") },
        { 9300098, new ItemDefinition(9300098, "HeadgearHelmetMk2 (crew Headgear)", ItemCategory.CrewEquipment, "Headgear:HeadgearHelmetMk2") },
        { 9300099, new ItemDefinition(9300099, "HeadgearHelmetFlakMk1Goggles (crew Headgear)", ItemCategory.CrewEquipment, "Headgear:HeadgearHelmetFlakMk1Goggles") },
        { 9300100, new ItemDefinition(9300100, "HeadgearHelmetFlakMk2Goggles (crew Headgear)", ItemCategory.CrewEquipment, "Headgear:HeadgearHelmetFlakMk2Goggles") },
        { 9300101, new ItemDefinition(9300101, "OxygenBottleSmall (crew Oxygen)", ItemCategory.CrewEquipment, "Oxygen:OxygenBottleSmall") },
        { 9300102, new ItemDefinition(9300102, "OxygenBottleMedium (crew Oxygen)", ItemCategory.CrewEquipment, "Oxygen:OxygenBottleMedium") },
        { 9300103, new ItemDefinition(9300103, "OxygenBottleLarge (crew Oxygen)", ItemCategory.CrewEquipment, "Oxygen:OxygenBottleLarge") },
        { 9300104, new ItemDefinition(9300104, "OxygenBottleSmallToughened (crew Oxygen)", ItemCategory.CrewEquipment, "Oxygen:OxygenBottleSmallToughened") },
        { 9300105, new ItemDefinition(9300105, "OxygenBottleMediumToughened (crew Oxygen)", ItemCategory.CrewEquipment, "Oxygen:OxygenBottleMediumToughened") },
        { 9300106, new ItemDefinition(9300106, "OxygenBottleLargeToughened (crew Oxygen)", ItemCategory.CrewEquipment, "Oxygen:OxygenBottleLargeToughened") },
        { 9300107, new ItemDefinition(9300107, "OxygenBottleAdvanced (crew Oxygen)", ItemCategory.CrewEquipment, "Oxygen:OxygenBottleAdvanced") },
        { 9300108, new ItemDefinition(9300108, "VestSurvivalSeaMk1 (crew Vest)", ItemCategory.CrewEquipment, "Vest:VestSurvivalSeaMk1") },
        { 9300109, new ItemDefinition(9300109, "VestSurvivalLandMk1 (crew Vest)", ItemCategory.CrewEquipment, "Vest:VestSurvivalLandMk1") },
        { 9300110, new ItemDefinition(9300110, "VestSurvivalSeaAndLand (crew Vest)", ItemCategory.CrewEquipment, "Vest:VestSurvivalSeaAndLand") },
        { 9300111, new ItemDefinition(9300111, "VestSurvivalSeaAndLandToughened (crew Vest)", ItemCategory.CrewEquipment, "Vest:VestSurvivalSeaAndLandToughened") },
        { 9300112, new ItemDefinition(9300112, "VestFlakLight (crew Vest)", ItemCategory.CrewEquipment, "Vest:VestFlakLight") },
        { 9300113, new ItemDefinition(9300113, "VestFlakHeavyMk1 (crew Vest)", ItemCategory.CrewEquipment, "Vest:VestFlakHeavyMk1") },
        { 9300114, new ItemDefinition(9300114, "GlovesWoolen (crew Gloves)", ItemCategory.CrewEquipment, "Gloves:GlovesWoolen") },
        { 9300115, new ItemDefinition(9300115, "GlovesWoolenFingerless (crew Gloves)", ItemCategory.CrewEquipment, "Gloves:GlovesWoolenFingerless") },
        { 9300116, new ItemDefinition(9300116, "GlovesLeather (crew Gloves)", ItemCategory.CrewEquipment, "Gloves:GlovesLeather") },
        { 9300117, new ItemDefinition(9300117, "GlovesLeatherToughened (crew Gloves)", ItemCategory.CrewEquipment, "Gloves:GlovesLeatherToughened") },
        { 9300118, new ItemDefinition(9300118, "GlovesMittensThermal (crew Gloves)", ItemCategory.CrewEquipment, "Gloves:GlovesMittensThermal") },
        { 9300119, new ItemDefinition(9300119, "GlovesMittensElectricallyHeated (crew Gloves)", ItemCategory.CrewEquipment, "Gloves:GlovesMittensElectricallyHeated") },
        { 9300120, new ItemDefinition(9300120, "BootsPlimsolls (crew Boots)", ItemCategory.CrewEquipment, "Boots:BootsPlimsolls") },
        { 9300121, new ItemDefinition(9300121, "BootsLeather (crew Boots)", ItemCategory.CrewEquipment, "Boots:BootsLeather") },
        { 9300122, new ItemDefinition(9300122, "BootsLeatherToughened (crew Boots)", ItemCategory.CrewEquipment, "Boots:BootsLeatherToughened") },
        { 9300123, new ItemDefinition(9300123, "BootsThermal (crew Boots)", ItemCategory.CrewEquipment, "Boots:BootsThermal") },
        { 9300124, new ItemDefinition(9300124, "BootsElectricallyHeated (crew Boots)", ItemCategory.CrewEquipment, "Boots:BootsElectricallyHeated") },
        { 9300125, new ItemDefinition(9300125, "FlightsuitBasicOlive (crew Flightsuit)", ItemCategory.CrewEquipment, "Flightsuit:FlightsuitBasicOlive") },
        { 9300126, new ItemDefinition(9300126, "FlightsuitBasicKhaki (crew Flightsuit)", ItemCategory.CrewEquipment, "Flightsuit:FlightsuitBasicKhaki") },
        { 9300127, new ItemDefinition(9300127, "FlightsuitBasicRoyalBlue (crew Flightsuit)", ItemCategory.CrewEquipment, "Flightsuit:FlightsuitBasicRoyalBlue") },
        { 9300128, new ItemDefinition(9300128, "FlightsuitBasicCamoWoodland (crew Flightsuit)", ItemCategory.CrewEquipment, "Flightsuit:FlightsuitBasicCamoWoodland") },
        { 9300129, new ItemDefinition(9300129, "FlightsuitBasicBlack (crew Flightsuit)", ItemCategory.CrewEquipment, "Flightsuit:FlightsuitBasicBlack") },
        { 9300130, new ItemDefinition(9300130, "VestSurvivalSeaMk2 (crew Vest)", ItemCategory.CrewEquipment, "Vest:VestSurvivalSeaMk2") },
        { 9300131, new ItemDefinition(9300131, "VestFlakMedium (crew Vest)", ItemCategory.CrewEquipment, "Vest:VestFlakMedium") },
        { 9300132, new ItemDefinition(9300132, "VestSurvivalLandMk3 (crew Vest)", ItemCategory.CrewEquipment, "Vest:VestSurvivalLandMk3") },
        { 9300133, new ItemDefinition(9300133, "VestSurvivalLandMk2 (crew Vest)", ItemCategory.CrewEquipment, "Vest:VestSurvivalLandMk2") },
        { 9300134, new ItemDefinition(9300134, "VestSurvivalSeaMk3 (crew Vest)", ItemCategory.CrewEquipment, "Vest:VestSurvivalSeaMk3") },
        { 9300135, new ItemDefinition(9300135, "VestFlakHeavyMk2 (crew Vest)", ItemCategory.CrewEquipment, "Vest:VestFlakHeavyMk2") },
        { 9300136, new ItemDefinition(9300136, "VestFlakHeavyMk3 (crew Vest)", ItemCategory.CrewEquipment, "Vest:VestFlakHeavyMk3") },
        { 9300137, new ItemDefinition(9300137, "FlightsuitWinterJumper01 (crew Flightsuit)", ItemCategory.CrewEquipment, "Flightsuit:FlightsuitWinterJumper01") },
        { 9300138, new ItemDefinition(9300138, "FlightsuitWinterJumper02 (crew Flightsuit)", ItemCategory.CrewEquipment, "Flightsuit:FlightsuitWinterJumper02") },
        { 9300139, new ItemDefinition(9300139, "FlightsuitWinterJumper03 (crew Flightsuit)", ItemCategory.CrewEquipment, "Flightsuit:FlightsuitWinterJumper03") },
        // Funds drops.
        { 9310000, new ItemDefinition(9310000, "Small Funds Drop", ItemCategory.Funds, "500") },
        { 9310001, new ItemDefinition(9310001, "Medium Funds Drop", ItemCategory.Funds, "1500") },
        { 9310002, new ItemDefinition(9310002, "Large Funds Drop", ItemCategory.Funds, "5000") },

        // Intel drops.
        { 9310010, new ItemDefinition(9310010, "Small Intel Drop", ItemCategory.Intel, "50") },
        { 9310011, new ItemDefinition(9310011, "Medium Intel Drop", ItemCategory.Intel, "150") },
        { 9310012, new ItemDefinition(9310012, "Large Intel Drop", ItemCategory.Intel, "500") },

        // Crew skill XP, one item per Crewman.SpecialisationSkill.
        { 9310020, new ItemDefinition(9310020, "Piloting Training", ItemCategory.CrewSkillXp, "Piloting:250") },
        { 9310021, new ItemDefinition(9310021, "Gunning Training", ItemCategory.CrewSkillXp, "Gunning:250") },
        { 9310022, new ItemDefinition(9310022, "Navigator Training", ItemCategory.CrewSkillXp, "Navigator:250") },
        { 9310023, new ItemDefinition(9310023, "Radio Operator Training", ItemCategory.CrewSkillXp, "RadioOp:250") },
        { 9310024, new ItemDefinition(9310024, "Engineer Training", ItemCategory.CrewSkillXp, "Engineer:250") },
        { 9310025, new ItemDefinition(9310025, "Bomb Aiming Training", ItemCategory.CrewSkillXp, "BombAiming:250") },
        { 9310026, new ItemDefinition(9310026, "First Aid Training", ItemCategory.CrewSkillXp, "FirstAid:250") },
        { 9310027, new ItemDefinition(9310027, "Fire Fighting Training", ItemCategory.CrewSkillXp, "FireFighting:250") },
        { 9310028, new ItemDefinition(9310028, "All-Round Training", ItemCategory.CrewSkillXp, "150") },

        // Mission unlocks: the training gate and every chapter's key mission (see LocationTable).
        { 9310030, new ItemDefinition(9310030, "Bomb Run Training Clearance", ItemCategory.MissionUnlock, "BombRunTraining") },
        { 9310031, new ItemDefinition(9310031, "Chapter 1 Clearance", ItemCategory.MissionUnlock, "C01_KEY") },
        { 9310032, new ItemDefinition(9310032, "Chapter 2 Clearance", ItemCategory.MissionUnlock, "C02_KEY") },
        { 9310033, new ItemDefinition(9310033, "Chapter 3 Clearance", ItemCategory.MissionUnlock, "C03_KEY") },
        { 9310034, new ItemDefinition(9310034, "Chapter 4 Clearance", ItemCategory.MissionUnlock, "C04_KEY") },
        { 9310035, new ItemDefinition(9310035, "Chapter 5 Clearance", ItemCategory.MissionUnlock, "C05_KEY") },
        { 9310036, new ItemDefinition(9310036, "Chapter 6 Clearance", ItemCategory.MissionUnlock, "C06_KEY") },
        { 9310037, new ItemDefinition(9310037, "Chapter 7 Clearance", ItemCategory.MissionUnlock, "C07_KEY") },
        { 9310038, new ItemDefinition(9310038, "Final Mission Clearance", ItemCategory.MissionUnlock, "C08_KEY") },
        { 9310039, new ItemDefinition(9310039, "DLC1 Chapter Clearance", ItemCategory.MissionUnlock, "DLCMP01_C01_KEY") },

        // Mid-mission repair and crew heal/resurrect. No payload needed.
        { 9310050, new ItemDefinition(9310050, "Instant Repair", ItemCategory.InstantRepair) },
        { 9310051, new ItemDefinition(9310051, "Instant Heal", ItemCategory.InstantHeal) },
    };

    public static bool TryGetDefinition(long id, out ItemDefinition definition)
    {
        return Items.TryGetValue(id, out definition);
    }
}
