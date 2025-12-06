using UnityEngine;

public class ItemPickUp : MonoBehaviour
{
    //define item type
    [Header("Item Data")]
    public ShipInventory.ItemType itemType;

    //cantidad a recoger(por si acaso sino despues cambio)
    public int amount = 1;

    private void OnTriggerEnter(Collider other)
    {
        ShipInventory shipInventory = other.GetComponent<ShipInventory>();

        if( shipInventory != null)
        {
            if (shipInventory.TryAddItem(itemType, amount))
            {
                //si el item se ha added con exito
                Destroy(gameObject);
            }
        }
    }
}
