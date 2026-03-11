using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// 2D Smoothed Particle Hydrodynamics fluid simulation.
/// Particles are rendered onto a grid texture via kernel splatting.
/// Mouse movement pushes nearby particles.
///
/// Uses auto-calibrated rest density, substeps for stability,
/// and velocity clamping to prevent explosions.
/// </summary>
public partial class SPHFluidSimulation : TextureRect
{
    // ── Configuration ───────────────────────────────────────

    [ExportGroup("Grid")]
    [Export] public int Resolution = 100;

    [ExportGroup("Particles")]
    [Export] public int ParticleCount = 2000;

    [ExportGroup("SPH")]
    [Export] public float KernelRadius = 0.025f;
    [Export] public float Stiffness = 200.0f;
    [Export] public float Viscosity = 80.0f;
    [Export] public float Gravity = 0.8f;
    [Export] public float BoundaryDamping = 0.3f;
    [Export] public float MaxVelocity = 2.0f;
    [Export] public int SubSteps = 4;

    [ExportGroup("Mouse")]
    [Export] public float MouseRadius = 0.1f;
    [Export] public float MouseForce = 50.0f;

    [ExportGroup("Rendering")]
    [Export] public float SplatRadius = 3.0f;
    [Export] public float ColorIntensity = 0.2f;

    // ── Particle data (struct-of-arrays) ────────────────────

    private float[] _px, _py;
    private float[] _vx, _vy;
    private float[] _fx, _fy;
    private float[] _density;
    private float[] _pressure;

    // ── SPH ─────────────────────────────────────────────────

    private float _h, _h2;
    private float _poly6Coeff, _spikyGradCoeff, _viscLapCoeff;
    private float _mass;
    private float _restDensity;

    // ── Spatial hash ────────────────────────────────────────

    private int _hashSize;
    private float _cellSize;
    private List<int>[] _grid;

    // ── Rendering ───────────────────────────────────────────

    private float[] _rBuf, _gBuf, _bBuf;
    private Image _img;
    private ImageTexture _tex;

    // ── Mouse ───────────────────────────────────────────────

    private Vector2 _prevMouse;
    private Vector2 _mouseVel;
    private bool _hasPrev;

    public override void _Ready()
    {
        _h = KernelRadius;
        _h2 = _h * _h;
        float h4 = _h2 * _h2;
        float h5 = h4 * _h;
        float h8 = h4 * h4;

        // 2D kernel coefficients (Müller et al.)
        _poly6Coeff = 4.0f / (Mathf.Pi * h8);
        _spikyGradCoeff = -10.0f / (Mathf.Pi * h5);
        _viscLapCoeff = 40.0f / (Mathf.Pi * h5);

        // Allocate
        _px = new float[ParticleCount]; _py = new float[ParticleCount];
        _vx = new float[ParticleCount]; _vy = new float[ParticleCount];
        _fx = new float[ParticleCount]; _fy = new float[ParticleCount];
        _density = new float[ParticleCount];
        _pressure = new float[ParticleCount];

        // Initialize particles scattered in upper portion
        InitParticles();

        // Spatial hash
        _cellSize = _h;
        _hashSize = Mathf.Max((int)(1.0f / _cellSize) + 1, 1);
        _grid = new List<int>[_hashSize * _hashSize];
        for (int i = 0; i < _grid.Length; i++)
            _grid[i] = new List<int>(16);

        // Auto-calibrate rest density from initial configuration
        _mass = 1.0f;
        BuildSpatialHash();
        ComputeDensity();
        float sum = 0;
        for (int i = 0; i < ParticleCount; i++) sum += _density[i];
        _restDensity = sum / ParticleCount;
        if (_restDensity < 1.0f) _restDensity = 1.0f;
        GD.Print($"SPH: Auto-calibrated rest density = {_restDensity:F1}, h={_h}, particles={ParticleCount}");

        // Render buffers
        _rBuf = new float[Resolution * Resolution];
        _gBuf = new float[Resolution * Resolution];
        _bBuf = new float[Resolution * Resolution];

        _img = Image.CreateEmpty(Resolution, Resolution, false, Image.Format.Rgba8);
        _tex = ImageTexture.CreateFromImage(_img);
        Texture = _tex;

        ExpandMode = ExpandModeEnum.IgnoreSize;
        StretchMode = StretchModeEnum.Scale;
        TextureFilter = TextureFilterEnum.Linear;
        MouseFilter = MouseFilterEnum.Ignore;
    }

