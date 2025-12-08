using UnityEngine;
using System.Collections.Generic;

public class ShipInventory : MonoBehaviour
{
    //items
    public enum ItemType { GoldChest, Alcohol, GoldBag, Boot, None}

    [System.Serializable]
    public class InventorySlot
    {
        public ItemType itemType;
        public int count;
    }

    [Header("Inventory Stats")]
    public int maxStorage = 5;

    [Header("UI Reference")]
    public ShipInventoryUI inventoryUI;

    private Dictionary<ItemType, int> inventory = new Dictionary<ItemType, int>();

    //inspector
    public List<InventorySlot> debugInventoryList = new List<InventorySlot>();

    private void Start()
    {
        //inicializar los tiposde item a 0?
        foreach (ItemType type in System.Enum.GetValues(typeof(ItemType)))
        {
            if(type != ItemType.None)
            {
                inventory[type] = 0;
            }
        }
    }

    //despres aixo ho borro
    private void Update()
    {
        //debugging??
        UpdateDebugList();
    }
    private void UpdateDebugList()
    {
        debugInventoryList.Clear();
        foreach (var pair in inventory)
        {
            if (pair.Value > 0)
            {
                debugInventoryList.Add(new InventorySlot { itemType = pair.Key, count = pair.Value });
            }
        }
    }

    public bool TryAddItem(ItemType type, int amount = 1)
    {
        //calculo num de items total actual
        int currentTotal = 0;
        foreach (var count in inventory.Values)
        {
            currentTotal += count;
        }

        if (currentTotal + amount > maxStorage)
        {
            Debug.Log("Inventario lleno, no se puede recoger: " + type);
            return false;
        }

        //add item
        if (inventory.ContainsKey(type))
        {
            inventory[type] += amount;
        }
        else
        {
            inventory.Add(type, amount);
        }

        if(inventoryUI != null)
        {
            inventoryUI.UpdateUI();
        }

        Debug.Log($"Recogido {amount} de {type}. Total: {inventory[type]}");
        //despues add evento para actualiz UI inventario o player o lo q sea
        return true;
    }

    public int GetItemCount(ItemType type)
    {
        return inventory.ContainsKey(type) ? inventory[type] : 0;
    }
}
