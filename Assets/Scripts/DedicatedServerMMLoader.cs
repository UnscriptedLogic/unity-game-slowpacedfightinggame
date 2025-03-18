using UnityEngine;
using UnscriptedEngine;

public class DedicatedServerMMLoader : MonoBehaviour
{
    private void Start()
    {

#if DEDICATED_SERVER
        Debug.Log("[SERVER] Skipping to game scene");
        UGameModeBase.instance.LoadScene(1);
#endif
    }
}
