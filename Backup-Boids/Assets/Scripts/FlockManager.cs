using System;
using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
using Unit;
using UnityEngine;
using Random = UnityEngine.Random;

public class FlockManager : MonoBehaviour 
{
    public static FlockManager instance;

    #region Current Selection Infos
    
    [Foldout("Current Selection Infos")]
    public Unit.Unit currentlySelectedUnit;
    [ReadOnly, SerializeField,  Foldout("Current Selection Infos")] public Flock currentlySelectedFlock;
    #endregion

    #region Flocks informations
    
    [SerializeField, Tooltip("Temps en secondes entre chaque mise à jour d'états des flocks," +
                             " plus le delais est petit, plus les performance seront impacté")] 
    private float flocksStateUpdateTime;
    private bool _isUpdatingFlocksStates;

    [Serializable]
    public class Flock
    {
        public FlockFaction faction;
        private FlockState _currentState;

        public FlockState CurrentState
        {
            get => _currentState;

            set
            {
                _currentState = value;

                foreach (var unit in unitsInFlocks)
                {
                    unit.currentSpeed = value switch
                    {                    
                        FlockState.Stationnaire => unit.data.normalSpeed,
                        FlockState.EnDéplacement => unit.data.normalSpeed,
                        FlockState.EnCombat => unit.data.attackSpeed,
                        _ => throw new ArgumentOutOfRangeException(nameof(value))
                    };
                }
            }
        }

        [Tooltip("Point d'encrage d'une flock, les unités vont essayer de rester proche")] public FlockAnchor anchor;
        public Color flockColor;
        public List<Unit.Unit> unitsInFlocks = new();
    }

    public enum FlockState
    {
        EnDéplacement,
        EnCombat,
        Stationnaire,
    }

    public enum FlockFaction
    {
        BlueFaction,
        RedFaction,
        GreenFaction
    }
    
    // Information pour les nouvelles flocks
    [Space, SerializeField, Foldout("New Flock Info")] private int unitCountInNewFlock;
    [SerializeField, Foldout("New Flock Info")] private UnitData.UnitType newFlockType;
    [SerializeField, Foldout("New Flock Info")] private FlockFaction newFlockFaction;
    #endregion

    #region Default Variables
    
    [Space, Tooltip("Distance de perception par défaut des autres unités," + 
                    " peut être override dans les paramètre de chaque unité"),  Foldout("Default Variables")] 
    public float defaultUnitPerceptionRadius = 3;

    [Tooltip("Distance max par défaut entre cette unité et son ancre, " +
             "peut être override dans les paramètre de chaque unité"),  Foldout("Default Variables")] 
    public float defaultUnitMaxDistFromAnchor = 20;

    [SerializeField,  Foldout("Default Variables")] private Material defaultUnitMat;
    [Foldout("Default Variables")] public Material unitOutlineMat;
    [Foldout("Default Variables")] public Material defaultAnchorMat;

    [SerializeField,  Foldout("Default Variables")] private GameObject defaultAerianUnit;
    [SerializeField,  Foldout("Default Variables")] private GameObject defaultTerrestUnit;
    [SerializeField,  Foldout("Default Variables")] private int indexOfDefaultUndergroundUnit;
    
    [SerializeField,  Foldout("Default Variables"), Range(0,100)] private float percentageOfUnitInAnchorToBeStationnary;
    #endregion
    
    #region All Flocks And Units

    [SerializeField] private bool showAllFlocksAndUnitsInInspector; // Used only for inspector purpose
    
    [ShowIf("showAllFlocksAndUnitsInInspector")] public List<Flock> allFlocks;
    [ShowIf("showAllFlocksAndUnitsInInspector")] public List<Unit.Unit> allUnits;
    #endregion
    
    #region Prefabs

    [SerializeField, Foldout("Prefabs")] private GameObject[] unitPrefabs;
    [SerializeField, Foldout("Prefabs"), Required()] private GameObject flockAnchorPrefab;
    #endregion
    
    #region Weights
    
    [Space, Foldout("Weights")] public bool useWeights;
    [Space, Tooltip("Force pour s'éloigner des autres unités"), Foldout("Weights")]
    public float separationWeight;
    [Tooltip("Force pour se raprocher du centre des positions des autres unités"), Foldout("Weights")]
    public float cohesionWeight;
    [Tooltip("Force pour que les unités aille dans la même direction"), Foldout("Weights")]
    public float alignmentWeight;
    [Tooltip("Force pour rester proche de l'ancre de sa flock"), Foldout("Weights")] 
    public float anchorWeight;
    [Tooltip("Multiplicateur de anchorWeight lorsque l'unité est en dehors des limites de l'ancre"), Foldout("Weights")] 
    public float anchorWeightMultiplicator;
    #endregion

