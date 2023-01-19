namespace SCMM.Steam.Data.Models.Enums
{
    /// <summary>
    /// https://partner.steamgames.com/doc/features/inventory/schema#Overview
    /// </summary>
    public enum SteamItemDefinitionType : byte
    {
        /// <summary>
        /// An item type that can be found in a player inventory.
        /// </summary>
        Item = 0,

        /// <summary>
        /// Represents a collection of ItemDefs, with an associated quantity of each type. When this item is granted, it automatically expands into the set of items configured in the bundle property.
        /// </summary>
        Bundle,

        /// <summary>
        /// Represents a random item. Granting this item will randomly select one item type from the bundle property, and create an instance of that type. (For example: imagine when a crate is unlocked, then one of the possible items is created)
        /// </summary>
        Generator,

        /// <summary>
        /// This is a special form of generator that can be granted by the ISteamInventory::TriggerItemDrop call from the application.
        /// </summary>
        PlaytimeGenerator,

        /// <summary>
        /// Special item definition that applies tags to item instances (see Steam Inventory Item Tags for more details).
        /// </summary>
        TagGenerator
    }
}
