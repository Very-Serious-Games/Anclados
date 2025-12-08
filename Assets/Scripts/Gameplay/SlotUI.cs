using UnityEngine;
using TMPro;

public class SlotUI : MonoBehaviour
{
    public TextMeshProUGUI itemText;

    public void SetText(string newText)
    {
        if(itemText != null)
        {
            itemText.text = newText;//ho poso per refdeerenciar explicitament aveure si em deixa de donar errror
        }
    }
}
