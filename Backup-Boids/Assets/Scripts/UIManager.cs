using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    private FlockManager _flockManager;
    
    [SerializeField] private GameObject debugPanel;
   
    [SerializeField] private TextMeshProUGUI fpsTMP;
    [SerializeField] private TextMeshProUGUI nbFlocksTMP;
    [SerializeField] private TextMeshProUGUI nbUnitsTMP;

    private bool _showDebugPanel;
    private void Start() => _flockManager = FlockManager.instance;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F3))
        {
            _showDebugPanel = !_showDebugPanel;
            debugPanel.SetActive(_showDebugPanel);
        }
        
        if (_showDebugPanel)
        {       
            fpsTMP.text = "FPS: " + Mathf.RoundToInt(1 / Time.unscaledDeltaTime);
            nbFlocksTMP.text = "Flocks number: " + _flockManager.allFlocks.Count;
            nbUnitsTMP.text = "Units number: " + _flockManager.allUnits.Count;
        }
    }
}
