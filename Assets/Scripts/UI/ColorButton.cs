using UnityEngine;
using UnityEngine.UI;

public class ColorButton : MonoBehaviour
{
    public void setColor()
    {
        LobbyManager.Instance.setColor(this.GetComponent<Image>().color);
        this.GetComponent<Button>().interactable = false;
    }
}
