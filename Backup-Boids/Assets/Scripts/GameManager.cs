using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
   public static GameManager instance;
   private FlockManager _flockManager;
   
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
   }

   private void Start()
   {
      _flockManager = FlockManager.instance;
   }

   private void Update()
   {
      if (Input.GetKeyDown(KeyCode.F2))
      {
         foreach (var unit in _flockManager.allUnits) unit.gameObject.SetActive(false);
         foreach (var flock in  _flockManager.allFlocks) flock.anchor.gameObject.SetActive(false);
         
         _flockManager.allUnits.Clear();
         _flockManager.allFlocks.Clear();
      }
   }
}