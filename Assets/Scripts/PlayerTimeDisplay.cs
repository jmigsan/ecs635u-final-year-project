using UnityEngine;
using TMPro;

public class PlayerTimeDisplay : MonoBehaviour
{
    [SerializeField] TextMeshPro worldTextDisplay;
    
    void Update()
    {
        if (GameClock.Instance != null && worldTextDisplay != null)
        {
            worldTextDisplay.text = GameClock.Instance.GetTime();
        }
    }
}
