using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class SellUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject sellPanel;
    public Transform contentParent;
    public GameObject sellSlotPrefab;
    public Dock currentDock; 

    private ShipInventory currentInventory;
    private List<GameObject> activeSlots = new List<GameObject>();

    void Start()
    {
        sellPanel.SetActive(false); 
    }

    public bool IsPanelActive()
    {
        return sellPanel.activeSelf;
    }

    public void ToggleSellPanel(ShipInventory inventory)
    {        
        sellPanel.SetActive(!sellPanel.activeSelf);
        currentInventory = inventory;

        if (sellPanel.activeSelf)
        {            
            PopulateSellList();
        }
        else
        {            
            ClearSellList();
                        
            //Dock dock = FindFirstObjectByType<Dock>();
            //if (dock != null)
            //{
            //    dock.UndockShip();
            //}
            if (currentInventory != null)
            {
                PlayerMovement pm = currentInventory.GetComponent<PlayerMovement>();
                if (pm != null)
                {
                    pm.isDocked = false;
                }
            }
        }
    }

    private void PopulateSellList()
    {
        ClearSellList(); 
                
        foreach (var slotData in currentInventory.debugInventoryList)
        {
            if (currentInventory.itemPrices.ContainsKey(slotData.itemType) && currentInventory.itemPrices[slotData.itemType] > 0)
            {                
                GameObject slot = Instantiate(sellSlotPrefab, contentParent);
                activeSlots.Add(slot);
                                
                SellSlotUI slotUI = slot.GetComponent<SellSlotUI>();
                if (slotUI != null)
                {
                    int price = currentInventory.itemPrices[slotData.itemType];
                    slotUI.SetupSlot(this, slotData.itemType, slotData.count, price);
                }
            }
        }
    }

    private void ClearSellList()
    {
        foreach (GameObject slot in activeSlots)
        {
            Destroy(slot);
        }
        activeSlots.Clear();
    }
        
    public void SellItem(ShipInventory.ItemType type)
    {
        int pricePerUnit = currentInventory.itemPrices[type];
                
        currentInventory.AddGold(pricePerUnit);
                
        currentInventory.RemoveItem(type);
                
        PopulateSellList();
    }

    public void CloseSellPanel()
    {        
        ToggleSellPanel(currentInventory);
    }
}