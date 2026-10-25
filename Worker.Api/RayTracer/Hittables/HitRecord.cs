using System.Numerics;

namespace RayTracer.Hittables
{
    public struct HitRecord
    {
        public Vector3 Point;   
        public Vector3 Normal;  
        public float T;        
        public bool FrontFace;  
        public void SetFaceNormal(in Ray ray, Vector3 outwardNormal)
        {
            FrontFace = Vector3.Dot(ray.Direction, outwardNormal) < 0;
            Normal = FrontFace ? outwardNormal : -outwardNormal;
        }
    }
}