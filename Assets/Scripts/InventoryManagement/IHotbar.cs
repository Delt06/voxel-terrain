using System;

namespace InventoryManagement
{
    public interface IHotbar
    {
        int Size { get; }
        int ActiveCellIndex { get; set; }
        InventoryCell GetCell(int index);
        event EventHandler ActiveCellIndexChanged;
        event ChangeAtIndexEventHandler CellChanged;

        IInventory Inventory { get; }
    }
}