    private void InitParticles()
    {
        // Spread particles in the upper portion so they fall naturally
        int side = (int)Mathf.Ceil(Mathf.Sqrt(ParticleCount));
        float spacing = _h * 0.55f;
        float blockW = side * spacing;
        float startX = 0.5f - blockW * 0.5f;
        float startY = 0.15f;

        for (int i = 0; i < ParticleCount; i++)
        {
            int row = i / side;
            int col = i % side;
            _px[i] = Mathf.Clamp(startX + col * spacing + (float)GD.RandRange(-0.001, 0.001), 0.01f, 0.99f);
            _py[i] = Mathf.Clamp(startY + row * spacing + (float)GD.RandRange(-0.001, 0.001), 0.01f, 0.99f);
            _vx[i] = 0; _vy[i] = 0;
        }
    }

    public override void _Process(double delta)
    {
        float dt = Mathf.Min((float)delta, 0.02f);
        float subDt = dt / SubSteps;

        UpdateMouse();

        for (int s = 0; s < SubSteps; s++)
        {
            BuildSpatialHash();
            ComputeDensity();
            ComputePressure();
            ComputeForces();
            Integrate(subDt);
        }

        Render();
    }

    // ── Mouse ───────────────────────────────────────────────

    private void UpdateMouse()
    {
        Vector2 mouse = GetLocalMousePosition();
        Vector2 sz = Size;
        if (sz.X <= 0 || sz.Y <= 0) return;

        Vector2 norm = new Vector2(mouse.X / sz.X, mouse.Y / sz.Y);

        if (_hasPrev)
            _mouseVel = norm - _prevMouse;
        else
            _mouseVel = Vector2.Zero;

        _prevMouse = norm;
        _hasPrev = true;
    }

    // ── Spatial Hash ────────────────────────────────────────

    private void BuildSpatialHash()
    {
        for (int i = 0; i < _grid.Length; i++)
            _grid[i].Clear();

        for (int i = 0; i < ParticleCount; i++)
        {
            int cx = Mathf.Clamp((int)(_px[i] / _cellSize), 0, _hashSize - 1);
            int cy = Mathf.Clamp((int)(_py[i] / _cellSize), 0, _hashSize - 1);
            _grid[cy * _hashSize + cx].Add(i);
        }
    }

    // ── Density ─────────────────────────────────────────────

    private void ComputeDensity()
    {
        for (int i = 0; i < ParticleCount; i++)
        {
            _density[i] = 0;
            float xi = _px[i], yi = _py[i];

            int cx = Mathf.Clamp((int)(xi / _cellSize), 0, _hashSize - 1);
            int cy = Mathf.Clamp((int)(yi / _cellSize), 0, _hashSize - 1);

            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                {
                    int nx = cx + dx, ny = cy + dy;
                    if (nx < 0 || nx >= _hashSize || ny < 0 || ny >= _hashSize) continue;

                    var cell = _grid[ny * _hashSize + nx];
                    for (int k = 0; k < cell.Count; k++)
                    {
                        int j = cell[k];
                        float rx = xi - _px[j];
                        float ry = yi - _py[j];
                        float r2 = rx * rx + ry * ry;

                        if (r2 < _h2)
                        {
                            float diff = _h2 - r2;
                            _density[i] += _mass * _poly6Coeff * diff * diff * diff;
                        }
                    }
                }
        }
    }

    // ── Pressure ────────────────────────────────────────────

    private void ComputePressure()
    {
        for (int i = 0; i < ParticleCount; i++)
            _pressure[i] = Stiffness * (_density[i] - _restDensity);
    }

    // ── Forces ──────────────────────────────────────────────

    private void ComputeForces()
    {
        for (int i = 0; i < ParticleCount; i++)
        {
            float fpx = 0, fpy = 0;
            float xi = _px[i], yi = _py[i];
            float vxi = _vx[i], vyi = _vy[i];
            float di = _density[i];
            float pi = _pressure[i];

            if (di < 1e-6f) { _fx[i] = 0; _fy[i] = Gravity; continue; }

            int cx = Mathf.Clamp((int)(xi / _cellSize), 0, _hashSize - 1);
            int cy = Mathf.Clamp((int)(yi / _cellSize), 0, _hashSize - 1);

            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                {
                    int nx = cx + dx, ny = cy + dy;
                    if (nx < 0 || nx >= _hashSize || ny < 0 || ny >= _hashSize) continue;

                    var cell = _grid[ny * _hashSize + nx];
                    for (int k = 0; k < cell.Count; k++)
                    {
                        int j = cell[k];
                        if (j == i) continue;

                        float rx = xi - _px[j];
                        float ry = yi - _py[j];
                        float r2 = rx * rx + ry * ry;

                        if (r2 >= _h2 || r2 < 1e-12f) continue;

                        float r = Mathf.Sqrt(r2);
                        float dj = _density[j];
                        if (dj < 1e-6f) continue;

                        float hrDiff = _h - r;

                        // Pressure force (spiky gradient)
                        float pAvg = (pi + _pressure[j]) * 0.5f;
                        float pressScale = _mass * pAvg / dj * _spikyGradCoeff * hrDiff * hrDiff;
                        float invR = 1.0f / r;
                        fpx += pressScale * rx * invR;
                        fpy += pressScale * ry * invR;

                        // Viscosity force
                        float viscScale = _mass * Viscosity / dj * _viscLapCoeff * hrDiff;
                        fpx += viscScale * (_vx[j] - vxi);
                        fpy += viscScale * (_vy[j] - vyi);
                    }
                }

            _fx[i] = fpx / di;
            _fy[i] = fpy / di + Gravity;

            // Mouse interaction
            float mdx = _prevMouse.X - xi;
            float mdy = _prevMouse.Y - yi;
            float md2 = mdx * mdx + mdy * mdy;
            float mr2 = MouseRadius * MouseRadius;
            if (md2 < mr2 && _mouseVel.LengthSquared() > 1e-8f)
            {
                float influence = 1.0f - md2 / mr2;
                _fx[i] += _mouseVel.X * MouseForce * influence;
                _fy[i] += _mouseVel.Y * MouseForce * influence;
            }
        }
    }

