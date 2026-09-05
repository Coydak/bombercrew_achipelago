# Generates the BomberUpgrade / CrewEquipment entries of ItemTable.cs from the
# real slot schema (BomberRequirements.GetRequirements()) and the real catalogues
# (BomberUpgradeCatalogueLoader / CrewmanGearCatalogueLoader), both dumped live
# from the running game via UnityExplorer.

# (slot_id, upgrade_type, default_name_or_None)
SLOTS = [
    ("fuselage_section_1", "FuselageMain", "FuselageStandard"),
    ("fuselage_section_2", "FuselageMain", "FuselageStandard"),
    ("fuselage_section_3", "FuselageMain", "FuselageStandard"),
    ("fuselage_section_4", "FuselageMain", "FuselageStandard"),
    ("fuselage_section_5", "FuselageMain", "FuselageStandard"),
    ("fuselage_section_wings", "FuselageMain", "FuselageStandard"),
    # fuselage_section_bay_doors (FuselageBombBayDoors) skipped: no catalogue items of that type.
    ("gun_turret_rear", "GunTurret", "GunTurret303x2Mk1"),
    ("gun_turret_top", "GunTurret", "GunTurret303x2Mk1"),
    ("gun_turret_front", "GunTurret", "GunTurret303x2Mk1"),
    ("gun_turret_ventral", "GunTurret", None),
    ("electrical_system", "Electrical", "ElectricalSystemMk1"),
    ("hydraulic_system", "Hyrdaulic", "HydraulicSystemMk1"),
    ("radar_system", "Radar", "RadarMk1"),
    ("extinguisher", "Extinguisher", None),
    ("engine_1", "Engine", "EngineStandardMk1"),
    ("engine_2", "Engine", "EngineStandardMk1"),
    ("engine_3", "Engine", "EngineStandardMk1"),
    ("engine_4", "Engine", "EngineStandardMk1"),
    ("rack_1", "EquipmentRack", "EquipmentRack1"),
    ("rack_2", "EquipmentRack", "EquipmentRack1"),
    ("rack_3", "EquipmentRack", None),
    ("rack_4", "EquipmentRack", None),
    ("livery_base_texture", "Livery", "Livery_Base_Default"),
    ("livery_nose_art", "Livery", None),
    ("livery_engine_art", "Livery", None),
    ("livery_wing_art", "Livery", None),
    ("livery_main_text", "Livery", None),
    ("oxygen_system", "OxygenTank", "OxygenTankMk1"),
    ("survival_dinghy", "SurvivalDinghy", None),
    ("survival_pigeon", "SurvivalPigeon", None),
    ("fuel_tanks", "FuelTank", "FuelTankMk1"),
    # missiles (Missiles) skipped: no catalogue items loaded (DLC-gated, not present in this install).
]

# Livery is special-cased below by name prefix since m_topCategories groups them
# by prefix (NoseArt/EngineArt/WingArt/MainText/Base) rather than by BomberUpgradeType,
# and the requirement's GetUpgradeConfig() is "Livery" for all of them.
LIVERY_PREFIX_TO_SLOT = {
    "Livery_Base_": "livery_base_texture",
    "Livery_NoseArt_": "livery_nose_art",
    "Livery_EngineArt_": "livery_engine_art",
    "Livery_WingArt_": "livery_wing_art",
    "Livery_MainText_": "livery_main_text",
}

