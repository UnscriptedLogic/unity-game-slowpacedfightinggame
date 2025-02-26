using UnityEngine;
using UnscriptedEngine;

public class C_AIController : UController
{
    private float spamInterval = 0.1f;
    private float interval;

    public override void PossessPawn(ULevelPawn pawn, bool overrideCurrentPawn = false)
    {
        base.PossessPawn(pawn, overrideCurrentPawn);
    }

    private void Update()
    {
        if (!IsServer) return;

        if (possessedPawn == null) return;

        if (interval <= 0f)
        {
            OnDefaultLeftMouseDown();

            interval = spamInterval;
        }
        else
        {
            interval -= Time.deltaTime;
        }
    }
}
