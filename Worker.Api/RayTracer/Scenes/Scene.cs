using RayTracer.Hittables;

namespace RayTracer.Scenes
{
    public record Scene(HittableList World, CameraSettings Camera);
}
