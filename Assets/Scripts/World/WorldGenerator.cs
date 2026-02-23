using UnityEngine;
using Outborn.World;

/// <summary>
/// Prozedurale Weltgeneration (RimWorld-Style):
/// Terrain-Typen aus Noise, Bäume und Steine natürlich verteilt.
/// Optional: Boden-Fliesen pro Zelle für sichtbare Terrain-Vielfalt.
/// </summary>
public class WorldGenerator : MonoBehaviour
{
    [Header("Referenzen")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private GameObject treePrefab;
    [SerializeField] private GameObject stonePrefab;

    [Header("Seed (0 = Zufall)")]
    [SerializeField] private int seed = 12345;

    [Header("Noise (Terrain)")]
    [SerializeField] private float terrainScale = 0.08f;
    [SerializeField] private float moistureOffset = 100f;  // zweiter Kanal für Feuchtigkeit
    [SerializeField] private float rockinessOffset = 200f;  // dritter Kanal für Fels

    [Header("Baum-Platzierung")]
    [SerializeField] private float treeNoiseScale = 0.06f;
    [SerializeField] private float treeThreshold = 0.45f;
    [SerializeField] private float treeSpawnChance = 0.7f;  // nach Threshold noch Zufall
    [SerializeField] private float minDistanceBetweenTrees = 2f;  // Zellen

    [Header("Stein-Platzierung")]
    [SerializeField] private float stoneNoiseScale = 0.07f;
    [SerializeField] private float stoneThreshold = 0.5f;
    [SerializeField] private float stoneSpawnChance = 0.65f;
    [SerializeField] private float minDistanceBetweenStones = 2.5f;

    [Header("Auto-Generierung")]
    [Tooltip("Bei Start automatisch generieren. Sonst nur per Aufruf Generate() oder Kontextmenü.")]
    [SerializeField] private bool generateOnStart = true;

    [Header("Terrain-Sichtbar (optional)")]
    [SerializeField] private bool spawnFloorTiles = false;
    [SerializeField] private GameObject floorTilePrefab;
    [SerializeField] private Color grassColor = new Color(0.2f, 0.5f, 0.2f);
    [SerializeField] private Color dirtColor = new Color(0.4f, 0.3f, 0.2f);
    [SerializeField] private Color stoneColor = new Color(0.45f, 0.45f, 0.5f);
    [SerializeField] private Color sandColor = new Color(0.7f, 0.65f, 0.5f);
    [SerializeField] private Color waterColor = new Color(0.2f, 0.35f, 0.6f);

    private Transform _worldRoot;
    private TerrainType[,] _terrain;
    private int _gridW, _gridH;
    private float _cellSize;

    void Awake()
    {
        if (gridManager == null)
            gridManager = FindFirstObjectByType<GridManager>();
        if (gridManager == null)
        {
            Debug.LogError("[WorldGenerator] GridManager fehlt.");
            enabled = false;
            return;
        }

        _gridW = gridManager.GridWidth;
        _gridH = gridManager.GridHeight;
        _cellSize = gridManager.CellSize;
        _terrain = new TerrainType[_gridW, _gridH];

        if (seed == 0)
            seed = Random.Range(1, 999999);
        Random.InitState(seed);
    }

    void Start()
    {
        if (generateOnStart)
            Generate();
    }

    /// <summary> Komplette Karte neu generieren (z.B. beim Spielstart). </summary>
    [ContextMenu("Generate Now")]
    public void Generate()
    {
        if (_worldRoot != null)
            Destroy(_worldRoot.gameObject);

        _worldRoot = new GameObject("GeneratedWorld").transform;

        GenerateTerrain();
        if (spawnFloorTiles && floorTilePrefab != null)
            SpawnFloorTiles();
        PlaceTrees();
        PlaceStones();
    }

    void GenerateTerrain()
    {
        float s = seed * 0.0001f;
        for (int x = 0; x < _gridW; x++)
        {
            for (int z = 0; z < _gridH; z++)
            {
                float nx = (x + s) * terrainScale;
                float nz = (z + s) * terrainScale;
                float moisture = Mathf.PerlinNoise(nx + moistureOffset, nz);
                float rock = Mathf.PerlinNoise(nx + rockinessOffset, nz);

                if (moisture < 0.25f)
                    _terrain[x, z] = TerrainType.Sand;
                else if (moisture > 0.7f && rock < 0.4f)
                    _terrain[x, z] = TerrainType.Water;
                else if (rock > 0.6f)
                    _terrain[x, z] = TerrainType.Stone;
                else if (rock > 0.4f || moisture < 0.45f)
                    _terrain[x, z] = TerrainType.Dirt;
                else
                    _terrain[x, z] = TerrainType.Grass;
            }
        }
    }

    void SpawnFloorTiles()
    {
        for (int x = 0; x < _gridW; x++)
        {
            for (int z = 0; z < _gridH; z++)
            {
                Vector3Int cell = new Vector3Int(x, 0, z);
                Vector3 worldPos = gridManager.CellToWorld(cell);
                var tile = Instantiate(floorTilePrefab, worldPos, Quaternion.identity, _worldRoot);
                tile.name = $"Floor_{x}_{z}";

                Color c = _terrain[x, z] switch
                {
                    TerrainType.Grass => grassColor,
                    TerrainType.Dirt => dirtColor,
                    TerrainType.Stone => stoneColor,
                    TerrainType.Sand => sandColor,
                    TerrainType.Water => waterColor,
                    _ => grassColor
                };

                var renderer = tile.GetComponentInChildren<Renderer>();
                if (renderer != null && renderer.material != null)
                    renderer.material.color = c;
            }
        }
    }

    void PlaceTrees()
    {
        if (treePrefab == null || treePrefab.GetComponent<ChoppableTree>() == null)
        {
            Debug.LogWarning("[WorldGenerator] Tree-Prefab fehlt oder hat kein ChoppableTree.");
            return;
        }

        float s = seed * 0.0001f;
        for (int x = 0; x < _gridW; x++)
        {
            for (int z = 0; z < _gridH; z++)
            {
                if (_terrain[x, z] == TerrainType.Water || _terrain[x, z] == TerrainType.Stone)
                    continue;
                if (gridManager.IsCellOccupied(new Vector3Int(x, 0, z)))
                    continue;

                float nx = (x + s) * treeNoiseScale;
                float nz = (z + s * 1.3f) * treeNoiseScale;
                float n = Mathf.PerlinNoise(nx, nz);
                if (n < treeThreshold) continue;
                if (Random.value > treeSpawnChance) continue;

                Vector3 worldPos = gridManager.CellToWorld(new Vector3Int(x, 0, z));
                if (TooCloseToOther(worldPos, true, minDistanceBetweenTrees * _cellSize))
                    continue;

                var tree = Instantiate(treePrefab, worldPos, Quaternion.Euler(0, Random.Range(0f, 360f), 0), _worldRoot);
                tree.name = "Tree";
                gridManager.SetCellOccupied(new Vector3Int(x, 0, z), true);
            }
        }
    }

    void PlaceStones()
    {
        if (stonePrefab == null || stonePrefab.GetComponent<MineableStone>() == null)
        {
            Debug.LogWarning("[WorldGenerator] Stone-Prefab fehlt oder hat kein MineableStone.");
            return;
        }

        float s = seed * 0.0001f;
        for (int x = 0; x < _gridW; x++)
        {
            for (int z = 0; z < _gridH; z++)
            {
                if (_terrain[x, z] == TerrainType.Water)
                    continue;
                if (gridManager.IsCellOccupied(new Vector3Int(x, 0, z)))
                    continue;

                float nx = (x + s * 2f) * stoneNoiseScale;
                float nz = (z + s * 0.7f) * stoneNoiseScale;
                float n = Mathf.PerlinNoise(nx, nz);
                if (n < stoneThreshold) continue;
                if (Random.value > stoneSpawnChance) continue;

                Vector3 worldPos = gridManager.CellToWorld(new Vector3Int(x, 0, z));
                if (TooCloseToOther(worldPos, false, minDistanceBetweenStones * _cellSize))
                    continue;

                var stone = Instantiate(stonePrefab, worldPos, Quaternion.Euler(0, Random.Range(0f, 360f), 0), _worldRoot);
                stone.name = "Stone";
                gridManager.SetCellOccupied(new Vector3Int(x, 0, z), true);
            }
        }
    }

    bool TooCloseToOther(Vector3 worldPos, bool checkTrees, float minDist)
    {
        float minSq = minDist * minDist;
        var root = _worldRoot != null ? _worldRoot : transform;
        foreach (Transform t in root)
        {
            if (t.position == worldPos) continue;
            bool isTree = t.GetComponentInChildren<ChoppableTree>() != null;
            bool isStone = t.GetComponentInChildren<MineableStone>() != null;
            if (checkTrees && !isTree) continue;
            if (!checkTrees && !isStone) continue;
            if ((t.position - worldPos).sqrMagnitude < minSq)
                return true;
        }
        return false;
    }

    public TerrainType GetTerrainAt(int x, int z)
    {
        if (x < 0 || x >= _gridW || z < 0 || z >= _gridH) return TerrainType.Grass;
        return _terrain[x, z];
    }

    public int Seed => seed;
}
