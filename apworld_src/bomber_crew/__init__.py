from dataclasses import dataclass

from BaseClasses import Region, Item, ItemClassification, Location
from Options import DefaultOnToggle, PerGameCommonOptions
from worlds.AutoWorld import World, WebWorld

from . import world_data


class BomberCrewItem(Item):
    game = "Bomber Crew"


class BomberCrewLocation(Location):
    game = "Bomber Crew"


class BomberCrewWeb(WebWorld):
    tutorials = []
    theme = "dirt"


class IncludeDLC1(DefaultOnToggle):
    """Whether to include the DLC1 campaign's missions (and its chapter-clearance item) in the
    pool. Turn this off if you don't own the DLC."""
    display_name = "Include DLC1 Content"


@dataclass
class BomberCrewOptions(PerGameCommonOptions):
    include_dlc1: IncludeDLC1


def _classify(category: str) -> ItemClassification:
    if category in ("BomberUpgradeProgressive", "BomberUpgrade", "CrewEquipment", "MissionUnlock"):
        return ItemClassification.useful
    # Funds, Intel, CrewSkillXp, InstantRepair, InstantHeal: nice to have, never required.
    return ItemClassification.filler


def _is_dlc1_location(name: str) -> bool:
    return name.startswith("DLCMP01_")


def _is_dlc1_item(payload: str) -> bool:
    return payload is not None and "DLCMP01" in payload


_FILLER_NAMES = [
    name for name, (_, category, payload) in world_data.ITEM_DATA.items()
    if _classify(category) == ItemClassification.filler
]


class BomberCrewWorld(World):
    """
    Bomber Crew: fly WWII bombing missions with your crew, upgrade your aircraft, and equip
    your airmen. Completing a mission, buying a bomber upgrade, or equipping a piece of crew
    gear all send checks; the multiworld grants back bomber upgrades, crew equipment, funds,
    intel, crew skill training, mission clearances, and emergency repairs/heals.

    Item/location names and ids are generated from the client mod's own tables
    (Archipelago/ItemTable.cs, Archipelago/LocationTable.cs) by tools/gen_apworld_data.py -
    see world_data.py and rerun that script instead of hand-editing ids here.
    """

    game = "Bomber Crew"
    web = BomberCrewWeb()
    options_dataclass = BomberCrewOptions
    options: BomberCrewOptions

    item_name_to_id = {name: item_id for name, (item_id, _, _) in world_data.ITEM_DATA.items()}
    location_name_to_id = {**world_data.MISSION_LOCATIONS, **world_data.SHOP_LOCATIONS}

    def create_item(self, name: str) -> Item:
        item_id, category, payload = world_data.ITEM_DATA[name]
        return BomberCrewItem(name, _classify(category), item_id, self.player)

    def create_event(self, name: str) -> Item:
        return BomberCrewItem(name, ItemClassification.progression, None, self.player)

    def _active_locations(self) -> dict:
        # location_name_to_id (above) stays the full static id table across every option
        # combination, per AP convention - this is the per-seed subset actually used, filtered
        # by IncludeDLC1.
        if self.options.include_dlc1:
            return self.location_name_to_id
        return {
            name: loc_id
            for name, loc_id in self.location_name_to_id.items()
            if not _is_dlc1_location(name)
        }

    def create_items(self) -> None:
        include_dlc1 = bool(self.options.include_dlc1)

        pool = []
        for name, (_, category, payload) in world_data.ITEM_DATA.items():
            if not include_dlc1 and _is_dlc1_item(payload):
                continue
            # Progressive bomber upgrades need one copy per tier ("{lineId}:{tierCount}"
            # payload) - each copy received bumps that line up one tier (see ItemRewarder).
            copies = int(payload.split(":", 1)[1]) if category == "BomberUpgradeProgressive" else 1
            pool.extend(self.create_item(name) for _ in range(copies))

        # The goal is a separate address=None "Victory" event location, not one of the real
        # (networked) locations, so the pool needs exactly one item per active real location.
        needed = len(self._active_locations()) - len(pool)
        i = 0
        while needed > 0:
            pool.append(self.create_item(_FILLER_NAMES[i % len(_FILLER_NAMES)]))
            i += 1
            needed -= 1

        self.multiworld.itempool += pool

    def create_regions(self) -> None:
        menu = Region("Menu", self.player, self.multiworld)
        self.multiworld.regions.append(menu)

        for name, loc_id in self._active_locations().items():
            menu.locations.append(BomberCrewLocation(self.player, name, loc_id, menu))

        # Goal: a dedicated event location (address=None, not part of location_name_to_id -
        # it's never sent over the network) rather than locking the event onto the real
        # C08_KEY location. Locking an event item directly onto a real-address location
        # crashes this Archipelago version's server (_speedups.LocationStore.__init__:
        # "TypeError: an integer is required") - reproduced and confirmed during development.
        victory_location = BomberCrewLocation(self.player, "Victory", None, menu)
        menu.locations.append(victory_location)
        victory_location.place_locked_item(self.create_event("Victory"))
        victory_location.access_rule = lambda state: state.can_reach_location("C08_KEY", self.player)

    def set_rules(self) -> None:
        # No item-gated access rules on purpose: the real game already enforces campaign
        # progression (chapters unlock sequentially; shop purchases need enough in-game
        # funds/intel) independently of Archipelago, and both always become reachable given
        # enough normal play. See TODO.md in the client mod repo for the full reasoning.
        self.multiworld.completion_condition[self.player] = lambda state: state.has("Victory", self.player)