# (name, upgrade_type)
BOMBER_CATALOGUE = [
    ("EngineStandardMk1","Engine"),("EngineStandardMk2","Engine"),("EngineStandardMk3","Engine"),
    ("EngineArmouredMk1","Engine"),("EngineArmouredMk2","Engine"),
    ("GunTurret303x2Mk1","GunTurret"),("GunTurret303x2Mk2","GunTurret"),("GunTurret303x4Mk3","GunTurret"),
    ("FuselageStandard","FuselageMain"),("FuselageLightweightMk1","FuselageMain"),("FuselageArmouredMk1","FuselageMain"),
    ("ElectricalSystemMk1","Electrical"),("ElectricalSystemMk2","Electrical"),
    ("ExtinguisherMk1","Extinguisher"),
    ("HydraulicSystemMk1","Hyrdaulic"),("HydraulicSystemMk2","Hyrdaulic"),
    ("RadarMk1","Radar"),("RadarMk2","Radar"),
    ("EquipmentRack1","EquipmentRack"),("EquipmentRack2","EquipmentRack"),("EquipmentRack3","EquipmentRack"),
    ("Livery_NoseArt_CurveDigital","Livery"),("Livery_NoseArt_RunnerDuck","Livery"),("Livery_NoseArt_Skull","Livery"),
    ("Livery_NoseArt_8ball","Livery"),("Livery_NoseArt_FlyingPig","Livery"),("Livery_NoseArt_TopHat","Livery"),
    ("Livery_NoseArt_CatsEyes01","Livery"),("Livery_NoseArt_VampireBat","Livery"),("Livery_NoseArt_Pumpkin","Livery"),
    ("Livery_EngineArt_Teeth","Livery"),("Livery_EngineArt_Flames","Livery"),("Livery_EngineArt_Lightning","Livery"),
    ("Livery_EngineArt_AceOfSpades","Livery"),("Livery_EngineArt_Skull","Livery"),("Livery_EngineArt_Pumpkin","Livery"),
    ("Livery_MainText_White","Livery"),("Livery_MainText_Black","Livery"),("Livery_MainText_Red","Livery"),("Livery_MainText_Yellow","Livery"),
    ("Livery_Base_Pumpkin","Livery"),("Livery_Base_Festive","Livery"),("Livery_Base_GiftWrap","Livery"),("Livery_Base_Pigeon","Livery"),
    ("Livery_Base_SalmonPink","Livery"),("Livery_Base_White","Livery"),("Livery_Base_Olive","Livery"),("Livery_Base_Yellow","Livery"),
    ("Livery_Base_Red","Livery"),("Livery_Base_SeaBlue","Livery"),("Livery_Base_IdentityCrisis","Livery"),("Livery_Base_CurveDigital","Livery"),
    ("Livery_Base_Default","Livery"),
    ("OxygenTankMk1","OxygenTank"),("OxygenTankMk2","OxygenTank"),
    ("GunTurret303x2Mk3","GunTurret"),
    ("RadarMk3","Radar"),
    ("FuselageArmouredMk2","FuselageMain"),
    ("RadarMk4","Radar"),
    ("DinghyMk1","SurvivalDinghy"),("DinghyMk2","SurvivalDinghy"),("DinghyMk3","SurvivalDinghy"),
    ("PigeonMk1","SurvivalPigeon"),("PigeonMk2","SurvivalPigeon"),("PigeonMk3","SurvivalPigeon"),
    ("FuelTankMk1","FuelTank"),("FuelTankMk2","FuelTank"),("FuelTankMk3","FuelTank"),("FuelTankSelfSealingMk1","FuelTank"),
    ("Livery_NoseArt_Custom4","Livery"),("Livery_EngineArt_Custom2","Livery"),("Livery_EngineArt_Custom4","Livery"),
    ("Livery_NoseArt_Custom1","Livery"),("Livery_NoseArt_Custom2","Livery"),
    ("ElectricalSystemMk3","Electrical"),
    ("Livery_NoseArt_Custom3","Livery"),("Livery_NoseArt_Custom0","Livery"),
    ("Livery_EngineArt_Custom3","Livery"),("Livery_EngineArt_Custom1","Livery"),("Livery_EngineArt_Custom0","Livery"),
    ("FuselageLightweightMk3","FuselageMain"),("FuselageLightweightMk2","FuselageMain"),
    ("EngineArmouredMk3","Engine"),
    ("FuselageLightweightMk4","FuselageMain"),("FuselageArmouredMk4","FuselageMain"),("FuselageArmouredMk3","FuselageMain"),
    ("FuselageLightweightMk5","FuselageMain"),("FuselageArmouredMk5","FuselageMain"),("FuselageArmouredMk6","FuselageMain"),("FuselageArmouredMk7","FuselageMain"),
    ("EngineArmouredMk4","Engine"),("EngineLightMk2","Engine"),("EngineLightMk1","Engine"),
    ("GunTurret50x4Mk4","GunTurret"),("GunTurret50x4Mk3","GunTurret"),("GunTurret303x4Mk4","GunTurret"),
    ("GunTurret50x2Mk3","GunTurret"),("GunTurret50x2Mk2","GunTurret"),("GunTurret50x2Mk1","GunTurret"),
    ("HydraulicSystemMk4","Hyrdaulic"),
    ("RadarMk6","Radar"),
    ("HydraulicSystemMk3","Hyrdaulic"),
    ("ExtinguisherMk2","Extinguisher"),
    ("OxygenTankMk3","OxygenTank"),
    ("RadarMk5","Radar"),
    ("ElectricalSystemMk5","Electrical"),
    ("ExtinguisherMk3","Extinguisher"),("ExtinguisherMk4","Extinguisher"),
    ("ElectricalSystemMk4","Electrical"),
    ("EngineStandardMk5","Engine"),("EngineLightMk3","Engine"),("EngineStandardMk4","Engine"),("EngineArmouredMk5","Engine"),
    ("Livery_WingArt_Round01","Livery"),("Livery_WingArt_Star01","Livery"),("Livery_WingArt_AceOfSpades","Livery"),
    ("Livery_WingArt_8ball","Livery"),("Livery_WingArt_Pumpkin","Livery"),
    ("Livery_WingArt_Custom0","Livery"),("Livery_WingArt_Custom1","Livery"),("Livery_WingArt_Custom2","Livery"),
    ("Livery_WingArt_Custom3","Livery"),("Livery_WingArt_Custom4","Livery"),
    ("GunTurret303x2Mk3_AmmoFeed","GunTurret"),("GunTurret50x4Mk3_AmmoFeed","GunTurret"),("GunTurret303x2Mk1_AmmoFeed","GunTurret"),
    ("GunTurret50x2Mk2_AmmoFeed","GunTurret"),("GunTurret303x2Mk2_AmmoFeed","GunTurret"),("GunTurret303x4Mk3_AmmoFeed","GunTurret"),
    ("GunTurret50x2Mk3_AmmoFeed","GunTurret"),("GunTurret50x2Mk1_AmmoFeed","GunTurret"),
    ("Livery_EngineArt_ProfilePic","Livery"),("Livery_WingArt_ProfilePic","Livery"),("Livery_NoseArt_ProfilePic","Livery"),
    ("Livery_NoseArt_ProfilePicRound","Livery"),("Livery_WingArt_ProfilePicRound","Livery"),("Livery_EngineArt_ProfilePicRound","Livery"),
    ("Livery_NoseArt_CompetitionArt01","Livery"),("Livery_NoseArt_CompetitionArt02","Livery"),("Livery_NoseArt_CompetitionArt03","Livery"),
    ("Livery_WingArt_CompetitionArt01","Livery"),("Livery_WingArt_CompetitionArt02","Livery"),("Livery_WingArt_CompetitionArt03","Livery"),
]

