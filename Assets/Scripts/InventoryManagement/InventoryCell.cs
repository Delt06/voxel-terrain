using System;
using UnityEngine;

namespace InventoryManagement
{
    [Serializable]
    public struct InventoryCell
    {
        public ItemConfig Item;
        [Min(0)] public int Count;

        public bool IsEmpty => Count == 0;

        public InventoryCell(ItemConfig item, int count)
        {
            if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count));
            Item = item;
            Count = count;
        }

        public static InventoryCell Empty => new InventoryCell();
    }
}