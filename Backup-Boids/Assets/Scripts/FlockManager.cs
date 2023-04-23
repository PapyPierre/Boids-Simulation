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
    private SelectionManager _selectionManager;
    
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
    
    [ShowIf("showAllFlocksAndUnitsInInspector"), ReadOnly] public List<Flock> allFlocks;
    [ShowIf("showAllFlocksAndUnitsInInspector"), ReadOnly] public List<Unit.Unit> allActiveUnits;
    #endregion

    #region Required GameObjects

    [Foldout("Required GameObjects"), Required()] public Transform blueBase;
    [Foldout("Required GameObjects"), Required()] public Transform redBase;
    [Foldout("Required GameObjects"), Required()] public Transform greenBase;

    [SerializeField, Foldout("Required GameObjects"), Required(), Space] private GameObject flockAnchorPrefab;
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
    [Tooltip("Multiplicateur de anchorWeight lorsque l'unité est en dehors des limites de l'ancre"), 
     Foldout("Weights"), Range(0,10)] public float anchorWeightMultiplicator;
    [Tooltip("Force pour retourner à la base de sa faction"), Foldout("Weights")] 
    public float baseWeight;
    #endregion

    private void Awake() 
    {
        if (instance != null)
        {
            Destroy(gameObject);
        }
        else instance = this;
        
        allActiveUnits.Clear();
        allFlocks.Clear();
    }

    private void Start()
    {
        _selectionManager = SelectionManager.instance;
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

        Vector3 newAnchorPos = newFlockFaction switch
        {
            FlockFaction.BlueFaction => new Vector3(blueBase.position.x, 20, blueBase.position.z),
            FlockFaction.RedFaction => new Vector3(redBase.position.x, 20, redBase.position.z),
            FlockFaction.GreenFaction => new Vector3(greenBase.position.x, 20, greenBase.position.z),
            _ => throw new ArgumentOutOfRangeException()
        };
        
        GameObject anchor = Instantiate(flockAnchorPrefab, newAnchorPos, Quaternion.identity);

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
                Vector3 pos = PedroHelpers.GenerateRandomPosInCube(newFlock.anchor.transform.position, defaultUnitMaxDistFromAnchor);
                Vector3 correctedPos = new Vector3(pos.x, 1, pos.z);
                SetUpUnit(correctedPos, defaultTerrestUnit, newFlock);
            }
        }
        
        if (!_isUpdatingFlocksStates) StartCoroutine(TickUpdateFlocksStates());
    }

    private void SetUpUnit(Vector3 spawnPos, GameObject unitPrefab, Flock newFlock)
    {
        Quaternion rot = Quaternion.identity;
        GameObject newUnitGO = Instantiate(unitPrefab, spawnPos, rot);
        Unit.Unit newUnit = newUnitGO.GetComponent<Unit.Unit>();
        newUnit.myFlock = newFlock;
        newUnit.myBase = newFlock.faction switch
        {
            FlockFaction.BlueFaction => blueBase,
            FlockFaction.RedFaction => redBase,
            FlockFaction.GreenFaction => greenBase,
            _ => throw new ArgumentOutOfRangeException(nameof(newFlock.faction))
        };
        allActiveUnits.Add(newUnit);
        newFlock.unitsInFlocks.Add(newUnit);
    }
    
    // Recurvise Update of all flocks states
    private IEnumerator TickUpdateFlocksStates()
    {
        _isUpdatingFlocksStates = true;

        foreach (var flock in allFlocks)
        {
            var nbOfUnitOutOfBound = 0;
            foreach (var unit in flock.unitsInFlocks)
            {
                unit.TickUpdate();
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
        StartCoroutine(TickUpdateFlocksStates());
    }
    
    public void MergeFlocks(Flock flockToMerge, Flock targetFlock)
    {
        foreach (var unit in targetFlock.unitsInFlocks)
        {
            unit.selectionCircle.SetActive(true);
        }
      
        foreach (var unit in flockToMerge.unitsInFlocks)
        {
            unit.myFlock = targetFlock;
            targetFlock.unitsInFlocks.Add(unit);
        }
      
        flockToMerge.anchor.gameObject.SetActive(false);
        allFlocks.Remove(flockToMerge);
      
       _selectionManager.currentlySelectedFlock = targetFlock;
    }

    private void OnDrawGizmos() 
    {     
        if (!Application.isPlaying) return;
        Gizmos.color = Color.red;
        if ( _selectionManager.currentlySelectedUnits == null ||  _selectionManager.currentlySelectedFlock == null ||
             !GameManager.instance.showMaxDistFromAnchor) return;
        if (allFlocks.Count > 0)
        {
            if ( _selectionManager.currentlySelectedFlock.unitsInFlocks[0].data.unitMaxDistFromAnchor > 0)
            {
                Gizmos.DrawWireSphere( _selectionManager.currentlySelectedFlock.anchor.transform.position, 
                    _selectionManager.currentlySelectedFlock.unitsInFlocks[0].data.unitMaxDistFromAnchor);
            }
            else Gizmos.DrawWireSphere( _selectionManager.currentlySelectedFlock.anchor.transform.position, 
                defaultUnitMaxDistFromAnchor);
        }
    }
}