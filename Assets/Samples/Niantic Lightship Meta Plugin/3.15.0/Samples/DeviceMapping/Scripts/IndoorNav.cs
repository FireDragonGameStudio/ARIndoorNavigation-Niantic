using UnityEngine;
using UnityEngine.AI;

namespace Niantic.Lightship.MetaQuest.InternalSamples {
    public class IndoorNav : MonoBehaviour {
        [SerializeField] private GameObject navigationTarget;
        [SerializeField] private LineRenderer line;

        private Transform player;
        private NavMeshPath navMeshPath;

        private void Start() => navMeshPath = new NavMeshPath();

        private void OnEnable() => player = Camera.main.transform;

        private void Update() {
            if (player != null && navigationTarget != null) {
                NavMesh.CalculatePath(player.position, navigationTarget.transform.position, NavMesh.AllAreas, navMeshPath);

                if (navMeshPath.status == NavMeshPathStatus.PathComplete) {
                    line.positionCount = navMeshPath.corners.Length;
                    line.SetPositions(navMeshPath.corners);
                } else {
                    line.positionCount = 0;
                }
            }
        }
    }
}
