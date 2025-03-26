using UnityEngine;
using UnityEngine.UI;

public class ButtonKill : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Awake()
    {
        this.GetComponent<Button>().interactable = false;
    }

    public void kill()
    {
        GameManager.Instance.Matar();
    }
}
