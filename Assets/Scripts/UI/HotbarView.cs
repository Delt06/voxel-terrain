using System;
using InventoryManagement;
using UnityEngine;

namespace UI
{
    public sealed class HotbarView : MonoBehaviour
    {
        [SerializeField] private HotbarViewCell _cellPrefab = default;

        public void Construct(IHotbar hotbar)
        {
            _hotbar = hotbar;
        }

        private void Start()
        {
            Refresh();
        }

        private void OnEnable()
        {
            Refresh();
            _hotbar.ActiveCellIndexChanged += _onActiveCellIndexChanged;
            _hotbar.CellChanged += _onCellChanged;
        }

        private void OnDisable()
        {
            _hotbar.ActiveCellIndexChanged -= _onActiveCellIndexChanged;
            _hotbar.CellChanged -= _onCellChanged;
        }

        private void Awake()
        {
            _onClick = (sender, args) =>
            {
                var cell = (HotbarViewCell) sender;
                _hotbar.ActiveCellIndex = cell.CellIndex;
            };

            _cells = new HotbarViewCell[_hotbar.Size];

            for (var index = 0; index < _cells.Length; index++)
            {
                var cell = Instantiate(_cellPrefab, transform);
                _cells[index] = cell;
                cell.OnClick += _onClick;
            }

            _onActiveCellIndexChanged = (sender, args) => Refresh();
            _onCellChanged = (sender, index) => Refresh(index);
        }

        private void Refresh(int? cellIndex = null)
        {
            if (cellIndex != null)
                RefreshCell(cellIndex.Value);
            else
                for (var index = 0; index < _cells.Length; index++)
                {
                    RefreshCell(index);
                }
        }

        private void RefreshCell(int index)
        {
            var cell = _hotbar.GetCell(index);
            var selected = index == _hotbar.ActiveCellIndex;
            _cells[index].SetUp(index, cell, selected);
        }

        private void OnDestroy()
        {
            foreach (var cell in _cells)
            {
                cell.OnClick -= _onClick;
            }
        }

        private HotbarViewCell[] _cells;
        private ChangeAtIndexEventHandler _onCellChanged;
        private EventHandler _onActiveCellIndexChanged;
        private EventHandler _onClick;
        private IHotbar _hotbar;
    }
}