    // ── Integration ─────────────────────────────────────────

    private void Integrate(float dt)
    {
        float maxV = MaxVelocity;

        for (int i = 0; i < ParticleCount; i++)
        {
            _vx[i] += _fx[i] * dt;
            _vy[i] += _fy[i] * dt;

            // Clamp velocity
            float speed2 = _vx[i] * _vx[i] + _vy[i] * _vy[i];
            if (speed2 > maxV * maxV)
            {
                float inv = maxV / Mathf.Sqrt(speed2);
                _vx[i] *= inv;
                _vy[i] *= inv;
            }

            _px[i] += _vx[i] * dt;
            _py[i] += _vy[i] * dt;

            // Boundary reflection
            float damp = BoundaryDamping;
            if (_px[i] < 0.001f) { _px[i] = 0.001f; _vx[i] = Mathf.Abs(_vx[i]) * damp; }
            if (_px[i] > 0.999f) { _px[i] = 0.999f; _vx[i] = -Mathf.Abs(_vx[i]) * damp; }
            if (_py[i] < 0.001f) { _py[i] = 0.001f; _vy[i] = Mathf.Abs(_vy[i]) * damp; }
            if (_py[i] > 0.999f) { _py[i] = 0.999f; _vy[i] = -Mathf.Abs(_vy[i]) * damp; }
        }
    }

    // ── Rendering ───────────────────────────────────────────

    private void Render()
    {
        int res = Resolution;
        int total = res * res;

        Array.Clear(_rBuf, 0, total);
        Array.Clear(_gBuf, 0, total);
        Array.Clear(_bBuf, 0, total);

        float splatR2 = SplatRadius * SplatRadius;
        int splatI = (int)Mathf.Ceil(SplatRadius);

        for (int p = 0; p < ParticleCount; p++)
        {
            float gx = _px[p] * res;
            float gy = _py[p] * res;
            int cx = (int)gx;
            int cy = (int)gy;

            float speed = Mathf.Sqrt(_vx[p] * _vx[p] + _vy[p] * _vy[p]);
            float hue = (Mathf.Atan2(_vy[p], _vx[p]) / (Mathf.Pi * 2) + 1.0f) % 1.0f;
            Color col = Color.FromHsv(hue, 0.7f, 1.0f);
            float weight = (speed * 4.0f + 0.2f) * ColorIntensity;

            for (int di = -splatI; di <= splatI; di++)
                for (int dj = -splatI; dj <= splatI; dj++)
                {
                    int ix = cx + di, iy = cy + dj;
                    if (ix < 0 || ix >= res || iy < 0 || iy >= res) continue;

                    float ddx = gx - ix - 0.5f;
                    float ddy = gy - iy - 0.5f;
                    float d2 = ddx * ddx + ddy * ddy;
                    if (d2 >= splatR2) continue;

                    float falloff = 1.0f - d2 / splatR2;
                    float w = falloff * weight;

                    int idx = iy * res + ix;
                    _rBuf[idx] += col.R * w;
                    _gBuf[idx] += col.G * w;
                    _bBuf[idx] += col.B * w;
                }
        }

        for (int j = 0; j < res; j++)
            for (int i = 0; i < res; i++)
            {
                int idx = j * res + i;
                float r = Mathf.Clamp(_rBuf[idx], 0, 1);
                float g = Mathf.Clamp(_gBuf[idx], 0, 1);
                float b = Mathf.Clamp(_bBuf[idx], 0, 1);
                float a = Mathf.Clamp((r + g + b) * 0.8f, 0, 1);
                _img.SetPixel(i, j, new Color(r, g, b, a));
            }

        _tex.Update(_img);
    }
}
