using UnityEngine;
using System.Collections.Generic;
using TMPro;

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

    [Header("Currency")]
    public int currentGold = 0;
    public TextMeshProUGUI goldText;

    //valor de cada item
    public Dictionary<ItemType, int> itemPrices = new Dictionary<ItemType, int>()
    {
        {ItemType.GoldChest, 500 },
        {ItemType.GoldBag, 100 },
        //otros items no dan dinero
        {ItemType.Alcohol, 0 },
        {ItemType.Boot, 0 }
    };

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
        
        UpdateGoldUI();
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

    public void AddGold(int amount)
    {
        currentGold += amount;
        UpdateGoldUI();
    }

    private void UpdateGoldUI()
    {
        if (goldText != null)
        {
            goldText.text = $"Gold: {currentGold}";
        }
    }

    //restar items al vender
    public void RemoveItem(ItemType type, int amount = 1)
    {
        if (inventory.ContainsKey(type) && inventory[type] >= amount)
        {
            inventory[type] -= amount;
                        
            if (inventoryUI != null)
            {
                inventoryUI.UpdateUI();
            }

            if (inventory[type] == 0)
            {
                // Limpieza si el total llega a cero
                inventory.Remove(type); // Opcional, si quiero q no aparezca en el diccionario
            }
        }
    }
}
