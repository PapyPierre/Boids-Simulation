using UnityEngine;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
   public static GameManager instance;
   private FlockManager _flockManager;

   private Camera _mainCamera;
   
   [SerializeField] private LayerMask groundLayer;

   public Unit.Unit mouseAboveThisUnit;

   #region Debug
   public bool showGizmosOnSelectedUnitOnly = true;
   [Space]
   public bool showMaxDistFromAnchor;
   public bool showInfluenceOfOthersFlockUnits;
   public bool showUnitsVelocity;
   public bool showUnitsAnchorAttraction;
   public bool showUnitsPerceptionRange;
   public bool showUnitsEngagementRange;
   public bool showUnitdEngagedUnits;
   #endregion
   
   private void Awake()
   {
      if (instance != null)
      {
         Destroy(gameObject);
      }
      else instance = this;
      
      Application.targetFrameRate = 60;
      _mainCamera = Camera.main;
   }
   
   private void Start() => _flockManager = FlockManager.instance;

   public void OnSelectionOrOrder()
   {
      if (!_flockManager.currentlySelectedUnit) // Si aucune unité n'est sélectionné 
      {
         if (!mouseAboveThisUnit) return; // Return si la souris ne hover pas une unité
         _flockManager.currentlySelectedUnit = mouseAboveThisUnit; // Sélectionner l'unité
         _flockManager.currentlySelectedFlock =  mouseAboveThisUnit.myFlock; // Sélectionne la flock de l'unité
         
         foreach (var unit in _flockManager.currentlySelectedFlock.unitsInFlocks)
         {
           unit.selectionCircle.SetActive(true);
         }
      }
      else // Si une unité est selectionné
      {
         if (!mouseAboveThisUnit) // Si la souris ne hover pas une unité
         {
            // Shoot un ray depuis la souris vers le monde 
            Vector3 mousePos = Mouse.current.position.ReadValue();   
            mousePos.z = _mainCamera.nearClipPlane;
            var ray = _mainCamera.ScreenPointToRay(mousePos);
            
            // Set l'anchor de la flock selectionée à la position du hit du raycast
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit,1000, groundLayer))
            {
               var anchorPos = _flockManager.currentlySelectedFlock.anchor.transform.position;
               anchorPos = new Vector3(hit.point.x, anchorPos.y, hit.point.z);
               _flockManager.currentlySelectedFlock.anchor.MoveAnchorTo(anchorPos);
            }
         }
         else // Si la souris hover une unité
         {
            // Si le joueur click sur une unité de la même flock que l'unité selectionné
            if (mouseAboveThisUnit.myFlock == _flockManager.currentlySelectedUnit.myFlock)
            {
               Debug.Log("Impossible de merge une flock avec elle-même");
               return;
            }
            
            // Si le joueur click sur une unité appartenant à une flock d'une faction différente de l'unité selectionné
            if (mouseAboveThisUnit.myFlock.faction != _flockManager.currentlySelectedUnit.myFlock.faction)
            {
              // Move to Ennemy and ready to engage
              _flockManager.currentlySelectedFlock.anchor.MoveAnchorTo(mouseAboveThisUnit.transform.position);
              foreach (var unit in _flockManager.currentlySelectedFlock.unitsInFlocks) unit.isReadyToEngage = true;
            }
            else // Si le joueur click sur une unité dont la flock appartient à la même faction que la flock de l'unité sélectionné
            {        
               // Merge with Ally
               MergeFlocks(_flockManager.currentlySelectedFlock, mouseAboveThisUnit.myFlock);
            }
         }
      }
   }

   public void OnUnselection()
   {
      _flockManager.currentlySelectedUnit = null;

      if ( _flockManager.currentlySelectedFlock is not null)
      { 
         foreach (var unit in  _flockManager.currentlySelectedFlock.unitsInFlocks)
         {
            unit.selectionCircle.SetActive(false);
         }
         
         _flockManager.currentlySelectedFlock = null;
      }
   }

   private void MergeFlocks(FlockManager.Flock flockToMerge, FlockManager.Flock targetFlock)
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
      _flockManager.allFlocks.Remove(flockToMerge);
      
      _flockManager.currentlySelectedFlock = targetFlock;
   }
}