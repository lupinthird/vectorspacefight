using Microsoft.Xna.Framework;
using VectorSpaceFight.Game.Constants;
using VectorSpaceFight.Game.Entities;
using VectorSpaceFight.Game.Rendering;

namespace VectorSpaceFight.Game.Systems;

public class DebrisSystem
{
    public void Update(List<LineDebris> debris, float dt)
    {
        foreach (var piece in debris)
            piece.Update(dt);
    }

    public void SpawnShipDebris(List<LineDebris> debris, Ship ship, Vector2 impactDirection)
    {
        var points = LineBatch.GetShipPoints(ship.Position, ship.Rotation, 14f);
        var edges = new[]
        {
            (points[0], points[1]),
            (points[1], points[2]),
            (points[2], points[0])
        };

        SpawnEdgeDebris(debris, edges, ship.Velocity, impactDirection, ship.Color);
    }

    public void SpawnAsteroidDebris(List<LineDebris> debris, Asteroid asteroid, Vector2 impactDirection)
    {
        var edges = asteroid.GetWorldEdges();
        SpawnEdgeDebris(debris, edges, asteroid.Velocity, impactDirection, new Color(200, 200, 200));
    }

    private static void SpawnEdgeDebris(
        List<LineDebris> debris,
        (Vector2 Start, Vector2 End)[] edges,
        Vector2 entityVelocity,
        Vector2 impactDirection,
        Color color)
    {
        for (int i = 0; i < edges.Length; i++)
        {
            var piece = GetInactive(debris);
            piece.Spawn(edges[i].Start, edges[i].End, entityVelocity, impactDirection, i, edges.Length, color);
        }
    }

    private static LineDebris GetInactive(List<LineDebris> debris)
    {
        foreach (var piece in debris)
        {
            if (!piece.Active)
                return piece;
        }

        var created = new LineDebris();
        debris.Add(created);
        return created;
    }
}
