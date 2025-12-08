using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SellSlotUI : MonoBehaviour
{
    public TextMeshProUGUI itemText;
    public TextMeshProUGUI priceText;
    public Button sellButton;

    private SellUI sellManager;
    private ShipInventory.ItemType itemType;

    public void SetupSlot(SellUI manager, ShipInventory.ItemType type, int count, int price)
    {
        sellManager = manager;
        itemType = type;

        itemText.text = $"{type.ToString()}: {count}";
        priceText.text = $"Sell: {price}G"; 
                
        sellButton.onClick.RemoveAllListeners();
        sellButton.onClick.AddListener(() => sellManager.SellItem(itemType));
                
        if (count == 0)
        {
            sellButton.interactable = false;
        }
    }
}
