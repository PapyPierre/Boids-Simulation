using System;
using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace Unit
{
    public class Unit : MonoBehaviour
    {
        private GameManager _gameManager;
        private FlockManager _flockManager;
        private SelectionManager _selectionManager;

        #region public and serialised variables
        [Expandable] public UnitData data;

        [ReadOnly] public FlockManager.Flock myFlock;
        public Transform myBase;
        public float currentHealthPoint;

        [SerializeField] private GameObject prefabBlue; 
        [SerializeField] private GameObject prefabRed;
        [SerializeField] private GameObject prefabGreen;
        public GameObject selectionCircle;
        
        [SerializeField] private Animator animator;
        private bool _isAnimatorNotNull;

        public bool isOutOfBoundOfAnchor;
        public bool isInIdle;
        public float currentSpeed;

        public bool isReadyToEngage;
        private List<Unit> _unitsInRangeOfEngagement = new ();
        public Unit engagedUnit;
        public Unit agressor;

        private bool _isDesengaged;

        private bool _isShooting;

        private bool _isDead;

        [SerializeField, Required()] private Slider lifeBarSlider;
        #endregion
        
        #region private variables for calculus
        private Vector3 _separationForce;
        private Vector3 _cohesionForce;
        private Vector3 _alignmentForce;
        
        private float _distToMyAnchor;

        private Vector3 _returnToAnchorForce;
        private Vector3 _velocity;
        private Vector3 _force;
        #endregion

        private void Awake()
        {
            currentHealthPoint = data.maxHealthPoint;
            if (data.canAutoEngage) isReadyToEngage = true;
        } 

        private void Start()
        {
            _gameManager = GameManager.instance;
            _flockManager = FlockManager.instance;
            _selectionManager = SelectionManager.instance;

            currentSpeed = data.normalSpeed;

            switch (myFlock.faction)
            {
                case FlockManager.FlockFaction.BlueFaction: prefabBlue.SetActive(true);
                    break;
                case FlockManager.FlockFaction.RedFaction: prefabRed.SetActive(true);
                    break;
                case FlockManager.FlockFaction.GreenFaction: prefabGreen.SetActive(true);
                    break;
                default: throw new ArgumentOutOfRangeException();
            }
            
            animator = myFlock.faction switch
            {
                FlockManager.FlockFaction.BlueFaction => prefabBlue.GetComponent<Animator>(),
                FlockManager.FlockFaction.RedFaction => prefabRed.GetComponent<Animator>(),
                FlockManager.FlockFaction.GreenFaction => prefabGreen.GetComponent<Animator>(),
                _ => null
            };

            _isAnimatorNotNull = animator != null;
            lifeBarSlider.maxValue = data.maxHealthPoint;
            lifeBarSlider.value = data.maxHealthPoint;
        }

        private void Update() 
        {
            if (_isDead) return;
            CalculateForces();
            MoveForward();
            ManageShooting();
        }

        public void TickUpdate()
        {
            if (_isDead) return;
            ManageEngagement();
            ManageDesengagement();
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
                            if (_selectionManager.currentlySelectedUnits.Contains(this))
                            {
                                Debug.DrawLine(transform.position, unit.transform.position, myFlock.flockColor);
                            }
                        }
                        else Debug.DrawLine(transform.position, unit.transform.position, myFlock.flockColor);
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
                        if (distToOtherUnit < data.engagementDist)
                        {
                            if (!_unitsInRangeOfEngagement.Contains(unit))
                            {
                                _unitsInRangeOfEngagement.Add(unit);
                            }
                        }
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
            
            switch (data.unitType)
            {
                case UnitData.UnitType.Aérienne:
                    _distToMyAnchor = Vector3.Distance(transform.position, myFlock.anchor.transform.position);
                    break;
                
                case UnitData.UnitType.Terrestre:
                    var anchorPos = myFlock.anchor.transform.position;
                    Vector3 anchorPosOnGround = new Vector3(anchorPos.x, 1, anchorPos.z);
                
                    _distToMyAnchor = Vector3.Distance(transform.position, anchorPosOnGround);
                    break;
                
                default:
                    _distToMyAnchor = Vector3.Distance(transform.position, myFlock.anchor.transform.position);
                    break;
            }

            _returnToAnchorForce *= (_distToMyAnchor / (10 -_flockManager.anchorWeightMultiplicator)) + 1;
            isOutOfBoundOfAnchor = _distToMyAnchor > maxDistFromAnchor;
        }
        
        private void MoveForward()
        {
            if (isInIdle)
            {
                if (_isAnimatorNotNull) animator.SetBool("isMoving", false);
                return;
            }

            if (_isDesengaged)
            {
                var dir = myBase.position - transform.position;
                Vector3 returnToBaseForce = dir.normalized;
                
                _force = _separationForce * _flockManager.separationWeight + _cohesionForce *
                         _flockManager.cohesionWeight + _alignmentForce * _flockManager.alignmentWeight + 
                         returnToBaseForce * _flockManager.baseWeight;
            }
            else
            {
                _force = _flockManager.useWeights switch
                {
                    true => _separationForce * _flockManager.separationWeight + _cohesionForce * _flockManager.cohesionWeight +
                            _alignmentForce * _flockManager.alignmentWeight + _returnToAnchorForce * _flockManager.anchorWeight,
                    false => _separationForce + _cohesionForce + _alignmentForce + _returnToAnchorForce
                };
            }
            
            var correctedForce = _force;
            if (data.unitType is UnitData.UnitType.Terrestre) correctedForce = new Vector3(_force.x, 0, _force.z);

            if (_isDesengaged)
            {
                var speed = data.desengamentSpeed switch
                {
                    UnitData.PossibleDesengamentSpeed.NormalSpeed => data.normalSpeed,
                    UnitData.PossibleDesengamentSpeed.AttackSpeed => data.attackSpeed,
                    UnitData.PossibleDesengamentSpeed.RenforcementSpeed => data.renforcementSpeed,
                    _ => throw new ArgumentOutOfRangeException()
                };

                ApplyVelocity(speed, correctedForce);
            }
            else if (isOutOfBoundOfAnchor) ApplyVelocity(data.renforcementSpeed, correctedForce);
            else ApplyVelocity(currentSpeed, correctedForce);
        }

        private void ApplyVelocity(float speed, Vector3 force)
        {
            _velocity = transform.forward * speed + force * Time.deltaTime; 
            _velocity = _velocity.normalized * speed;
            if (_isAnimatorNotNull) animator.SetBool("isMoving", true);
            transform.position += _velocity * Time.deltaTime;
            transform.rotation = Quaternion.LookRotation(_velocity);
        }
        #endregion

        #region Combat Behaviour
        
        List<Unit> preSelectedEngagementTarget = new();
        private int _dataForTargeting;

        #region Engagement
        private void ManageEngagement()
        {
            if (data.canProtect)
            {
                foreach (var unitData in data.protectionUnitList)
                {
                    foreach (var unit in myFlock.unitsInFlocks)
                    {
                        if (unit.data == unitData && unit.agressor is not null)
                        {
                            Engage(unit.agressor);
                            return;
                        }
                    }
                }
            }

            if (_unitsInRangeOfEngagement.Count is 0 || !isReadyToEngage || engagedUnit is not null || _isDesengaged) return;

            if (_unitsInRangeOfEngagement.Count is 1)
            {
                Engage(_unitsInRangeOfEngagement[0]);
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
            if (engagedUnit is not null) return; // Si une unité est déja engagé, return
                
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

                    if (!closestUnit)
                    {
                        closestUnit = unit;
                        distToClosestUnit = distToUnit;
                    }
                    else if (distToUnit < distToClosestUnit)
                    {
                        closestUnit = unit;
                        distToClosestUnit = distToUnit;
                    }
                }

                Engage(closestUnit);
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
                    
                    case 0: throw new ArgumentException();
                }
            }
            
            if (preSelectedEngagementTarget.Count == 1) engagedUnit = preSelectedEngagementTarget[0];
            else
            {
                _indexOfCurrentTargetingPriority++;
                DetermineTarget();   
            }
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

        private void Engage(Unit unit)
        {
            engagedUnit = unit;
            engagedUnit.agressor = this;
        }
        #endregion

        #region Desengagement

        private void ManageDesengagement()
        {
            if (!data.canDesengage || data.desengagementTriggers.Count is 0 || _isDesengaged) return;

            foreach (var desengagementTrigger in data.desengagementTriggers)
            {
                if (desengagementTrigger == UnitData.DesengagementTrigger.ByDistance)
                {
                    if (_distToMyAnchor > data.desengamentDist) Desengage();
                }
                else if (desengagementTrigger == UnitData.DesengagementTrigger.ByHealthPoint)
                {
                    if (currentHealthPoint < data.dammageThreshold) Desengage();
                }
            }
        }

        private void Desengage()
        {
            engagedUnit.agressor = null;
            engagedUnit = null;
            _isDesengaged = true;
        }
        #endregion

        #region Shooting

        private void ManageShooting()
        {
            if (engagedUnit is null || _isShooting) return;
            Shoot();
            StartCoroutine(ShootingCooldown());

        }

        private void Shoot()
        {
            _isShooting = true;
            var shotDamage = Random.Range(data.damageRange.x, data.damageRange.y);
            engagedUnit.TakeDamage(shotDamage);
        }

        public void TakeDamage(int damage)
        {
            if (_isDead) return;
         
            if (currentHealthPoint - damage > 0)
            {
                currentHealthPoint -= damage;
                lifeBarSlider.value = currentHealthPoint;
            }
            else
            {
                currentHealthPoint = 0;
                lifeBarSlider.value = 0;
                StartCoroutine(Die());
            }
        }

        private IEnumerator Die()
        {
            _isDead = true;
            if (_isAnimatorNotNull) animator.SetBool("isDead", true);
            yield return new WaitForSeconds(5);
            gameObject.SetActive(false);
        }

        private IEnumerator ShootingCooldown()
        {
            yield return new WaitForSeconds(data.delayBetweenShots);
            _isShooting = false;
        }

        #endregion
        #endregion

        #region Selection
        private void OnMouseEnter()
        {
            _selectionManager.mouseAboveThisUnit = this;
            selectionCircle.SetActive(true);
        }

        private void OnMouseExit()
        { 
            _selectionManager.mouseAboveThisUnit = null;
            if (_selectionManager.currentlySelectedFlock != myFlock) selectionCircle.SetActive(false);
        }
        #endregion

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || !_flockManager) return;
            if (_gameManager.showGizmosOnSelectedUnitOnly)
            {
                if (_selectionManager.currentlySelectedUnits.Count is 0) return;
                if (_selectionManager.currentlySelectedUnits.Contains(this)) ShowGizmos();
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
                if (engagedUnit != null && _gameManager.showUnitdEngagedUnits)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawLine(transform.position,engagedUnit.transform.position);
                }
        }
    }
}