# (name, gear_type)
CREW_CATALOGUE = [
    ("HeadgearNone","Headgear"),("HeadgearHatWinter01","Headgear"),("HeadgearHatWinter02","Headgear"),("HeadgearHatWinter03","Headgear"),
    ("HeadgearCapService","Headgear"),("HeadgearBeretBlue","Headgear"),("HeadgearHeadphones","Headgear"),("HeadgearCapPilot","Headgear"),
    ("HeadgearCapPilotGoggles","Headgear"),("HeadgearCapPilotGogglesPhones","Headgear"),("HeadgearHelmetLeatherFlying","Headgear"),
    ("HeadgearHelmetLeatherFlyingGoggles","Headgear"),("HeadgearHelmetMk1","Headgear"),("HeadgearHelmetMk2","Headgear"),
    ("HeadgearHelmetFlakMk1Goggles","Headgear"),("HeadgearHelmetFlakMk2Goggles","Headgear"),
    ("OxygenNone","Oxygen"),("OxygenBottleSmall","Oxygen"),("OxygenBottleMedium","Oxygen"),("OxygenBottleLarge","Oxygen"),
    ("OxygenBottleSmallToughened","Oxygen"),("OxygenBottleMediumToughened","Oxygen"),("OxygenBottleLargeToughened","Oxygen"),("OxygenBottleAdvanced","Oxygen"),
    ("VestNone","Vest"),("VestSurvivalSeaMk1","Vest"),("VestSurvivalLandMk1","Vest"),("VestSurvivalSeaAndLand","Vest"),
    ("VestSurvivalSeaAndLandToughened","Vest"),("VestFlakLight","Vest"),("VestFlakHeavyMk1","Vest"),
    ("GlovesNone","Gloves"),("GlovesWoolen","Gloves"),("GlovesWoolenFingerless","Gloves"),("GlovesLeather","Gloves"),
    ("GlovesLeatherToughened","Gloves"),("GlovesMittensThermal","Gloves"),("GlovesMittensElectricallyHeated","Gloves"),
    ("BootsNone","Boots"),("BootsPlimsolls","Boots"),("BootsLeather","Boots"),("BootsLeatherToughened","Boots"),
    ("BootsThermal","Boots"),("BootsElectricallyHeated","Boots"),
    ("FlightsuitNone","Flightsuit"),("FlightsuitBasicOlive","Flightsuit"),("FlightsuitBasicKhaki","Flightsuit"),
    ("FlightsuitBasicRoyalBlue","Flightsuit"),("FlightsuitBasicCamoWoodland","Flightsuit"),("FlightsuitBasicBlack","Flightsuit"),
    ("VestSurvivalSeaMk2","Vest"),("VestFlakMedium","Vest"),("VestSurvivalLandMk3","Vest"),("VestSurvivalLandMk2","Vest"),
    ("VestSurvivalSeaMk3","Vest"),("VestFlakHeavyMk2","Vest"),("VestFlakHeavyMk3","Vest"),
    ("FlightsuitWinterJumper01","Flightsuit"),("FlightsuitWinterJumper02","Flightsuit"),("FlightsuitWinterJumper03","Flightsuit"),
]

