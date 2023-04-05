using UnityEngine;

public class GameManager : MonoBehaviour
{
   public static GameManager instance;
   
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
}