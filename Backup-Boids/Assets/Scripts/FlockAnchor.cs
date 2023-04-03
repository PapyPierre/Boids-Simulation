using UnityEngine;

public class FlockAnchor : MonoBehaviour
{
   private FlockManager.Flock _myFlock;

   [SerializeField] private float anchorMatEmissionMultiplicator = 1.5f;
   
   [SerializeField] private MeshRenderer meshRenderer;
   [SerializeField] private LineRenderer lineRenderer;
   
   public void SetUpMeshRenderer(FlockManager.Flock newFlock)
   {
      _myFlock = newFlock;
      Material newAnchorMat = new Material(FlockManager.instance.defaultAnchorMat);
      meshRenderer.material = newAnchorMat;
      meshRenderer.material.color = _myFlock.flockColor;
      meshRenderer.material.SetColor("_EmissionColor",  _myFlock.flockColor * anchorMatEmissionMultiplicator);
   }

   public void SetUpLineRender()
   {
      var myPos = transform.position;
      lineRenderer.SetPosition(0, myPos);
      lineRenderer.SetPosition(1, new Vector3(myPos.x, myPos.y -100, myPos.z));
      lineRenderer.material = meshRenderer.material;
      lineRenderer.startColor = _myFlock.flockColor * anchorMatEmissionMultiplicator;
      lineRenderer.endColor = _myFlock.flockColor * anchorMatEmissionMultiplicator;
   }

   public void MoveAnchorTo(Vector3 pos)
   {
      transform.position = pos;
      SetUpLineRender();
      _myFlock.CurrentState = FlockManager.FlockState.EnDéplacement;
      foreach (var unit in _myFlock.unitsInFlocks) unit.isInIdle = false;
   }
}