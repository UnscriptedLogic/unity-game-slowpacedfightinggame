using System;

public static class PlayerEvents 
{
    public static event Action<ulong> OnPlayerDied;

    public static void PlayerDiedEvent(ulong playerId) => OnPlayerDied?.Invoke(playerId);
}