def livery_slot_for(name):
    for prefix, slot in LIVERY_PREFIX_TO_SLOT.items():
        if name.startswith(prefix):
            return slot
    return None

# Non-cosmetic bomber upgrades are progressive: one item per upgrade *line* (parallel
# variants like Standard/Armoured/Light engines are separate lines, never merged), applied
# fleet-wide to every slot of the matching BomberUpgradeType. Receiving another copy moves
# that line up one tier. Ordered lowest to highest tier; payload is "{lineId}:{tierCount}"
# so the .apworld generator knows how many copies to put in the pool without duplicating
# this table in Python.
PROGRESSIVE_LINES = {
    "EngineStandard": ("Engine", ["EngineStandardMk1", "EngineStandardMk2", "EngineStandardMk3", "EngineStandardMk4", "EngineStandardMk5"]),
    "EngineArmoured": ("Engine", ["EngineArmouredMk1", "EngineArmouredMk2", "EngineArmouredMk3", "EngineArmouredMk4", "EngineArmouredMk5"]),
    "EngineLight": ("Engine", ["EngineLightMk1", "EngineLightMk2", "EngineLightMk3"]),
    "GunTurret303x2": ("GunTurret", ["GunTurret303x2Mk1", "GunTurret303x2Mk2", "GunTurret303x2Mk3"]),
    "GunTurret303x2AmmoFeed": ("GunTurret", ["GunTurret303x2Mk1_AmmoFeed", "GunTurret303x2Mk2_AmmoFeed", "GunTurret303x2Mk3_AmmoFeed"]),
    "GunTurret303x4": ("GunTurret", ["GunTurret303x4Mk3", "GunTurret303x4Mk4"]),
    "GunTurret303x4AmmoFeed": ("GunTurret", ["GunTurret303x4Mk3_AmmoFeed"]),
    "GunTurret50x4": ("GunTurret", ["GunTurret50x4Mk3", "GunTurret50x4Mk4"]),
    "GunTurret50x4AmmoFeed": ("GunTurret", ["GunTurret50x4Mk3_AmmoFeed"]),
    "GunTurret50x2": ("GunTurret", ["GunTurret50x2Mk1", "GunTurret50x2Mk2", "GunTurret50x2Mk3"]),
    "GunTurret50x2AmmoFeed": ("GunTurret", ["GunTurret50x2Mk1_AmmoFeed", "GunTurret50x2Mk2_AmmoFeed", "GunTurret50x2Mk3_AmmoFeed"]),
    "FuselageLightweight": ("FuselageMain", ["FuselageLightweightMk1", "FuselageLightweightMk2", "FuselageLightweightMk3", "FuselageLightweightMk4", "FuselageLightweightMk5"]),
    "FuselageArmoured": ("FuselageMain", ["FuselageArmouredMk1", "FuselageArmouredMk2", "FuselageArmouredMk3", "FuselageArmouredMk4", "FuselageArmouredMk5", "FuselageArmouredMk6", "FuselageArmouredMk7"]),
    "Electrical": ("Electrical", ["ElectricalSystemMk1", "ElectricalSystemMk2", "ElectricalSystemMk3", "ElectricalSystemMk4", "ElectricalSystemMk5"]),
    "Hydraulic": ("Hyrdaulic", ["HydraulicSystemMk1", "HydraulicSystemMk2", "HydraulicSystemMk3", "HydraulicSystemMk4"]),
    "Radar": ("Radar", ["RadarMk1", "RadarMk2", "RadarMk3", "RadarMk4", "RadarMk5", "RadarMk6"]),
    "Extinguisher": ("Extinguisher", ["ExtinguisherMk1", "ExtinguisherMk2", "ExtinguisherMk3", "ExtinguisherMk4"]),
    "EquipmentRack": ("EquipmentRack", ["EquipmentRack1", "EquipmentRack2", "EquipmentRack3"]),
    "OxygenTank": ("OxygenTank", ["OxygenTankMk1", "OxygenTankMk2", "OxygenTankMk3"]),
    "FuelTank": ("FuelTank", ["FuelTankMk1", "FuelTankMk2", "FuelTankMk3"]),
    "FuelTankSelfSealing": ("FuelTank", ["FuelTankSelfSealingMk1"]),
    "SurvivalDinghy": ("SurvivalDinghy", ["DinghyMk1", "DinghyMk2", "DinghyMk3"]),
    "SurvivalPigeon": ("SurvivalPigeon", ["PigeonMk1", "PigeonMk2", "PigeonMk3"]),
}

