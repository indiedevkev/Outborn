using UnityEngine;
using Outborn.Inventory;

/// <summary>
/// Job: Baum fällen (Holz hacken). Nach Abschluss: Holz ins Inventar, Baum entfernen.
/// </summary>
public class HarvestJob : Job
{
    public ChoppableTree Tree { get; private set; }
    public int WoodAmount { get; private set; }

    public HarvestJob(ChoppableTree tree) : base(JobType.Harvest, tree.transform.position, tree.WorkTime)
    {
        Tree = tree;
        WoodAmount = tree.WoodAmount;
    }

    public override void CompleteJob()
    {
        base.CompleteJob();
        if (Tree != null)
        {
            if (ColonyInventory.Instance != null)
                ColonyInventory.Instance.Add(ResourceType.Wood, WoodAmount);
            UnityEngine.Object.Destroy(Tree.gameObject);
        }
    }

    public override void CancelJob()
    {
        if (Tree != null)
            Tree.SetJobCreated(false);
        base.CancelJob();
    }
}
