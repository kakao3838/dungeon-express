using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class ItemData : ScriptableObject
{
    public string itemName;
    public Sprite icon;
    [TextArea] public string description;
    [Range(0, 100)] public int quality;
    public bool isLocked;

    public enum Rarity { Common, Rare, Epic, Legendary }
    public Rarity rarity;
}