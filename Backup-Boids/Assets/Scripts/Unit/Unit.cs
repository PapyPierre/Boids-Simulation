using System;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

namespace Unit
{
    public class Unit : MonoBehaviour
    {
        private GameManager _gameManager;
        private FlockManager _flockManager;
        
        [Expandable] public UnitData data;

        [ReadOnly] public FlockManager.Flock myFlock;
        public float currentHealthPoint;
        
        [SerializeField] private GameObject prefabBlue; 
        [SerializeField] private GameObject prefabRed;
        [SerializeField] private GameObject prefabGreen;
        public GameObject selectionCircle;
        
        [SerializeField] private Animator animator;
        private bool _isanimatorNotNull;

        public bool isOutOfBoundOfAnchor;
        public bool isInIdle;
        public float currentSpeed;

        public bool isReadyToEngage;
        private List<Unit> _unitsInRangeOfEngagement = new ();
        [SerializeField] private Unit _engagedUnit;

        #region private variables for calculus
        private Vector3 _separationForce;
        private Vector3 _cohesionForce;
        private Vector3 _alignmentForce;

        private Vector3 _returnToAnchorForce;
        private Vector3 _velocity;
        private Vector3 _force;
        #endregion

        private void Awake()
        {
            _isanimatorNotNull = animator != null;
            currentHealthPoint = data.maxHealthPoint;
            if (data.canAutoEngage) isReadyToEngage = true;
        } 

