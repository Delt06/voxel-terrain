using System;
using System.Text;
using InventoryManagement;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI
{
    public sealed class HotbarViewCell : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField]
        private Image _icon = default;

        [SerializeField]
        private TMP_Text _countText = default;

        [SerializeField]
        private Graphic _selection = default;

        public void OnPointerClick(PointerEventData eventData)
        {
            OnClick?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler OnClick;

        public int CellIndex { get; private set; }

        public void SetUp(int cellIndex, InventoryCell cell, bool selected)
        {
            CellIndex = cellIndex;

            if (cell.IsEmpty)
            {
                _icon.enabled = false;
                _countText.enabled = false;
            }
            else
            {
                _icon.sprite = cell.Item.MainSprite;
                _icon.enabled = true;

                _stringBuilder.Clear().Append(cell.Count);
                _countText.SetText(_stringBuilder);
                _countText.enabled = true;
            }

            _selection.enabled = selected;
        }

        private readonly StringBuilder _stringBuilder = new StringBuilder();
    }
}