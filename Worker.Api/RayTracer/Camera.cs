using System.Numerics;

namespace RayTracer
{
    public record CameraSettings(
        Vector3 LookFrom,
        Vector3 LookAt,
        float VerticalFov,
        float Aperture = 0f,
        float? FocusDistance = null);

    public class Camera
    {
        private readonly Vector3 _origin;
        private readonly Vector3 _lowerLeftCorner;
        private readonly Vector3 _horizontal;
        private readonly Vector3 _vertical;
        private readonly Vector3 _u;
        private readonly Vector3 _v;
        private readonly float _lensRadius;

        public Camera(CameraSettings settings, float aspectRatio)
        {
            var theta = settings.VerticalFov * MathF.PI / 180f;
            var viewportHeight = 2f * MathF.Tan(theta / 2);
            var viewportWidth = aspectRatio * viewportHeight;
            var focusDistance = settings.FocusDistance ?? (settings.LookFrom - settings.LookAt).Length();

            var w = Vector3.Normalize(settings.LookFrom - settings.LookAt);
            _u = Vector3.Normalize(Vector3.Cross(Vector3.UnitY, w));
            _v = Vector3.Cross(w, _u);

            _origin = settings.LookFrom;
            _horizontal = focusDistance * viewportWidth * _u;
            _vertical = focusDistance * viewportHeight * _v;
            _lowerLeftCorner = _origin - _horizontal / 2 - _vertical / 2 - focusDistance * w;
            _lensRadius = settings.Aperture / 2;
        }

        public Ray GetRay(float s, float t, Random rng)
        {
            var rd = _lensRadius * VectorMath.RandomInUnitDisk(rng);
            var offset = _u * rd.X + _v * rd.Y;
            return new Ray(
                _origin + offset,
                _lowerLeftCorner + s * _horizontal + t * _vertical - _origin - offset);
        }
    }
}
