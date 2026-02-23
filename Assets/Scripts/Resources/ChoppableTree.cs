using UnityEngine;

/// <summary>
/// An Baum-Objekt hängen. Macht es per Mausklick auswählbar;
/// mit [P] kann dann ein "Holz hacken"-Job erstellt werden.
/// Benötigt einen Collider auf diesem oder Kind-Objekt.
/// </summary>
public class ChoppableTree : MonoBehaviour, ISelectable
{
    [Header("Job")]
    [SerializeField] private float workTime = 5f;
    [SerializeField] private int woodAmount = 20;

    [Header("Auswahl-Optik")]
    [SerializeField] private Color selectedColor = new Color(0.4f, 0.8f, 0.3f);
    [SerializeField] private float indicatorHeightOffset = 0.5f;

    private bool _isSelected;
    private bool _hasChopJob;
    private Renderer _renderer;
    private Color _originalColor;
    private GameObject _indicatorInstance;

    public float WorkTime => workTime;
    public int WoodAmount => woodAmount;
    public bool HasChopJob => _hasChopJob;

    void Awake()
    {
        _renderer = GetComponentInChildren<Renderer>();
        if (_renderer != null)
            _originalColor = _renderer.material.color;
    }

    public void SetJobCreated(bool created)
    {
        _hasChopJob = created;
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
        _indicatorInstance.name = "TreeSelectionIndicator";
        _indicatorInstance.transform.SetParent(transform);
        _indicatorInstance.transform.localPosition = Vector3.up * indicatorHeightOffset;
        _indicatorInstance.transform.localScale = new Vector3(1.5f, 0.05f, 1.5f);
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = selectedColor;
        _indicatorInstance.GetComponent<Renderer>().material = mat;
        Destroy(_indicatorInstance.GetComponent<Collider>());
        _indicatorInstance.SetActive(false);
    }
}
