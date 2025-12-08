using UnityEngine;

public class Dock : MonoBehaviour
{
    private PlayerMovement dockedShip;

    [Header("UI Reference")]
    public SellUI sellManager;

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"Trigger detectado con: {other.gameObject.name}");
        if (dockedShip == null)
        {
            PlayerMovement playerMovement = other.GetComponent<PlayerMovement>();

            if (playerMovement != null)
            {
                Debug.Log("¡Barco detectado! Abriendo UI.");
                dockedShip = playerMovement;                                
                dockedShip.isDocked = true;
                                
                if (sellManager != null)
                {
                    ShipInventory shipInventory = other.GetComponent<ShipInventory>();
                    if (shipInventory != null)
                    {
                        // Llama a ToggleSellPanel para abrir el popup con el inventario del barco
                        sellManager.ToggleSellPanel(shipInventory);
                    }
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {        
        if (dockedShip != null && other.GetComponent<PlayerMovement>() == dockedShip)
        {            
            if (!sellManager.IsPanelActive())
            {
                dockedShip.isDocked = false;
                dockedShip = null;
            }
        }
    }
        
    public void UndockShip()
    {
        if (dockedShip != null)
        {
            dockedShip.isDocked = false;
            dockedShip = null;
        }
    }
}