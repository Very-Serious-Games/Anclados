using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ShipInventoryUI : MonoBehaviour
{
    [Header("Referencias UI")]
    public ShipInventory shipInventory;

    public Transform contentParent;

    public GameObject inventorySlotPrefab;

    private Dictionary<ShipInventory.ItemType, GameObject> itemSlots=
        new Dictionary<ShipInventory.ItemType, GameObject>();

    private void Start()
    {
        if(shipInventory == null)
        {
            shipInventory = Object.FindFirstObjectByType<ShipInventory>();
        }
        UpdateUI();
    }
        
    public void UpdateUI()
    {        
        CleanUpSlots();
                
        foreach (ShipInventory.ItemType type in System.Enum.GetValues(typeof(ShipInventory.ItemType)))
        {
            if (type == ShipInventory.ItemType.None) continue;

            int count = shipInventory.GetItemCount(type);

            if (count > 0)
            {                
                if (itemSlots.ContainsKey(type))
                {                    
                    UpdateSlotText(type, count);
                }
                else
                {                    
                    CreateNewSlot(type, count);
                }
            }
        }
    }

    private void CreateNewSlot(ShipInventory.ItemType type, int count)
    {
        GameObject newSlot = Instantiate(inventorySlotPrefab, contentParent);
        itemSlots.Add(type, newSlot);
        UpdateSlotText(type, count, newSlot);
    }

    private void UpdateSlotText(ShipInventory.ItemType type, int count, GameObject slot = null)
    {        
        GameObject slotToUpdate = slot ?? itemSlots[type];

        //Text textComponent = slotToUpdate.GetComponentInChildren<Text>();

        //if (textComponent != null)
        //{
        //    textComponent.text = $"{type.ToString()}: **{count}**";
        //}
        SlotUI slotComponent = slotToUpdate.GetComponent<SlotUI>();

        if (slotComponent != null)
        {
            slotComponent.SetText($"- {type.ToString()}: {count}");
        }
    }

    private void CleanUpSlots()
    {        
        var keysToRemove = new List<ShipInventory.ItemType>();

        foreach (var pair in itemSlots)
        {
            ShipInventory.ItemType type = pair.Key;
            GameObject slotObject = pair.Value;

            if (shipInventory.GetItemCount(type) <= 0)
            {                
                Destroy(slotObject);
                keysToRemove.Add(type);
            }
        }
                
        foreach (var key in keysToRemove)
        {
            itemSlots.Remove(key);
        }
    }
}
