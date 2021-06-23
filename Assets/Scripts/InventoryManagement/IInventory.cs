namespace InventoryManagement
{
    public interface IInventory
    {
        InventoryCell GetCellAt(int cellIndex);
        void SetCellAt(int cellIndex, InventoryCell cell);
        bool TryCollect(ItemConfig item, int count);
        event ChangeAtIndexEventHandler CellChanged;
    }
}