using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// P = Holz hacken (bei ausgewähltem Baum), V = Minen (bei ausgewähltem Stein).
/// Erstellt HarvestJob bzw. MineJob und meldet sie beim JobManager an.
/// </summary>
public class ResourceJobInput : MonoBehaviour
{
    [SerializeField] private SelectionManager selectionManager;
    [SerializeField] private bool allowMultipleJobsPerObject = false;

    void Update()
    {
        if (selectionManager == null)
            selectionManager = FindFirstObjectByType<SelectionManager>();
        if (selectionManager == null || JobManager.Instance == null) return;

        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        var selected = selectionManager.GetSelectedObjects();
        if (selected.Count == 0) return;

        // P = Chop (nur erster ausgewählter Baum)
        if (keyboard.pKey.wasPressedThisFrame)
        {
            foreach (var sel in selected)
            {
                var mb = sel as MonoBehaviour;
                if (mb == null) continue;
                var tree = mb.GetComponent<ChoppableTree>();
                if (tree == null) continue;
                if (!allowMultipleJobsPerObject && tree.HasChopJob) continue;

                tree.SetJobCreated(true);
                var job = new HarvestJob(tree);
                JobManager.Instance.AddJob(job);
                break;
            }
        }

        // V = Mine (nur erster ausgewählter Stein)
        if (keyboard.vKey.wasPressedThisFrame)
        {
            foreach (var sel in selected)
            {
                var mb = sel as MonoBehaviour;
                if (mb == null) continue;
                var stone = mb.GetComponent<MineableStone>();
                if (stone == null) continue;
                if (!allowMultipleJobsPerObject && stone.HasMineJob) continue;

                stone.SetJobCreated(true);
                var job = new MineJob(stone);
                JobManager.Instance.AddJob(job);
                break;
            }
        }
    }
}
