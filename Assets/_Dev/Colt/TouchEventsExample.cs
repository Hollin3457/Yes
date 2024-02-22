using Microsoft.MixedReality.Toolkit.Input;
using UnityEngine;

public class TouchEventsExample : MonoBehaviour, IMixedRealityTouchHandler
{
    public Transform trfm;

    public void OnTouchStarted(HandTrackingInputEventData eventData)
    {
        foreach (var pointer in eventData.InputSource.Pointers)
        {
            PokePointer poke = pointer as PokePointer;
            var nearPointer = pointer as IMixedRealityNearPointer;

            Debug.Log($"IsPoke: {poke != null} - {poke?.Position}, IsNear: {nearPointer != null} - {nearPointer?.Position}, InputData: {eventData.InputData}");
            if (poke != null)
            {
                Instantiate(trfm, poke.Position, Quaternion.identity);
            }
        }
    }
    public void OnTouchCompleted(HandTrackingInputEventData eventData) { }
    public void OnTouchUpdated(HandTrackingInputEventData eventData)
    {
        foreach (var pointer in eventData.InputSource.Pointers)
        {
            PokePointer poke = pointer as PokePointer;

            if (poke != null)
            {
                Debug.Log($"U-IsPoke: {poke != null} - {poke?.Position}, InputData: {eventData.InputData}");
                Instantiate(trfm, poke.Position, Quaternion.identity);
            }
        }
    }
}
