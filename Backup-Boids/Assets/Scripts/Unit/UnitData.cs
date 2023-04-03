using System;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

namespace Unit
{
    [CreateAssetMenu(fileName = "UnitData", menuName = "Unit/UnitData", order = 1)]
    public class UnitData : ScriptableObject
    {
        // ---------------------------------- Module d'Identité -------------------------------------------

        #region Module d'Identité
        [Foldout("Module Identité")] public int unitId;
        [Foldout("Module Identité")]public string unitName;

        [Foldout("Module Identité")]public int maxHealthPoint;
        #endregion
        
        // ---------------------------------- Module de Déplacement ----------------------------------------

        #region Module de Déplacement
        [Foldout("Module de déplacement")] public UnitType unitType;
  
        public enum UnitType
        {
            Aérienne,
            Terrestre,
            //TODO Souterrainne
        }

        [Tooltip("Speed by default"), Foldout("Module de déplacement")] public float normalSpeed;
        [Tooltip("Speed during combat"), Foldout("Module de déplacement")] public float attackSpeed;
        [Tooltip("Speed when out of bound of the anchor"), Foldout("Module de déplacement")] 
        public float renforcementSpeed;
        

        #endregion

        // ---------------------------------- Module de Comportement en Flock ----------------------------------------

        #region Module de Comportement en Flock
        [Space, Tooltip("Distance de perception des autres unités"), Foldout("Module de Comportement en Flock")] 
        public float unitPerceptionRadius;
        [Tooltip("Distance max entre cette unité et son ancre"), Foldout("Module de Comportement en Flock")] 
        public float unitMaxDistFromAnchor;
        
        [SerializeField] private List<UnitData> anchoringList;

        [Tooltip("Hors Combat"), SerializeField] private float minDistFromOtherUnitInFlock;
        [Tooltip("Hors Combat"), SerializeField] private float maxDistFromOtherUnitInFlock;
        #endregion

        // ---------------------------------- Module de Comportement en Combat ----------------------------------------

        #region Module de Comportement en Combat
        [Foldout("Module de Comportement en Combat")] public bool canAutoEngage;
        [Foldout("Module de Comportement en Combat")] public int engagementDist;
        [SerializeField, Foldout("Module de Comportement en Combat")] private bool canDesengage;

        [SerializeField, Foldout("Module de Comportement en Combat")] private List<DesengagementTrigger> desengagementTriggers;

        public enum DesengagementTrigger
        {
            ByDistance, // Si Distance avec anchor > desengamentDist
            ByHealthPoint // Si currentHealthPoint < dammageThreshold
        }

        [SerializeField, Foldout("Module de Comportement en Combat")] private int desengamentDist;
        [SerializeField, Foldout("Module de Comportement en Combat")] private int dammageThreshold;
        
        [SerializeField, Foldout("Module de Comportement en Combat")] private PossibleDesengamentSpeed desengamentSpeed;
        
        enum PossibleDesengamentSpeed
        {
            NormalSpeed,
            AttackSpeed,
            RenforcementSpeed
        }

        [Space, SerializeField, Foldout("Module de Comportement en Combat")] private bool canProtect;
        
        [Foldout("Module de Comportement en Combat")] public List<UnitData> protectionUnitList;
        #endregion

        // ---------------------------------- Module Ciblage ---------------------------------------------

        #region Module Ciblage
        [Tooltip("Si une donnée est null ou à 0, elle n'est pas pris en compte"), Foldout("Module Ciblage")] 
        public List<TargetingPrioOptions> targetingPriority;

        [Serializable]
        public class TargetingPrioOptions
        {
            [HideIf(EConditionOperator.Or, "ShowMinDist", "ShowHp"), AllowNesting] public UnitData unitData;
            [HideIf(EConditionOperator.Or, "ShowUnit", "ShowHp"), AllowNesting] public int minDist;
            [HideIf(EConditionOperator.Or, "ShowUnit", "ShowMinDist"), AllowNesting] public int hpThreshold;

            private bool ShowUnit() { return unitData != null; }
            private bool ShowMinDist() { return minDist > 0; }
            private bool ShowHp() { return hpThreshold > 0; }
        }

        [SerializeField, Foldout("Module Ciblage")] private TargetingModes targetingSeverity;

        enum TargetingModes
        {
            JusquAuBout,
            APorté,
            Prio
        }
        #endregion
        
        // ---------------------------------- Module Armement ---------------------------------------------

        #region Module Armement
        [Tooltip("Les dégats infligé par cette unité seront compris entre ces valeurs"),
         MinMaxSlider(0.0f, 100.0f), SerializeField, Foldout("Module Armement")] private Vector2Int damageRange;
        
        [Tooltip("Cette unité pourra tiré sur une autre unité situé à une distance comprise entre ces valeurs"),
         MinMaxSlider(0.0f, 100.0f), SerializeField, Foldout("Module Armement")] private Vector2Int shootingDistanceRange;
        
        [SerializeField, Foldout("Module Armement")] private float delayBetweenShots; // En ms
        [SerializeField, Foldout("Module Armement")] private bool canShootUndergroundUnits;
        #endregion
        
        // ---------------------------------- Module Physique & Apparence ---------------------------------------------

        #region Module Physique & Apparence
        [Tooltip("Détermine si l’unité peut passer au travers du décors ou non"), 
         SerializeField, Foldout("Module Physique & Apparence")] private bool isTangible;


        [SerializeField, Foldout("Module Physique & Apparence")] private int mass; // A voir si je le fait ptdr
        #endregion

        //  Module Particules
        //  Module Sons
        //  Module Capacité
    }
}