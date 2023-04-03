using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    private FlockManager _flockManager;
    
    [SerializeField] private GameObject debugPanel;
   
    [SerializeField] private TextMeshProUGUI fpsTMP;
    [SerializeField] private TextMeshProUGUI nbFlocksTMP;
    [SerializeField] private TextMeshProUGUI nbUnitsTMP;
    private void Start() => _flockManager = FlockManager.instance;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F3)) debugPanel.SetActive(!debugPanel.activeSelf);

        fpsTMP.text = "FPS: " + Mathf.RoundToInt(1 / Time.unscaledDeltaTime);
        nbFlocksTMP.text = "Flocks number: " + _flockManager.allFlocks.Count;
        nbUnitsTMP.text = "Units number: " + _flockManager.allUnits.Count;
    }
}
