using System.Collections.Generic;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnscriptedEngine;

public class UIC_MainMenuUI : UCanvasController
{
    [System.Serializable]
    public class UISet
    {
        [SerializeField] private string id;
        [SerializeField] private RectTransform root;
        [SerializeField] private CinemachineCamera cineCam;

        public string ID => id;
        public RectTransform Root => root;
        public CinemachineCamera CineCam => cineCam;

        public void SetActive(bool value)
        {
            cineCam.gameObject.SetActive(value);
            root.gameObject.SetActive(value);
        }
    }

    [SerializeField] private TextMeshProUGUI versionTMP;

    [SerializeField] private List<UISet> uiSets;
    [SerializeField] private Stack<UISet> activatedSets;

    [Header("Debug")]
    [SerializeField] private int debugIndex;

    private void Start()
    {
        versionTMP.text = $"v{Application.version}";

        activatedSets = new Stack<UISet>();

        //Assumes the first set is the default set
        activatedSets.Push(uiSets[0]);
    }

    public void AttachSet(string id)
    {
        if (activatedSets.Count > 0)
        {
            activatedSets.Peek().SetActive(false);
        }

        for (int i = 0; i < uiSets.Count; i++)
        {
            if (uiSets[i].ID == id)
            {
                uiSets[i].SetActive(true);
                activatedSets.Push(uiSets[i]);
                break;
            }
        }
    }

    public void DettachTopSet()
    {
        if (activatedSets.Count <= 0) return;

        activatedSets.Pop().SetActive(false);
        activatedSets.Peek().SetActive(true);
    }

    private void ShowAtIndex(int index)
    {
        foreach (var set in uiSets)
        {
            set.SetActive(false);
        }

        uiSets[index].SetActive(true);
    }

    private void OnValidate()
    {
        if (debugIndex < 0) debugIndex = 0;
        if (debugIndex >= uiSets.Count) debugIndex = uiSets.Count - 1;
        ShowAtIndex(debugIndex);
    }
}