        private void Start()
        {
            _gameManager = GameManager.instance;
            _flockManager = FlockManager.instance;

            currentSpeed = data.normalSpeed;

            switch (myFlock.faction)
            {
                case FlockManager.FlockFaction.BlueFaction:
                    prefabBlue.SetActive(true);
                    break;
                case FlockManager.FlockFaction.RedFaction:
                    prefabRed.SetActive(true);
                    break;
                case FlockManager.FlockFaction.GreenFaction:
                    prefabGreen.SetActive(true);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void Update() 
        {
            CalculateForces();
            MoveForward();
            ManageEngagement();
        }

        #region Movement Behaviour
        private void CalculateForces() 
        {
            Vector3 seperationSum = Vector3.zero;
            Vector3 positionSum = Vector3.zero;
            Vector3 headingSum = Vector3.zero;

            int unitsNearby = 0;

            foreach (var unit in _flockManager.allUnits)
            {
                if (this == unit) continue;
                
                Vector3 otherUnitPosition = unit.transform.position;
                float distToOtherUnit = Vector3.Distance(transform.position, otherUnitPosition);
                
                if (unit.myFlock == myFlock) // Unité de la même flock
                {
                    if (data.unitPerceptionRadius > 0)
                    {
                        if (!(distToOtherUnit < data.unitPerceptionRadius)) continue;
                    }
                    else
                    {
                        // Prends le perceptionRadius par defaut si = à 0
                        if (!(distToOtherUnit < _flockManager.defaultUnitPerceptionRadius)) continue;
                    }

                    if (_gameManager.showInfluenceOfOthersFlockUnits)
                    {
                        if (_gameManager.showGizmosOnSelectedUnitOnly)
                        {
                            if (_flockManager.currentlySelectedUnit == this)
                            {
                                Debug.DrawLine(transform.position, unit.transform.position, Color.magenta);
                            }
                        }
                        else Debug.DrawLine(transform.position, unit.transform.position, Color.magenta);
                    }
                    
                    seperationSum += -(otherUnitPosition - transform.position) * (1f / Mathf.Max(distToOtherUnit, .0001f));
                    positionSum += otherUnitPosition;
                    headingSum += unit.transform.forward;

                    unitsNearby++;
                }
                else  // Unité d'une autre flock
                {
                    if (unit.myFlock.faction != myFlock.faction) // Unité ennemies
                    {
                        if (distToOtherUnit < data.engagementDist) _unitsInRangeOfEngagement.Add(unit);
                    }
                }
            }

            if (unitsNearby > 0) 
            {
                _separationForce = seperationSum / unitsNearby;
                _cohesionForce   = (positionSum / unitsNearby) - transform.position;
                _alignmentForce  = headingSum / unitsNearby;
            }
            else 
            {
                _separationForce = Vector3.zero;
                _cohesionForce   = Vector3.zero;
                _alignmentForce  = Vector3.zero;
            }
            
            if (data.unitMaxDistFromAnchor == 0) GoBackToAnchor(_flockManager.defaultUnitMaxDistFromAnchor);
            else GoBackToAnchor(data.unitMaxDistFromAnchor);
        }

        private void GoBackToAnchor(float maxDistFromAnchor)
        {
            var dir = myFlock.anchor.transform.position - transform.position;
            _returnToAnchorForce = dir.normalized;
            
            // If too far from anchor, apply multiplicator to go back to the anchor
            float distToAnchor;

            if (data.unitType is UnitData.UnitType.Aérienne)
            {
                distToAnchor = Vector3.Distance(transform.position, myFlock.anchor.transform.position);
            }
            else if (data.unitType is UnitData.UnitType.Terrestre)
            {
                Vector3 anchorPosOnGround = new Vector3(myFlock.anchor.transform.position.x, 1,
                    myFlock.anchor.transform.position.z);
                
                distToAnchor = Vector3.Distance(transform.position, anchorPosOnGround);
            }
            else // Default, shoudn't be used
            {
                distToAnchor = Vector3.Distance(transform.position, myFlock.anchor.transform.position);
            }
            
            if (distToAnchor > maxDistFromAnchor)
            {
                isOutOfBoundOfAnchor = true;
                _returnToAnchorForce *= _flockManager.anchorWeightMultiplicator;
            }
            else isOutOfBoundOfAnchor = false;
        }
        
        private void MoveForward()
        {
            if (isInIdle)
            {
                if (_isanimatorNotNull) animator.SetBool("isMoving", false);
                return;
            }
            
            _force = _flockManager.useWeights switch
            {
                true => _separationForce * _flockManager.separationWeight + _cohesionForce * _flockManager.cohesionWeight +
                        _alignmentForce * _flockManager.alignmentWeight + _returnToAnchorForce * _flockManager.anchorWeight,
                false => _separationForce + _cohesionForce + _alignmentForce + _returnToAnchorForce
            };

            var correctedForce = _force;
            if (data.unitType is UnitData.UnitType.Terrestre) correctedForce = new Vector3(_force.x, 0, _force.z);

            if (isOutOfBoundOfAnchor)
            { 
                _velocity = transform.forward * data.renforcementSpeed + correctedForce * Time.deltaTime;
                _velocity = _velocity.normalized * data.renforcementSpeed;
                if (_isanimatorNotNull) animator.SetBool("isMoving", true);
            }
            else
            {
                {
                    _velocity = transform.forward * currentSpeed + correctedForce * Time.deltaTime;
                    _velocity = _velocity.normalized * currentSpeed;
                    if (_isanimatorNotNull) animator.SetBool("isMoving", true);
                }
            }
            
            transform.position += _velocity * Time.deltaTime;
            transform.rotation = Quaternion.LookRotation(_velocity);
        }
        #endregion

        #region Combat Behaviour
        
        List<Unit> preSelectedEngagementTarget = new();
        private int _dataForTargeting;

        private void ManageEngagement()
        {
            if (_unitsInRangeOfEngagement.Count is 0 || !isReadyToEngage || _engagedUnit is not null) return;

            if (_unitsInRangeOfEngagement.Count is 1)
            {
                _engagedUnit = _unitsInRangeOfEngagement[0];
                return;
            }

            if (dertermineNewTarget)
            {
                DetermineTarget(); // Parmis celle à porté
                dertermineNewTarget = false;
                
            }
        }

        private bool dertermineNewTarget = true;

        private int _indexOfCurrentTargetingPriority;

        private void DetermineTarget()
        {
            if (_engagedUnit is not null) return; // Si une unité est déja engagé, return
                
            if (preSelectedEngagementTarget.Count is 0) LookForTargetInGivenList(_unitsInRangeOfEngagement, _indexOfCurrentTargetingPriority);
            else LookForTargetInGivenList(preSelectedEngagementTarget, _indexOfCurrentTargetingPriority);
        }

        private void LookForTargetInGivenList(List<Unit> unitsTargetable, int i)
        {
            preSelectedEngagementTarget.Clear();

            if (i >= data.targetingPriority.Count) // Si il reste + que 1 untié après avoir passer touts les priorité de ciblage
            {
                Unit closestUnit = null;
                float distToClosestUnit = 0;

                foreach (var unit in unitsTargetable)
                {
                    float distToUnit = Vector3.Distance(transform.position, unit.transform.position);

                    if (closestUnit is null)
                    {
                        distToClosestUnit = distToUnit;
                        closestUnit = unit;
                        continue;
                    }
                   
                    if (distToUnit < distToClosestUnit)
                    {
                        distToClosestUnit = distToUnit;
                        closestUnit = unit;
                    }
                }

                _engagedUnit = closestUnit;
                return;
            }
            
            foreach (var unit in unitsTargetable)
            {
                switch (SelectTargetingPrioWithGivenIndex(i))
                {
                    case 1: // L'élèment est un type d'unité
                        if (unit.data.unitId == _dataForTargeting) preSelectedEngagementTarget.Add(unit);
                        break;
                    
                    case 2: // L'élèment est une distance min
                        
                        float distToUnit = Vector3.Distance(transform.position, unit.transform.position);
                        
                        if (distToUnit < _dataForTargeting) preSelectedEngagementTarget.Add(unit);
                        break;
                    
                    case 3: // l'élèment est un seuil de pv

                        if (unit.currentHealthPoint < _dataForTargeting) preSelectedEngagementTarget.Add(unit);
                        break;
                    case 0:
                        throw new ArgumentException();
                }
            }

            if (preSelectedEngagementTarget.Count > 1)
            {
                _indexOfCurrentTargetingPriority++;
                DetermineTarget();   
            }
            else _engagedUnit = preSelectedEngagementTarget[0];
        }

        private int SelectTargetingPrioWithGivenIndex(int index)
        {
            if (data.targetingPriority[index].unitData is not null) // Si l'élèment est un type d'unité
            {
                _dataForTargeting = data.targetingPriority[index].unitData.unitId;
                return 1;
            }
            else if (data.targetingPriority[index].minDist > 0) // Si l'élèment est une distance min
            {
                _dataForTargeting = data.targetingPriority[index].minDist;
                return 2;
            }
            else if (data.targetingPriority[index].hpThreshold > 0) // Si l'élèment est un seuil de pv
            {
                _dataForTargeting = data.targetingPriority[index].hpThreshold;
                return 3;
            }
            else return 0;
        }
        
        #endregion

        #region Selection
        private void OnMouseEnter()
        {
            _gameManager.mouseAboveThisUnit = this;
            selectionCircle.SetActive(true);
        }

        private void OnMouseExit()
        { 
            _gameManager.mouseAboveThisUnit = null;
            if (_flockManager.currentlySelectedFlock != myFlock) selectionCircle.SetActive(false);
        } 
        
        #endregion

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || !_flockManager) return;
            if (_gameManager.showGizmosOnSelectedUnitOnly)
            {
                if (!_flockManager.currentlySelectedUnit) return;
                if (_flockManager.currentlySelectedUnit == this) ShowGizmos();
            }
            else ShowGizmos();
        }

