namespace RayTracer
{
    
    public class ImageBuffer
    {
        public ImageBuffer(int width, int height)
        {
            Width = width;
            Height = height;
            Pixels = new byte[width * height * 3];
        }

        public int Width { get; }

        public int Height { get; }

        public byte[] Pixels { get; }

        public void SetPixel(int x, int y, byte r, byte g, byte b)
        {
            var i = (y * Width + x) * 3;
            Pixels[i] = r;
            Pixels[i + 1] = g;
            Pixels[i + 2] = b;
        }

        public byte[] EncodePng() => PngEncoder.Encode(this);
    }
}
