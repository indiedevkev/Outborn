using UnityEngine;

public class BuildingTest : MonoBehaviour
{
    [SerializeField] private BuildingPlacer buildingPlacer;

    void Update()
    {
        // Press B to enter build mode
        if (UnityEngine.InputSystem.Keyboard.current.bKey.wasPressedThisFrame)
        {
            buildingPlacer.EnterBuildMode(0);
        }
    }
}