        private void ShowGizmos()
        {
              // Indique la vélocité de l'unité
                if (_gameManager.showUnitsVelocity) Gizmos.DrawRay(transform.position, _velocity / 2);
                
                // Indique la force d'atraction vers l'ancre
                Gizmos.color = Color.yellow;
                if (_gameManager.showUnitsAnchorAttraction)
                {
                    Gizmos.DrawRay(transform.position, _returnToAnchorForce * _flockManager.anchorWeight);
                }
                
                // Indique la range de perception de l'unité
                Gizmos.color = Color.green;
                if (_gameManager.showUnitsPerceptionRange)
                {
                    if (data.unitPerceptionRadius > 0) Gizmos.DrawWireSphere(transform.position, data.unitPerceptionRadius);
                    else Gizmos.DrawWireSphere(transform.position, _flockManager.defaultUnitPerceptionRadius);
                }

                // Indique la force d'atraction vers l'ancre (ligne en vert = dans la range de l'ance, rouge = out of bound)
                if (_gameManager.showUnitsAnchorAttraction)
                {
                    if (data.unitMaxDistFromAnchor > 0)
                    {
                        if (Vector3.Distance(transform.position,
                                myFlock.anchor.transform.position) 
                            > data.unitMaxDistFromAnchor)
                        {
                            Gizmos.color = Color.red;
                            Gizmos.DrawRay(transform.position, _returnToAnchorForce * _flockManager.anchorWeight);
                        }
                        else
                        {
                            Gizmos.color = Color.green;
                            Gizmos.DrawRay(transform.position, _returnToAnchorForce * _flockManager.anchorWeight);
                        }
                    }
                    else
                    {
                        if (Vector3.Distance(transform.position, myFlock.anchor.transform.position) 
                            > _flockManager.defaultUnitMaxDistFromAnchor)
                        {
                            Gizmos.color = Color.red;
                            Gizmos.DrawRay(transform.position, _returnToAnchorForce * _flockManager.anchorWeight);
                        }
                        else
                        {
                            Gizmos.color = Color.green;
                            Gizmos.DrawRay(transform.position, _returnToAnchorForce * _flockManager.anchorWeight);
                        }
                    }
                }
                
                // Indique la proté d'engagement de l'unité
                Gizmos.color = Color.red;
                if (_gameManager.showUnitsEngagementRange) Gizmos.DrawWireSphere(transform.position, data.engagementDist);

                // Indique l'unité ciblé par cette unité
                if (_engagedUnit != null && _gameManager.showUnitdEngagedUnits)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawLine(transform.position,_engagedUnit.transform.position);
                }
        }
    }
}