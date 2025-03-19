using UnityEngine;
using UnityEngine.UI;

public class ColorButton : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
  


    public void setColor()
    {
        LobbyManager.Instance.setColor(this.GetComponent<Image>().color);
        this.GetComponent<Button>().interactable = false;
    }


}
