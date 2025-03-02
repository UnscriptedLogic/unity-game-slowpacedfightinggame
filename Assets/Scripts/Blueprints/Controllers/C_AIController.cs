using UnityEngine;
using UnscriptedEngine;

public class C_AIController : C_PlayerController
{
    private float spamInterval = 0.5f;
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
            possessedPawn.OnDefaultLeftMouseDown();

            interval = spamInterval;
        }
        else
        {
            interval -= Time.deltaTime;
        }
    }
}
