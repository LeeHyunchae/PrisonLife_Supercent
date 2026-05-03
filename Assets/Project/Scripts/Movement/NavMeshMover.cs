using System.Threading;
using Cysharp.Threading.Tasks;
using PrisonLife.Core;
using UnityEngine;
using UnityEngine.AI;

namespace PrisonLife.Movement
{
    public class NavMeshMover : IMover
    {
        private readonly NavMeshAgent navMeshAgent;
        private readonly Transform actorTransform;
        private readonly float rotationLerpRate;

        public NavMeshMover(NavMeshAgent _navMeshAgent, Transform _actorTransform, float _rotationLerpRate = 15f)
        {
            navMeshAgent = _navMeshAgent;
            actorTransform = _actorTransform;
            rotationLerpRate = _rotationLerpRate;
        }

        public Vector3 CurrentPosition => actorTransform.position;

        public bool HasArrivedAtDestination
        {
            get
            {
                if (navMeshAgent == null) return true;
                if (navMeshAgent.pathPending) return false;
                if (!navMeshAgent.hasPath) return false;
                return navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance;
            }
        }

        public void SetVelocity(Vector3 _velocityWorldSpace)
        {
            if (navMeshAgent == null) return;

            if (navMeshAgent.hasPath)
            {
                navMeshAgent.ResetPath();
            }

            if (_velocityWorldSpace.sqrMagnitude < 0.0001f)
            {
                navMeshAgent.velocity = Vector3.zero;
                return;
            }

            navMeshAgent.Move(_velocityWorldSpace * Time.deltaTime);
            ApplyRotationTowardsDirection(_velocityWorldSpace);
        }

        public void SetDestination(Vector3 _targetWorldPosition)
        {
            if (navMeshAgent == null) return;
            if (!navMeshAgent.isOnNavMesh) return;
            navMeshAgent.ResetPath();
            navMeshAgent.SetDestination(_targetWorldPosition);
        }

        public void StopImmediately()
        {
            if (navMeshAgent == null) return;
            if (navMeshAgent.isOnNavMesh) navMeshAgent.ResetPath();
            navMeshAgent.velocity = Vector3.zero;
        }

        public async UniTask MoveToAsync(Vector3 _targetWorldPosition, CancellationToken _cancellationToken = default)
        {
            if (navMeshAgent == null) return;

            navMeshAgent.ResetPath();
            navMeshAgent.SetDestination(_targetWorldPosition);

            await UniTask.WaitUntil(
                () => HasArrivedAtDestination,
                cancellationToken: _cancellationToken);
        }

        private void ApplyRotationTowardsDirection(Vector3 _velocityWorldSpace)
        {
            Vector3 lookDirection = new Vector3(_velocityWorldSpace.x, 0f, _velocityWorldSpace.z);
            if (lookDirection.sqrMagnitude < 0.0001f) return;

            Quaternion targetRotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
            actorTransform.rotation = Quaternion.Slerp(
                actorTransform.rotation,
                targetRotation,
                rotationLerpRate * Time.deltaTime);
        }
    }
}
