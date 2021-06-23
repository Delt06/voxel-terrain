namespace InventoryManagement
{
    public static class HotbarExt
    {
        public static InventoryCell GetActiveCell(this IHotbar hotbar) => hotbar.GetCell(hotbar.ActiveCellIndex);

        public static void SetActiveCell(this IHotbar hotbar, InventoryCell cell)
        {
            hotbar.Inventory.SetCellAt(hotbar.ActiveCellIndex, cell);
        }
    }
}