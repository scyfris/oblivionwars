using Godot;
using System;

/// <summary>
/// 2D grid-based fluid simulation (Jos Stam's Stable Fluids).
/// Mouse movement injects velocity and dye into the fluid.
/// Rendered to a TextureRect with velocity-based coloring.
/// </summary>
public partial class FluidSimulation : TextureRect
{
    [ExportGroup("Grid")]
    [Export] public int N = 100;

    [ExportGroup("Fluid")]
    [Export] public float Viscosity = 0.0f;
    [Export] public float Diffusion = 0.0001f;
    [Export] public float DensityDecay = 0.995f;

    [ExportGroup("Mouse Interaction")]
    [Export] public float ForceScale = 5.0f;
    [Export] public float DensityRate = 200.0f;
    [Export] public int BrushRadius = 2;

    [ExportGroup("Solver")]
    [Export] public int Iterations = 4;

    private int _total;
    private float[] _u, _v, _u0, _v0;
    private float[] _d, _d0;

    private Image _img;
    private ImageTexture _tex;
    private Vector2 _prevMouse;
    private bool _hasPrev;

    private int IX(int x, int y) => x + (N + 2) * y;

    public override void _Ready()
    {
        _total = (N + 2) * (N + 2);
        _u  = new float[_total]; _v  = new float[_total];
        _u0 = new float[_total]; _v0 = new float[_total];
        _d  = new float[_total]; _d0 = new float[_total];

        _img = Image.CreateEmpty(N, N, false, Image.Format.Rgba8);
        _tex = ImageTexture.CreateFromImage(_img);
        Texture = _tex;

        ExpandMode = ExpandModeEnum.IgnoreSize;
        StretchMode = StretchModeEnum.Scale;
        TextureFilter = TextureFilterEnum.Linear;
        MouseFilter = MouseFilterEnum.Ignore;
    }

    public override void _Process(double delta)
    {
        float dt = Mathf.Min((float)delta, 0.05f);

        Array.Clear(_u0, 0, _total);
        Array.Clear(_v0, 0, _total);
        Array.Clear(_d0, 0, _total);

        ApplyMouse();
        VelStep(dt);
        DensStep(dt);
        Render();
    }

    // ── Mouse Input ─────────────────────────────────────────

    private void ApplyMouse()
    {
        Vector2 mouse = GetLocalMousePosition();
        Vector2 sz = Size;
        if (sz.X <= 0 || sz.Y <= 0) return;

        int i = Mathf.Clamp((int)(mouse.X / sz.X * N) + 1, 1, N);
        int j = Mathf.Clamp((int)(mouse.Y / sz.Y * N) + 1, 1, N);

        if (_hasPrev)
        {
            Vector2 diff = mouse - _prevMouse;
            float speed = diff.Length();

            if (speed > 0.5f)
            {
                // Inject velocity
                _u0[IX(i, j)] = diff.X * ForceScale;
                _v0[IX(i, j)] = diff.Y * ForceScale;

                // Inject density in a small brush
                for (int di = -BrushRadius; di <= BrushRadius; di++)
                    for (int dj = -BrushRadius; dj <= BrushRadius; dj++)
                    {
                        int ni = Mathf.Clamp(i + di, 1, N);
                        int nj = Mathf.Clamp(j + dj, 1, N);
                        _d0[IX(ni, nj)] = DensityRate;
                    }
            }
        }

        _prevMouse = mouse;
        _hasPrev = true;
    }

    // ── Velocity Step ───────────────────────────────────────

    private void VelStep(float dt)
    {
        AddSrc(_u, _u0, dt);
        AddSrc(_v, _v0, dt);

        Swap(ref _u, ref _u0);
        Diffuse(1, _u, _u0, Viscosity, dt);
        Swap(ref _v, ref _v0);
        Diffuse(2, _v, _v0, Viscosity, dt);

        Project(_u, _v, _u0, _v0);

        Swap(ref _u, ref _u0);
        Swap(ref _v, ref _v0);
        Advect(1, _u, _u0, _u0, _v0, dt);
        Advect(2, _v, _v0, _u0, _v0, dt);

        Project(_u, _v, _u0, _v0);
    }

    // ── Density Step ────────────────────────────────────────

    private void DensStep(float dt)
    {
        AddSrc(_d, _d0, dt);
        Swap(ref _d, ref _d0);
        Diffuse(0, _d, _d0, Diffusion, dt);
        Swap(ref _d, ref _d0);
        Advect(0, _d, _d0, _u, _v, dt);

        // Gradual decay so density doesn't accumulate forever
        for (int i = 0; i < _total; i++)
            _d[i] *= DensityDecay;
    }

    // ── Core Solver ─────────────────────────────────────────

    private void AddSrc(float[] x, float[] s, float dt)
    {
        for (int i = 0; i < _total; i++)
            x[i] += dt * s[i];
    }

