using UnityEngine;

public abstract class ItemConfig : ScriptableObject
{
    [SerializeField, Min(0)] private int _id = 0;

    public abstract Sprite MainSprite { get; }

    public int ID => _id;
}