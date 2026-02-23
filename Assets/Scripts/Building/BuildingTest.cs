using UnityEngine;

public class BuildingTest : MonoBehaviour
{
    [SerializeField] private BuildingPlacer buildingPlacer;
    [SerializeField] private BuildMenuUI buildMenuUI;

    void Update()
    {
        // Press B to toggle build menu + build mode
        if (UnityEngine.InputSystem.Keyboard.current.bKey.wasPressedThisFrame)
        {
            if (buildMenuUI != null)
            {
                buildMenuUI.ToggleMenu();
            }
            else if (buildingPlacer != null)
            {
                buildingPlacer.ToggleBuildMode();
            }
        }
    }
}