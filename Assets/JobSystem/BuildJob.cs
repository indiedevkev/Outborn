using UnityEngine;

public class BuildJob : Job
{
    public GameObject buildingPrefab;
    public Vector3Int gridPosition;  // ← Statt GridObject!
    private GameObject constructionSite;

    public BuildJob(Vector3 location, Vector3Int gridPos, GameObject prefab, float buildTime = 5f) 
        : base(JobType.Build, location, buildTime)
    {
        buildingPrefab = prefab;
        gridPosition = gridPos;
        priority = JobPriority.Normal;
    }

    public override void StartJob()
    {
        base.StartJob();
        
        // Create construction site visual (optional)
        // constructionSite = CreateConstructionSiteVisual();
    }

    public override void DoWork(float amount)
    {
        base.DoWork(amount);
        
        // Update construction visual progress
        // UpdateConstructionVisual(GetProgress());
    }

    public override void CompleteJob()
    {
        base.CompleteJob();
        
        // Spawn actual building
        if (buildingPrefab != null)
        {
            GameObject building = Object.Instantiate(buildingPrefab, workLocation, Quaternion.identity);
            Debug.Log($"Building constructed at {workLocation}");
        }
        
        // Remove construction site
        if (constructionSite != null)
        {
            Object.Destroy(constructionSite);
        }
        
        // Mark grid as occupied
       GridManager gridManager = Object.FindFirstObjectByType<GridManager>();
        if (gridManager != null)
        {
            gridManager.SetCellOccupied(gridPosition, true);
        }
    }

    public override void CancelJob()
    {
        base.CancelJob();
        
        // Remove construction site
        if (constructionSite != null)
        {
            Object.Destroy(constructionSite);
        }
    }
}