    private void Diffuse(int b, float[] x, float[] x0, float diff, float dt)
    {
        float a = dt * diff * N * N;
        LinSolve(b, x, x0, a, 1 + 4 * a);
    }

    private void LinSolve(int b, float[] x, float[] x0, float a, float c)
    {
        float inv = 1.0f / c;
        for (int k = 0; k < Iterations; k++)
        {
            for (int j = 1; j <= N; j++)
                for (int i = 1; i <= N; i++)
                    x[IX(i, j)] = (x0[IX(i, j)] + a * (
                        x[IX(i - 1, j)] + x[IX(i + 1, j)] +
                        x[IX(i, j - 1)] + x[IX(i, j + 1)]
                    )) * inv;
            SetBnd(b, x);
        }
    }

    private void Advect(int b, float[] d, float[] d0, float[] u, float[] v, float dt)
    {
        float dt0 = dt * N;
        for (int j = 1; j <= N; j++)
            for (int i = 1; i <= N; i++)
            {
                float x = Mathf.Clamp(i - dt0 * u[IX(i, j)], 0.5f, N + 0.5f);
                float y = Mathf.Clamp(j - dt0 * v[IX(i, j)], 0.5f, N + 0.5f);

                int i0 = (int)x, j0 = (int)y;
                float s1 = x - i0, s0 = 1 - s1;
                float t1 = y - j0, t0 = 1 - t1;

                d[IX(i, j)] = s0 * (t0 * d0[IX(i0, j0)]     + t1 * d0[IX(i0, j0 + 1)]) +
                              s1 * (t0 * d0[IX(i0 + 1, j0)] + t1 * d0[IX(i0 + 1, j0 + 1)]);
            }
        SetBnd(b, d);
    }

    private void Project(float[] u, float[] v, float[] p, float[] div)
    {
        float h = 1.0f / N;
        for (int j = 1; j <= N; j++)
            for (int i = 1; i <= N; i++)
            {
                div[IX(i, j)] = -0.5f * h * (
                    u[IX(i + 1, j)] - u[IX(i - 1, j)] +
                    v[IX(i, j + 1)] - v[IX(i, j - 1)]);
                p[IX(i, j)] = 0;
            }
        SetBnd(0, div);
        SetBnd(0, p);

        LinSolve(0, p, div, 1, 4);

        for (int j = 1; j <= N; j++)
            for (int i = 1; i <= N; i++)
            {
                u[IX(i, j)] -= 0.5f * N * (p[IX(i + 1, j)] - p[IX(i - 1, j)]);
                v[IX(i, j)] -= 0.5f * N * (p[IX(i, j + 1)] - p[IX(i, j - 1)]);
            }
        SetBnd(1, u);
        SetBnd(2, v);
    }

    private void SetBnd(int b, float[] x)
    {
        for (int i = 1; i <= N; i++)
        {
            x[IX(0, i)]     = b == 1 ? -x[IX(1, i)] : x[IX(1, i)];
            x[IX(N + 1, i)] = b == 1 ? -x[IX(N, i)] : x[IX(N, i)];
            x[IX(i, 0)]     = b == 2 ? -x[IX(i, 1)] : x[IX(i, 1)];
            x[IX(i, N + 1)] = b == 2 ? -x[IX(i, N)] : x[IX(i, N)];
        }
        x[IX(0, 0)]         = 0.5f * (x[IX(1, 0)]     + x[IX(0, 1)]);
        x[IX(0, N + 1)]     = 0.5f * (x[IX(1, N + 1)] + x[IX(0, N)]);
        x[IX(N + 1, 0)]     = 0.5f * (x[IX(N, 0)]     + x[IX(N + 1, 1)]);
        x[IX(N + 1, N + 1)] = 0.5f * (x[IX(N, N + 1)] + x[IX(N + 1, N)]);
    }

    private void Swap(ref float[] a, ref float[] b) => (a, b) = (b, a);

    // ── Rendering ───────────────────────────────────────────

    private void Render()
    {
        for (int j = 0; j < N; j++)
            for (int i = 0; i < N; i++)
            {
                float d = _d[IX(i + 1, j + 1)];
                float uv = _u[IX(i + 1, j + 1)];
                float vv = _v[IX(i + 1, j + 1)];
                float speed = Mathf.Sqrt(uv * uv + vv * vv);

                // Hue from velocity direction — gives rainbow swirls
                float hue = (Mathf.Atan2(vv, uv) / (Mathf.Pi * 2) + 1.0f) % 1.0f;

                // Intensity from density + velocity magnitude
                float intensity = Mathf.Clamp(d * 0.01f + speed * 0.05f, 0, 1);

                Color c = Color.FromHsv(hue, 0.7f, intensity);
                c.A = Mathf.Clamp(intensity * 1.5f, 0, 1);

                _img.SetPixel(i, j, c);
            }
        _tex.Update(_img);
    }
}
