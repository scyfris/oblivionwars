
// Tile types for surfaces that affect movement.
// Tile data: "surface_type"
public enum TileSurfaceType
{
    Normal = 0,
    Slippery = 1,
    Sticky = 2,
    Bouncy = 3
}

// Tile hazards that may affect player.
// Tiole data: "hazard_type"
public enum TileHazardType
{
    None = 0,
    Spikes = 1,
    Lava = 2,
    Acid = 3
}
