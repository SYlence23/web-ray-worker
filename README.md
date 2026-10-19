# web-ray-worker

ASP.NET Core **ray tracing worker**. It receives render tasks from [web-load-balancer](../web-load-balancer), renders them on all CPU cores, reports progress, and returns the image as a PNG.

Several identical instances run at the same time (2 minimum, more when autoscaled). Workers are **stateless**: they don't use the database, keep tasks only in memory, and can be stopped or restarted at any time.

> Part of the [raytracer](../) project. The other repos are [web-load-balancer](../web-load-balancer) (FastAPI gateway) and [web-client](../web-client) (React UI).

---

## How it fits

```
web-client ──▶ web-load-balancer ──▶ web-ray-worker-1
                     │           └──▶ web-ray-worker-2
                     ▼           └──▶ web-ray-worker-N
                  web-db
```

The worker is never called by the browser directly, only by the load balancer.

## What it renders

A path tracer based on the free book [*Ray Tracing in One Weekend*](https://raytracing.github.io/books/RayTracingInOneWeekend.html):

- **Geometry:** spheres
- **Materials:** matte (Lambertian), metal (with fuzz), glass (dielectric with refraction)
- **Camera:** position, look-at point, field of view, optional depth of field
- **Anti-aliasing:** N samples per pixel, averaged
- **Bounces:** up to `MaxDepth` reflections per ray

### Input and output

- **Input:** a preset scene name plus the image settings. The worker builds the 3D scene in code, so no model files are uploaded.
- **Output:** a PNG image, which can be fetched partially while rendering.

### Preset scenes

| Name | Description |
|---|---|
| `spheres` | A few spheres with different materials on a ground plane |
| `glass` | Glass and metal spheres showing refraction and reflection |
| `random` | The book's cover scene, with hundreds of small random spheres (heaviest) |

New scenes are added in `RayTracer/Scenes/`.

---

## API

| Method | Path | Description |
|---|---|---|
| `POST` | `/tasks` | Start a render. Returns `201 { id }`, or `503` if the worker is full |
| `GET` | `/tasks/{id}` | `{ status, progress, error? }` |
| `GET` | `/tasks/{id}/image` | PNG, partial while rendering or final when done |
| `DELETE` | `/tasks/{id}` | Cancel the task |
| `GET` | `/health` | `{ name, running, capacity }` |

This must stay in sync with the "Renderer contract" section of the web-load-balancer README.

### `POST /tasks`

```json
{
  "scene": "glass",
  "width": 800,
  "height": 450,
  "samplesPerPixel": 64,
  "maxDepth": 20,
  "seed": 42
}
```

Response: `201 { "id": "3f2c..." }`

| Status | When |
|---|---|
| `400` | Unknown scene or invalid numbers |
| `422` | Over the worker's own complexity limit (a safety net; the gateway checks first) |
| `503` | `running >= capacity`, so the gateway should pick another worker |

### `GET /tasks/{id}`

```json
{ "status": "running", "progress": 37.5, "error": null }
```

- `status` is one of `running`, `done`, `cancelled` or `failed`.
- `progress` is the percentage of rows finished, from 0 to 100.

### `GET /health`

```json
{ "name": "web-ray-worker-1", "running": 1, "capacity": 2 }
```

The load balancer uses `running / capacity` to choose the least-loaded worker.

---

## How rendering works

1. `POST /tasks` validates the request, creates a `RenderTask` and starts it on a background thread, then returns at once.
2. The image is rendered **row by row** with `Parallel.For`, so one task uses every core.
3. After each row the worker increments a counter, and `progress = rowsDone / height × 100`.
4. Pixels are written into a shared buffer, so `GET /image` can encode a partial PNG at any moment.
5. Every row checks the task's `CancellationToken`. `DELETE` or a timeout stops the render within one row.
6. Finished and cancelled tasks are kept in memory for `TaskTtlMinutes`, then removed. The gateway downloads the final image and stores it.

### Capacity

`Capacity` is how many tasks run at the same time. Because each task already uses all cores, `1` or `2` is a sensible value. More tasks just split the CPU and make each one slower.

---

## Configuration

Settings come from `appsettings.json` or environment variables:

| Setting | Env var | Default | Description |
|---|---|---|---|
| Worker name | `Worker__Name` | machine name | Shown in `/health` and the admin panel |
| Capacity | `Worker__Capacity` | `2` | Max concurrent tasks |
| Max complexity | `Worker__MaxComplexity` | `150000000` | Max width × height × samples |
| Max render time | `Worker__MaxRenderSeconds` | `120` | Task is cancelled after this |
| Task TTL | `Worker__TaskTtlMinutes` | `10` | How long finished tasks stay in memory |
| Port | `ASPNETCORE_URLS` | `http://+:8080` | Listen address |

## Tech stack

.NET 8 · ASP.NET Core minimal APIs · `System.Numerics.Vector3` for vector math · `Parallel.For` · SixLabors.ImageSharp for PNG encoding · Swagger (Swashbuckle) for testing

## Project structure

```
web-ray-worker/
├── src/
│   ├── Worker.Api/              # ASP.NET Core web app
│   │   ├── Program.cs           # endpoints, DI, config
│   │   ├── TaskManager.cs       # in-memory task store, capacity, cleanup
│   │   ├── RenderTask.cs        # status, progress, buffer, CancellationTokenSource
│   │   └── Dockerfile
│   └── RayTracer/               # pure rendering library, no web code
│       ├── Ray.cs, Camera.cs
│       ├── Hittables/           # Sphere, HittableList
│       ├── Materials/           # Lambertian, Metal, Dielectric
│       ├── Scenes/              # spheres, glass, random
│       └── Renderer.cs          # Render(scene, settings, progress, token)
├── tools/
│   └── RenderCli/               # console app: render a scene to a PNG without the API
├── tests/
│   └── RayTracer.Tests/
└── web-ray-worker.sln
```

Keeping `RayTracer` separate from the web project means you can build and test the renderer with `RenderCli` before any API code exists.

## Running

**Console renderer only (first milestone):**

```bash
dotnet run --project tools/RenderCli -- --scene glass --width 400 --height 225 --spp 32 --out out.png
```

**API locally:**

```bash
dotnet run --project src/Worker.Api
# Swagger UI: http://localhost:8080/swagger
```

**Two workers for load balancing tests:**

```bash
docker build -t web-ray-worker -f src/Worker.Api/Dockerfile .
docker run -d -p 5001:8080 -e Worker__Name=worker-1 web-ray-worker
docker run -d -p 5002:8080 -e Worker__Name=worker-2 web-ray-worker
```

**Whole system:** run `docker compose up --build` from the main `raytracer` repo.

## Roadmap

- [ ] Vector math, rays, camera and spheres; RenderCli writes a PNG
- [ ] Materials (matte, metal, glass) and anti-aliasing
- [ ] Preset scenes
- [ ] `Parallel.For` rendering with a progress callback
- [ ] Minimal API: `POST/GET/DELETE /tasks`, `/health`
- [ ] Partial image endpoint
- [ ] Capacity limit (`503`), timeout and task cleanup
- [ ] Dockerfile and two instances running
- [ ] Unit tests for ray-sphere hits and the materials

## Backlog

- [ ] Triangle meshes (`.obj` models) with a BVH for speed
- [ ] User-adjustable scenes (number of spheres, camera, materials)
- [ ] Progressive rendering (low samples first, refined over time)
