using System.Collections.Generic;
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

    [SerializeField] private List<UISet> uiSets;
    [SerializeField] private Queue<UISet> activatedSets;

    private void Start()
    {
        uiSets = new List<UISet>();
        activatedSets = new Queue<UISet>();
    }

    private void AttachSet(string id)
    {
        if (activatedSets.Count > 0)
        {
            UISet set = activatedSets.Peek();
            set.SetActive(false);
        }

        for (int i = 0; i < uiSets.Count; i++)
        {
            UISet set = uiSets[i];
            if (set == null) continue;

            set.SetActive(true);
            activatedSets.Enqueue(set);
        }
    }

    private void DettachTopSet()
    {
        if (activatedSets.Count <= 0) return;

        activatedSets.Dequeue().SetActive(false);
        activatedSets.Peek().SetActive(true);
    }
}