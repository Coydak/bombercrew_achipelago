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
/// Populate this with the real item IDs from the world definition.
/// </summary>
public static class ItemTable
{
    public static readonly Dictionary<long, ItemDefinition> Items = new()
    {
        // Example placeholder entries. Replace with real AP item IDs and effects.
        // { 91001, new ItemDefinition(91001, "Small Funds Drop", ItemCategory.Funds, "100") },
        // { 91002, new ItemDefinition(91002, "Engine Upgrade Mk II", ItemCategory.BomberUpgrade, "EngineMk2") },
    };

    public static bool TryGetDefinition(long id, out ItemDefinition definition)
    {
        return Items.TryGetValue(id, out definition);
    }
}
