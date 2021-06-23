using System;
using UnityEngine;

namespace InventoryManagement
{
    public sealed class Inventory : MonoBehaviour, IInventory
    {
        [SerializeField]
        private InventoryCell[] _initialItems = new InventoryCell[0];

        [SerializeField, Min(1)]
        private int _capacity = 9;

        public InventoryCell GetCellAt(int cellIndex)
        {
            EnsureInitialized();
            ValidateCellIndex(cellIndex);
            return _cells[cellIndex];
        }

        public void SetCellAt(int cellIndex, InventoryCell cell)
        {
            EnsureInitialized();
            ValidateCellIndex(cellIndex);
            _cells[cellIndex] = cell;
            CellChanged?.Invoke(this, cellIndex);
        }

        private void ValidateCellIndex(int cellIndex)
        {
            if (cellIndex < 0 || cellIndex >= _cells.Length)
                throw new ArgumentOutOfRangeException(nameof(cellIndex), cellIndex, "Cell index must be valid.");
        }

        public bool TryCollect(ItemConfig item, int count)
        {
            EnsureInitialized();

            if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count));

            for (var cellIndex = 0; cellIndex < _cells.Length; cellIndex++)
            {
                var cell = _cells[cellIndex];

                if (cell.IsEmpty)
                {
                    cell = new InventoryCell(item, count);
                    SetCellAt(cellIndex, cell);
                    return true;
                }

                if (ReferenceEquals(cell.Item, item))
                {
                    cell.Count += count;
                    SetCellAt(cellIndex, cell);
                    return true;
                }
            }

            return false;
        }

        public event ChangeAtIndexEventHandler CellChanged;

        private void Awake()
        {
            EnsureInitialized();
        }

        private void EnsureInitialized()
        {
            if (_initialized) return;

            _initialized = true;
            Initialize();
        }

        private void Initialize()
        {
            _cells = new InventoryCell[_capacity];

            foreach (var item in _initialItems)
            {
                if (item.Count <= 0) continue;
                if (TryCollect(item.Item, item.Count)) continue;
                Debug.LogWarning($"Could not collect an initial item: {item.Count}x {item.Item}.", this);
            }
        }

        private InventoryCell[] _cells;
        private bool _initialized;

        private void OnValidate()
        {
            for (var index = 0; index < _initialItems.Length; index++)
            {
                var item = _initialItems[index];
                item.Count = Mathf.Max(1, item.Count);
                _initialItems[index] = item;
            }
        }
    }
}