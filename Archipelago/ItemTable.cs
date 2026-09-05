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
/// The BomberUpgradeProgressive and CrewEquipment entries below were generated from the real
/// bomber requirement slots (BomberRequirements.GetRequirements()) and the real upgrade/gear
/// catalogues (BomberUpgradeCatalogueLoader / CrewmanGearCatalogueLoader), both dumped live
/// from a running game via UnityExplorer. See tools/gen_item_table.py for the generator.
///
/// Non-cosmetic bomber upgrades are progressive: one item per upgrade *line* (parallel
/// variants like Standard/Armoured/Light engines are separate lines), applied fleet-wide
/// to every slot of the matching type - receiving another copy moves that line up one
/// tier (see BomberCrew/ItemRewarder.cs ProgressiveLines for the tier tables). Payload is
/// "{lineId}:{tierCount}".
///
/// Cosmetic Livery skins are intentionally excluded entirely (no items, no locations) - no
/// gameplay effect either way. The plain BomberUpgrade category (direct "slotId:upgradeName"
/// install, see ItemRewarder.ApplyBomberUpgrade) has no entries here right now but is kept
/// around in case a future non-progressive bomber upgrade item is ever wanted again.
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
        { 9300023, new ItemDefinition(9300023, "HeadgearHatWinter01 (crew Headgear)", ItemCategory.CrewEquipment, "Headgear:HeadgearHatWinter01") },
        { 9300024, new ItemDefinition(9300024, "HeadgearHatWinter02 (crew Headgear)", ItemCategory.CrewEquipment, "Headgear:HeadgearHatWinter02") },
        { 9300025, new ItemDefinition(9300025, "HeadgearHatWinter03 (crew Headgear)", ItemCategory.CrewEquipment, "Headgear:HeadgearHatWinter03") },
        { 9300026, new ItemDefinition(9300026, "HeadgearCapService (crew Headgear)", ItemCategory.CrewEquipment, "Headgear:HeadgearCapService") },
        { 9300027, new ItemDefinition(9300027, "HeadgearBeretBlue (crew Headgear)", ItemCategory.CrewEquipment, "Headgear:HeadgearBeretBlue") },
        { 9300028, new ItemDefinition(9300028, "HeadgearHeadphones (crew Headgear)", ItemCategory.CrewEquipment, "Headgear:HeadgearHeadphones") },
        { 9300029, new ItemDefinition(9300029, "HeadgearCapPilot (crew Headgear)", ItemCategory.CrewEquipment, "Headgear:HeadgearCapPilot") },
        { 9300030, new ItemDefinition(9300030, "HeadgearCapPilotGoggles (crew Headgear)", ItemCategory.CrewEquipment, "Headgear:HeadgearCapPilotGoggles") },
        { 9300031, new ItemDefinition(9300031, "HeadgearCapPilotGogglesPhones (crew Headgear)", ItemCategory.CrewEquipment, "Headgear:HeadgearCapPilotGogglesPhones") },
        { 9300032, new ItemDefinition(9300032, "HeadgearHelmetLeatherFlying (crew Headgear)", ItemCategory.CrewEquipment, "Headgear:HeadgearHelmetLeatherFlying") },
        { 9300033, new ItemDefinition(9300033, "HeadgearHelmetLeatherFlyingGoggles (crew Headgear)", ItemCategory.CrewEquipment, "Headgear:HeadgearHelmetLeatherFlyingGoggles") },
        { 9300034, new ItemDefinition(9300034, "HeadgearHelmetMk1 (crew Headgear)", ItemCategory.CrewEquipment, "Headgear:HeadgearHelmetMk1") },
        { 9300035, new ItemDefinition(9300035, "HeadgearHelmetMk2 (crew Headgear)", ItemCategory.CrewEquipment, "Headgear:HeadgearHelmetMk2") },
        { 9300036, new ItemDefinition(9300036, "HeadgearHelmetFlakMk1Goggles (crew Headgear)", ItemCategory.CrewEquipment, "Headgear:HeadgearHelmetFlakMk1Goggles") },
        { 9300037, new ItemDefinition(9300037, "HeadgearHelmetFlakMk2Goggles (crew Headgear)", ItemCategory.CrewEquipment, "Headgear:HeadgearHelmetFlakMk2Goggles") },
        { 9300038, new ItemDefinition(9300038, "OxygenBottleSmall (crew Oxygen)", ItemCategory.CrewEquipment, "Oxygen:OxygenBottleSmall") },
        { 9300039, new ItemDefinition(9300039, "OxygenBottleMedium (crew Oxygen)", ItemCategory.CrewEquipment, "Oxygen:OxygenBottleMedium") },
        { 9300040, new ItemDefinition(9300040, "OxygenBottleLarge (crew Oxygen)", ItemCategory.CrewEquipment, "Oxygen:OxygenBottleLarge") },
        { 9300041, new ItemDefinition(9300041, "OxygenBottleSmallToughened (crew Oxygen)", ItemCategory.CrewEquipment, "Oxygen:OxygenBottleSmallToughened") },
        { 9300042, new ItemDefinition(9300042, "OxygenBottleMediumToughened (crew Oxygen)", ItemCategory.CrewEquipment, "Oxygen:OxygenBottleMediumToughened") },
        { 9300043, new ItemDefinition(9300043, "OxygenBottleLargeToughened (crew Oxygen)", ItemCategory.CrewEquipment, "Oxygen:OxygenBottleLargeToughened") },
        { 9300044, new ItemDefinition(9300044, "OxygenBottleAdvanced (crew Oxygen)", ItemCategory.CrewEquipment, "Oxygen:OxygenBottleAdvanced") },
        { 9300045, new ItemDefinition(9300045, "VestSurvivalSeaMk1 (crew Vest)", ItemCategory.CrewEquipment, "Vest:VestSurvivalSeaMk1") },
        { 9300046, new ItemDefinition(9300046, "VestSurvivalLandMk1 (crew Vest)", ItemCategory.CrewEquipment, "Vest:VestSurvivalLandMk1") },
        { 9300047, new ItemDefinition(9300047, "VestSurvivalSeaAndLand (crew Vest)", ItemCategory.CrewEquipment, "Vest:VestSurvivalSeaAndLand") },
        { 9300048, new ItemDefinition(9300048, "VestSurvivalSeaAndLandToughened (crew Vest)", ItemCategory.CrewEquipment, "Vest:VestSurvivalSeaAndLandToughened") },
        { 9300049, new ItemDefinition(9300049, "VestFlakLight (crew Vest)", ItemCategory.CrewEquipment, "Vest:VestFlakLight") },
        { 9300050, new ItemDefinition(9300050, "VestFlakHeavyMk1 (crew Vest)", ItemCategory.CrewEquipment, "Vest:VestFlakHeavyMk1") },
        { 9300051, new ItemDefinition(9300051, "GlovesWoolen (crew Gloves)", ItemCategory.CrewEquipment, "Gloves:GlovesWoolen") },
        { 9300052, new ItemDefinition(9300052, "GlovesWoolenFingerless (crew Gloves)", ItemCategory.CrewEquipment, "Gloves:GlovesWoolenFingerless") },
        { 9300053, new ItemDefinition(9300053, "GlovesLeather (crew Gloves)", ItemCategory.CrewEquipment, "Gloves:GlovesLeather") },
        { 9300054, new ItemDefinition(9300054, "GlovesLeatherToughened (crew Gloves)", ItemCategory.CrewEquipment, "Gloves:GlovesLeatherToughened") },
        { 9300055, new ItemDefinition(9300055, "GlovesMittensThermal (crew Gloves)", ItemCategory.CrewEquipment, "Gloves:GlovesMittensThermal") },
        { 9300056, new ItemDefinition(9300056, "GlovesMittensElectricallyHeated (crew Gloves)", ItemCategory.CrewEquipment, "Gloves:GlovesMittensElectricallyHeated") },
        { 9300057, new ItemDefinition(9300057, "BootsPlimsolls (crew Boots)", ItemCategory.CrewEquipment, "Boots:BootsPlimsolls") },
        { 9300058, new ItemDefinition(9300058, "BootsLeather (crew Boots)", ItemCategory.CrewEquipment, "Boots:BootsLeather") },
        { 9300059, new ItemDefinition(9300059, "BootsLeatherToughened (crew Boots)", ItemCategory.CrewEquipment, "Boots:BootsLeatherToughened") },
        { 9300060, new ItemDefinition(9300060, "BootsThermal (crew Boots)", ItemCategory.CrewEquipment, "Boots:BootsThermal") },
        { 9300061, new ItemDefinition(9300061, "BootsElectricallyHeated (crew Boots)", ItemCategory.CrewEquipment, "Boots:BootsElectricallyHeated") },
        { 9300062, new ItemDefinition(9300062, "FlightsuitBasicOlive (crew Flightsuit)", ItemCategory.CrewEquipment, "Flightsuit:FlightsuitBasicOlive") },
        { 9300063, new ItemDefinition(9300063, "FlightsuitBasicKhaki (crew Flightsuit)", ItemCategory.CrewEquipment, "Flightsuit:FlightsuitBasicKhaki") },
        { 9300064, new ItemDefinition(9300064, "FlightsuitBasicRoyalBlue (crew Flightsuit)", ItemCategory.CrewEquipment, "Flightsuit:FlightsuitBasicRoyalBlue") },
        { 9300065, new ItemDefinition(9300065, "FlightsuitBasicCamoWoodland (crew Flightsuit)", ItemCategory.CrewEquipment, "Flightsuit:FlightsuitBasicCamoWoodland") },
        { 9300066, new ItemDefinition(9300066, "FlightsuitBasicBlack (crew Flightsuit)", ItemCategory.CrewEquipment, "Flightsuit:FlightsuitBasicBlack") },
        { 9300067, new ItemDefinition(9300067, "VestSurvivalSeaMk2 (crew Vest)", ItemCategory.CrewEquipment, "Vest:VestSurvivalSeaMk2") },
        { 9300068, new ItemDefinition(9300068, "VestFlakMedium (crew Vest)", ItemCategory.CrewEquipment, "Vest:VestFlakMedium") },
        { 9300069, new ItemDefinition(9300069, "VestSurvivalLandMk3 (crew Vest)", ItemCategory.CrewEquipment, "Vest:VestSurvivalLandMk3") },
        { 9300070, new ItemDefinition(9300070, "VestSurvivalLandMk2 (crew Vest)", ItemCategory.CrewEquipment, "Vest:VestSurvivalLandMk2") },
        { 9300071, new ItemDefinition(9300071, "VestSurvivalSeaMk3 (crew Vest)", ItemCategory.CrewEquipment, "Vest:VestSurvivalSeaMk3") },
        { 9300072, new ItemDefinition(9300072, "VestFlakHeavyMk2 (crew Vest)", ItemCategory.CrewEquipment, "Vest:VestFlakHeavyMk2") },
        { 9300073, new ItemDefinition(9300073, "VestFlakHeavyMk3 (crew Vest)", ItemCategory.CrewEquipment, "Vest:VestFlakHeavyMk3") },
        { 9300074, new ItemDefinition(9300074, "FlightsuitWinterJumper01 (crew Flightsuit)", ItemCategory.CrewEquipment, "Flightsuit:FlightsuitWinterJumper01") },
        { 9300075, new ItemDefinition(9300075, "FlightsuitWinterJumper02 (crew Flightsuit)", ItemCategory.CrewEquipment, "Flightsuit:FlightsuitWinterJumper02") },
        { 9300076, new ItemDefinition(9300076, "FlightsuitWinterJumper03 (crew Flightsuit)", ItemCategory.CrewEquipment, "Flightsuit:FlightsuitWinterJumper03") },        // Funds drops.
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