entries = []  # (item_id_offset, display_name, category, payload)

# --- Progressive bomber upgrades: one item per line, applied fleet-wide. ---
for line_id, (upgrade_type, tiers) in PROGRESSIVE_LINES.items():
    display = f"Progressive {line_id}"
    payload = f"{line_id}:{len(tiers)}"
    entries.append((display, "BomberUpgradeProgressive", payload))

# --- Cosmetic livery: still one item per slot x catalogue item (no tiering concept for skins). ---
for slot_id, slot_type, default_name in SLOTS:
    if slot_type != "Livery":
        continue
    for item_name, item_type in BOMBER_CATALOGUE:
        if item_type != "Livery" or livery_slot_for(item_name) != slot_id:
            continue
        if item_name == default_name:
            continue
        display = f"{item_name} ({slot_id})"
        payload = f"{slot_id}:{item_name}"
        entries.append((display, "BomberUpgrade", payload))

# --- Crew equipment: one item per non-default catalogue entry ---
for item_name, gear_type in CREW_CATALOGUE:
    if item_name.endswith("None"):
        continue
    display = f"{item_name} (crew {gear_type})"
    payload = f"{gear_type}:{item_name}"
    entries.append((display, "CrewEquipment", payload))

print(f"-- total entries: {len(entries)}")

BASE_ID = 9300000
lines = []
for i, (display, category, payload) in enumerate(entries):
    item_id = BASE_ID + i
    esc_display = display.replace('"', '\\"')
    esc_payload = payload.replace('"', '\\"')
    lines.append(f'        {{ {item_id}, new ItemDefinition({item_id}, "{esc_display}", ItemCategory.{category}, "{esc_payload}") }},')

with open("gen_output.txt", "w") as f:
    f.write("\n".join(lines))

print("wrote", len(lines), "lines to gen_output.txt")
