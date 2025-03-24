using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class OnHoverShadow : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Shadow shadow;
    [SerializeField] private Vector2 hidden = Vector2.zero;
    [SerializeField] private Vector2 hover = new Vector2(5f, -5f);
    [SerializeField] private Ease ease = Ease.OutBack;
    [SerializeField] private float duration = 0.25f;

    public void OnPointerEnter(PointerEventData eventData)
    {
        Assert.IsNotNull(shadow, "Shadow is not assigned.");

        DOTween.To(() => shadow.effectDistance, x => shadow.effectDistance = x, hover, duration).SetEase(ease);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Assert.IsNotNull(shadow, "Shadow is not assigned.");

        DOTween.To(() => shadow.effectDistance, x => shadow.effectDistance = x, hidden, duration).SetEase(ease);
    }
}
