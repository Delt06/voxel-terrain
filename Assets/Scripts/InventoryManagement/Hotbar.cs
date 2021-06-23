using System;
using UnityEngine;

namespace InventoryManagement
{
    public sealed class Hotbar : MonoBehaviour, IHotbar
    {
        [SerializeField, Min(1)] private int _size = 4;

        public void Construct(IInventory inventory)
        {
            Inventory = inventory;
        }

        public int Size => _size;

        public int ActiveCellIndex
        {
            get => _activeCellIndex;
            set
            {
                _activeCellIndex = Mathf.Clamp(value, 0, _size - 1);
                ActiveCellIndexChanged?.Invoke(this, EventArgs.Empty);
            }
        }


        public event EventHandler ActiveCellIndexChanged;
        public event ChangeAtIndexEventHandler CellChanged;

        public InventoryCell GetCell(int index) => Inventory.GetCellAt(index);

        public IInventory Inventory { get; private set; }

        private void OnEnable()
        {
            Inventory.CellChanged += _onInventoryCellChanged;
        }

        private void OnDisable()
        {
            Inventory.CellChanged -= _onInventoryCellChanged;
        }

        private void Awake()
        {
            _onInventoryCellChanged = (sender, index) =>
            {
                if (index >= Size) return;
                CellChanged?.Invoke(this, index);
            };
        }

        private ChangeAtIndexEventHandler _onInventoryCellChanged;
        private int _activeCellIndex;
    }
}