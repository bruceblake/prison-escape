using UnityEngine;

namespace Prison
{
    /// <summary>
    /// The physical half of the objective waypoint: a caution-yellow route line painted on the
    /// floor along the NavMesh path, plus a pulsing beacon (light beam + ground ring) at the
    /// destination. Built from primitives/LineRenderer at runtime — no scene wiring, no colliders.
    /// Owned and driven by <see cref="ObjectiveWaypointUI"/>.
    /// </summary>
    public class WaypointWorldGuide : MonoBehaviour
    {
        private const float LineWidth = 0.28f;
        private const float LineFloorOffset = 0.1f;
        private const float BeamHeight = 7f;
        private const float BeamThickness = 0.3f;
        private const float RingDiameter = 2.2f;

        private LineRenderer _pathLine;
        private Transform _beam;
        private Transform _ring;
        private Material _lineMat;
        private Material _beamMat;
        private Material _ringMat;

        public static WaypointWorldGuide Create()
        {
            var root = new GameObject("WaypointWorldGuide");
            DontDestroyOnLoad(root);
            var guide = root.AddComponent<WaypointWorldGuide>();
            guide.Build();
            return guide;
        }

        private void Build()
        {
            var shader = Shader.Find("Sprites/Default"); // unlit, transparent, pipeline-agnostic
            Color yellow = PrisonUITheme.CautionYellow;

            _lineMat = new Material(shader);
            _lineMat.color = new Color(yellow.r, yellow.g, yellow.b, 0.85f);

            var lineGo = new GameObject("PathLine");
            lineGo.transform.SetParent(transform, false);
            _pathLine = lineGo.AddComponent<LineRenderer>();
            _pathLine.material = _lineMat;
            _pathLine.startWidth = LineWidth;
            _pathLine.endWidth = LineWidth * 0.55f;
            _pathLine.numCornerVertices = 4;
            _pathLine.numCapVertices = 4;
            _pathLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _pathLine.receiveShadows = false;
            _pathLine.alignment = LineAlignment.TransformZ;   // flat on the floor
            _pathLine.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            _pathLine.positionCount = 0;

            _beamMat = new Material(shader);
            _beamMat.color = new Color(yellow.r, yellow.g, yellow.b, 0.4f);
            _beam = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
            _beam.name = "DestinationBeam";
            _beam.SetParent(transform, false);
            Destroy(_beam.GetComponent<Collider>());
            _beam.GetComponent<MeshRenderer>().material = _beamMat;
            _beam.localScale = new Vector3(BeamThickness, BeamHeight, BeamThickness);

            _ringMat = new Material(shader);
            _ringMat.color = new Color(yellow.r, yellow.g, yellow.b, 0.55f);
            _ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder).transform;
            _ring.name = "DestinationRing";
            _ring.SetParent(transform, false);
            Destroy(_ring.GetComponent<Collider>());
            _ring.GetComponent<MeshRenderer>().material = _ringMat;
            _ring.localScale = new Vector3(RingDiameter, 0.02f, RingDiameter);

            SetVisible(false);
        }

        public void SetVisible(bool visible)
        {
            if (_pathLine != null) _pathLine.gameObject.SetActive(visible);
            if (_beam != null) _beam.gameObject.SetActive(visible);
            if (_ring != null) _ring.gameObject.SetActive(visible);
        }

        /// <summary>Paints the route on the floor and parks the beacon at the destination.</summary>
        public void UpdateGuide(Vector3[] pathCorners, Vector3 destination)
        {
            float pulse = 0.9f + 0.2f * Mathf.Sin(Time.time * 4f);

            if (_beam != null)
            {
                _beam.gameObject.SetActive(true);
                _beam.position = destination + Vector3.up * (BeamHeight * 0.5f);
                _beam.localScale = new Vector3(BeamThickness * pulse, BeamHeight, BeamThickness * pulse);
            }
            if (_ring != null)
            {
                _ring.gameObject.SetActive(true);
                _ring.position = destination + Vector3.up * 0.06f;
                _ring.localScale = new Vector3(RingDiameter * pulse, 0.02f, RingDiameter * pulse);
            }

            if (_pathLine == null) return;
            if (pathCorners == null || pathCorners.Length < 2)
            {
                _pathLine.positionCount = 0;
                return;
            }

            _pathLine.gameObject.SetActive(true);
            _pathLine.positionCount = pathCorners.Length;
            for (int i = 0; i < pathCorners.Length; i++)
                _pathLine.SetPosition(i, pathCorners[i] + Vector3.up * LineFloorOffset);
        }

        private void OnDestroy()
        {
            if (_lineMat != null) Destroy(_lineMat);
            if (_beamMat != null) Destroy(_beamMat);
            if (_ringMat != null) Destroy(_ringMat);
        }
    }
}
