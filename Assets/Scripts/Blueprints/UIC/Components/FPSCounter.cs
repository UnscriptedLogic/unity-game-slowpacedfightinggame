using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class FPSCounter : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textTMP;

    private void Update()
    {
        textTMP.text = $"FPS: {(1f / Time.deltaTime).ToString("0.0")}";
    }
}
