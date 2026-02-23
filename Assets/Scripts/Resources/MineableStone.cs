using UnityEngine;

/// <summary>
/// An Stein-Objekt hängen. Macht es per Mausklick auswählbar;
/// mit [V] kann dann ein "Minen"-Job erstellt werden.
/// Benötigt einen Collider auf diesem oder Kind-Objekt.
/// </summary>
public class MineableStone : MonoBehaviour, ISelectable
{
    [Header("Job")]
    [SerializeField] private float workTime = 8f;
    [SerializeField] private int stoneAmount = 15;

    [Header("Auswahl-Optik")]
    [SerializeField] private Color selectedColor = new Color(0.6f, 0.6f, 0.65f);
    [SerializeField] private float indicatorHeightOffset = 0.3f;

    private bool _isSelected;
    private bool _hasMineJob;
    private Renderer _renderer;
    private Color _originalColor;
    private GameObject _indicatorInstance;

    public float WorkTime => workTime;
    public int StoneAmount => stoneAmount;
    public bool HasMineJob => _hasMineJob;

    void Awake()
    {
        _renderer = GetComponentInChildren<Renderer>();
        if (_renderer != null)
            _originalColor = _renderer.material.color;
    }

    public void SetJobCreated(bool created)
    {
        _hasMineJob = created;
    }

    public void OnSelected()
    {
        _isSelected = true;
        if (_indicatorInstance == null)
            CreateIndicator();
        if (_indicatorInstance != null)
            _indicatorInstance.SetActive(true);
        if (_renderer != null)
            _renderer.material.color = selectedColor;
    }

    public void OnDeselected()
    {
        _isSelected = false;
        if (_indicatorInstance != null)
            _indicatorInstance.SetActive(false);
        if (_renderer != null)
            _renderer.material.color = _originalColor;
    }

    public bool IsSelected() => _isSelected;

    void CreateIndicator()
    {
        _indicatorInstance = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        _indicatorInstance.name = "StoneSelectionIndicator";
        _indicatorInstance.transform.SetParent(transform);
        _indicatorInstance.transform.localPosition = Vector3.up * indicatorHeightOffset;
        _indicatorInstance.transform.localScale = new Vector3(1.2f, 0.05f, 1.2f);
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = selectedColor;
        _indicatorInstance.GetComponent<Renderer>().material = mat;
        Destroy(_indicatorInstance.GetComponent<Collider>());
        _indicatorInstance.SetActive(false);
    }
}
