using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;

public class MoveHandlerManager : MonoBehaviour
{
    [SerializeField] private List<MoveTransitionHandler> moveHandlers = new List<MoveTransitionHandler>();

    public bool AreAllTweensDone
    {
        get
        {
            if (moveHandlers == null || moveHandlers.Count == 0) return true;
            foreach (var moveHandler in moveHandlers)
            {
                if (moveHandler.TransitionTween != null)
                {
                    return false;
                }
            }

            return true;
        }
    }

    public void AssignTween(object value)
    {
        if (value is MoveTransitionHandler moveHandler)
        {
            if (moveHandlers == null)
            {
                moveHandlers = new List<MoveTransitionHandler>();
            }

            moveHandlers.Add(moveHandler);
        }
    }

    public void Hide()
    {
        foreach (var moveHandler in moveHandlers)
        {
            moveHandler.TransitionClose();
        }
    }
}