using System;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.InputSystem;

public class SelectionManager : MonoBehaviour
{
    public static SelectionManager instance;
    private FlockManager _flockManager;
    
    private Camera _mainCamera;
    
    #region Current Selection Infos
    
    [Foldout("Current Selection Infos"), ReadOnly] public Unit.Unit mouseAboveThisUnit;
    [Foldout("Current Selection Infos"), ReadOnly] public List<Unit.Unit> currentlySelectedUnits;
    [ReadOnly, SerializeField,  Foldout("Current Selection Infos")] public FlockManager.Flock currentlySelectedFlock;
    #endregion

    [SerializeField] private LayerMask groundLayer;
    private void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
        }
        else instance = this;
        
        _mainCamera = Camera.main;
    }
    
   private void Start()
   { 
      _flockManager = FlockManager.instance;
   }
   
   public void OnLeftButtonClick()
   {
      if (currentlySelectedUnits.Count is 0) // Si aucune unité n'est sélectionné 
      {
         if (!mouseAboveThisUnit) return; // Return si la souris ne hover pas une unité
         SelectUnit(mouseAboveThisUnit);
      }
      else // Si au moins une unité est selectionné
      {
         if (!mouseAboveThisUnit) // Si la souris ne hover pas une unité
         {
            // Shoot un ray depuis la souris vers le monde 
            Vector3 mousePos = Mouse.current.position.ReadValue();
            mousePos.z = _mainCamera.nearClipPlane;
            var ray = _mainCamera.ScreenPointToRay(mousePos);

            // Set l'anchor de la flock selectionée à la position du hit du raycast
            RaycastHit hit2;
            if (Physics.Raycast(ray, out hit2, 1000, groundLayer))
            {
               var anchorPos = currentlySelectedFlock.anchor.transform.position;
               anchorPos = new Vector3(hit2.point.x, anchorPos.y, hit2.point.z);
               currentlySelectedFlock.anchor.MoveAnchorTo(anchorPos);
            }
         }
         else // Si la souris hover une unité
         {
            // Si le joueur click sur une unité de la même flock que l'unité selectionné
            if (mouseAboveThisUnit.myFlock == currentlySelectedFlock)
            {
               Debug.Log("Impossible de merge une flock avec elle-même");
               return;
            }

            // Si le joueur click sur une unité appartenant à une flock d'une faction différente de l'unité selectionné
            if (mouseAboveThisUnit.myFlock.faction != currentlySelectedFlock.faction)
            {
               // Move to Ennemy and ready to engage
               var tempPos = mouseAboveThisUnit.transform.position;
               var tempNewPos = new Vector3(tempPos.x, currentlySelectedFlock.anchor.transform.position.y, tempPos.z);
               currentlySelectedFlock.anchor.MoveAnchorTo(tempNewPos);
               foreach (var unit in currentlySelectedFlock.activeUnitsInFlocks) unit.isReadyToEngage = true;
            }
            else // Si le joueur click sur une unité dont la flock appartient à la même faction que la flock de l'unité sélectionné
            {
               // Merge with Ally
               _flockManager.MergeFlocks(currentlySelectedFlock, mouseAboveThisUnit.myFlock);
            }
         }
      }
   }

   private void SelectUnit(Unit.Unit unit)
   {
      currentlySelectedUnits.Add(unit); // Sélectionner l'unité
      currentlySelectedFlock = unit.myFlock; // Sélectionne la flock de l'unité
         
      foreach (var u in currentlySelectedFlock.activeUnitsInFlocks)
      {
         u.selectionCircle.SetActive(true);
      }
   }

   public void OnRightButtonClick() => UnSelectAll();

   private void UnSelectAll()
   {
      currentlySelectedUnits.Clear();

      if (currentlySelectedFlock is not null)
      { 
         foreach (var unit in currentlySelectedFlock.activeUnitsInFlocks)
         {
            unit.selectionCircle.SetActive(false);
         }
         
         currentlySelectedFlock = null;
      }
   }
}