    private void Awake() 
    {
        if (instance != null)
        {
            Destroy(gameObject);
        }
        else instance = this;
        
        allUnits.Clear();
        allFlocks.Clear();
    }

    [Button()]
    public void InstantiateNewFlock()
    {
        if (!Application.isPlaying)
        {
            Debug.LogError("Must be in play mode to call this function");
            return;
        }
        
        Flock newFlock = new Flock();
        newFlock.faction = newFlockFaction;
        
        GameObject anchor = Instantiate(flockAnchorPrefab, new Vector3(0, 20, 0), Quaternion.identity);

        newFlock.anchor = anchor.GetComponent<FlockAnchor>();
        newFlock.CurrentState = FlockState.Stationnaire;

        newFlock.flockColor = newFlockFaction switch
        {       
            FlockFaction.BlueFaction => new Color(0, 0, Random.Range(0.2f, 1f)),
            FlockFaction.RedFaction => new Color(Random.Range(0.2f, 1f), 0, 0),
            FlockFaction.GreenFaction => new Color(0, Random.Range(0.2f, 1f), 0),
            _ => newFlock.flockColor
        };
        
        newFlock.anchor.SetUpMeshRenderer(newFlock);    
        newFlock.anchor.SetUpLineRender();

        allFlocks.Add(newFlock);

        if (newFlockType is UnitData.UnitType.Aérienne)
        {
            for (int i = 0; i < unitCountInNewFlock; i++)
            {
                Vector3 pos = PedroHelpers.GenerateRandomPosInCube(newFlock.anchor.transform.position, defaultUnitMaxDistFromAnchor);
                SetUpUnit(pos, defaultAerianUnit, newFlock);
            }
        }
        else if (newFlockType is UnitData.UnitType.Terrestre) 
        {
            for (int i = 0; i < unitCountInNewFlock; i++)
            {
                Vector3 pos = new Vector3(Random.Range(-defaultUnitMaxDistFromAnchor, defaultUnitMaxDistFromAnchor),
                    1, Random.Range(-defaultUnitMaxDistFromAnchor, defaultUnitMaxDistFromAnchor));
                
                SetUpUnit(pos, defaultTerrestUnit, newFlock);
            }
        }
        
        if (!_isUpdatingFlocksStates) StartCoroutine(UpdateFlocksStates());
    }

    private void SetUpUnit(Vector3 spawnPos, GameObject unitPrefab, Flock newFlock)
    {
        Quaternion rot = Quaternion.identity;
        GameObject newUnitGO = Instantiate(unitPrefab, spawnPos, rot);
        Unit.Unit newUnit = newUnitGO.GetComponent<Unit.Unit>();
        newUnit.myFlock = newFlock;
        allUnits.Add(newUnit);
        newFlock.unitsInFlocks.Add(newUnit);
    }
    
    // Recurvise Update of all flocks states
    private IEnumerator UpdateFlocksStates()
    {
        _isUpdatingFlocksStates = true;

        foreach (var flock in allFlocks)
        {
            var nbOfUnitOutOfBound = 0;
            foreach (var unit in flock.unitsInFlocks)
            {
                if (unit.isOutOfBoundOfAnchor) nbOfUnitOutOfBound++;
            }

            if (nbOfUnitOutOfBound > (1 - percentageOfUnitInAnchorToBeStationnary / 100) * flock.unitsInFlocks.Count)
            {
                flock.CurrentState = FlockState.EnDéplacement;
            }
            else flock.CurrentState = FlockState.Stationnaire;
            
            foreach (var unit in flock.unitsInFlocks)
            {  
                if (flock.CurrentState is FlockState.Stationnaire) 
                { 
                    if (unit.data.unitType is UnitData.UnitType.Terrestre) 
                    {
                        if (!unit.isOutOfBoundOfAnchor) unit.isInIdle = true;
                    } 
                }
                else unit.isInIdle = false;
            }
        }

        yield return new WaitForSeconds(flocksStateUpdateTime);
        StartCoroutine(UpdateFlocksStates());
    }
    
    private void OnDrawGizmos() 
    {     
        if (!Application.isPlaying) return;
        Gizmos.color = Color.red;
        if (currentlySelectedUnit == null || currentlySelectedFlock == null || !GameManager.instance.showMaxDistFromAnchor) return;
        if (allFlocks.Count > 0)
        {
            Gizmos.DrawWireSphere(currentlySelectedFlock.anchor.transform.position, defaultUnitMaxDistFromAnchor);
        }
    }
}