using System.ComponentModel.DataAnnotations;

namespace Worker.Api.Models
{
    public class RenderRequest
    {
        [Required]
        public string Scene { get; set; } = string.Empty;

        [Range(1, 1920)]
        public int Width { get; set; }

        [Range(1, 1080)]
        public int Height { get; set; }

        [Range(1, 256)]
        public int SamplesPerPixel { get; set; }

        [Range(1, 100)]
        public int MaxDepth { get; set; } = 20;

        public int? Seed { get; set; }

        public long Complexity => (long)Width * Height * SamplesPerPixel;
    }
}
