using UnityEngine;
using Outborn.Inventory;

/// <summary>
/// Job: Stein abbauen (Minen). Nach Abschluss: Stein ins Inventar, Stein-Objekt entfernen.
/// </summary>
public class MineJob : Job
{
    public MineableStone Stone { get; private set; }
    public int StoneAmount { get; private set; }

    public MineJob(MineableStone stone) : base(JobType.Mine, stone.transform.position, stone.WorkTime)
    {
        Stone = stone;
        StoneAmount = stone.StoneAmount;
    }

    public override void CompleteJob()
    {
        base.CompleteJob();
        if (Stone != null)
        {
            if (ColonyInventory.Instance != null)
                ColonyInventory.Instance.Add(ResourceType.Stone, StoneAmount);
            UnityEngine.Object.Destroy(Stone.gameObject);
        }
    }

    public override void CancelJob()
    {
        if (Stone != null)
            Stone.SetJobCreated(false);
        base.CancelJob();
    }
}
