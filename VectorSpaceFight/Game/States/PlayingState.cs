using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using VectorSpaceFight.Game.Constants;
using VectorSpaceFight.Game.Entities;
using VectorSpaceFight.Game.Systems;

namespace VectorSpaceFight.Game.States;

public class PlayingState : IGameState
{
    private readonly GameContext _context;
    private readonly Action<Ship[]> _endMatch;

    private readonly AsteroidSpawner _asteroidSpawner;
    private readonly DebrisSystem _debrisSystem = new();
    private readonly CollisionSystem _collisionSystem;
    private readonly Ship[] _ships;
    private readonly List<Bullet> _bullets = new();
    private readonly List<Asteroid> _asteroids = new();
    private readonly List<LineDebris> _debris = new();

    private float _matchTimer;
    private float _elapsedTime;
    private int _leaderPlayerIndex;

    public PlayingState(GameContext context, Action<Ship[]> endMatch)
    {
        _context = context;
        _endMatch = endMatch;
        _asteroidSpawner = new AsteroidSpawner();
        _collisionSystem = new CollisionSystem(
            _asteroidSpawner,
            _debrisSystem,
            _debris,
            playRumble: () => context.Audio.PlayRumble(),
            playExplosion: () => context.Audio.PlayExplosion());
        _ships = new[]
        {
            new Ship(0),
            new Ship(1),
            new Ship(2),
            new Ship(3)
        };
    }

    public void Enter()
    {
        _matchTimer = GameConstants.MatchDurationSeconds;
        _elapsedTime = 0f;

        foreach (var ship in _ships)
        {
            ship.Kills = 0;
            ship.ResetToSpawn();
        }

        _bullets.Clear();
        _debris.Clear();
        _asteroidSpawner.Reset(_asteroids);
        _leaderPlayerIndex = -1;
        _context.Input.RequestAllSpinnerHeadingSync();
    }

    public void Exit()
    {
        _context.Audio.StopAll();
    }

    public void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _elapsedTime += dt;
        _matchTimer -= dt;

        if (_matchTimer <= 0f)
        {
            _endMatch(_ships);
            return;
        }

        for (int i = 0; i < _ships.Length; i++)
            _context.Input.SyncSpinnerHeadingIfNeeded(i, _ships[i].Rotation);

        var inputs = _context.Input.ReadInput();

        for (int i = 0; i < _ships.Length; i++)
        {
            var ship = _ships[i];
            UpdateRespawn(ship, dt);
            UpdateShield(ship, inputs[i].ShieldPressed, dt);

            if (ship.LeaderHighlightTimer > 0f)
                ship.LeaderHighlightTimer -= dt;

            bool canControl = ship.IsAlive && inputs[i].Connected;
            ship.IsThrusting = canControl && inputs[i].Thrust;
            _context.Audio.UpdateThrust(i, ship.IsThrusting, canControl);
            _context.Audio.UpdateShield(i, ship.ShieldActive && !ship.IsSpawnProtection && ship.IsAlive);

            if (!canControl)
                continue;

            var result = PhysicsSystem.ApplyShipControl(ship, inputs[i], dt, _bullets);
            if (result.FiredShot)
                _context.Audio.PlayShoot(i);
        }

        PhysicsSystem.UpdateShieldBreaches(_ships, dt);
        PhysicsSystem.UpdateBullets(_bullets, dt);
        PhysicsSystem.UpdateAsteroids(_asteroids, dt);
        _asteroidSpawner.Update(_asteroids, dt);
        _debrisSystem.Update(_debris, dt);

        _collisionSystem.Update(_ships, _bullets, _asteroids);
        PhysicsSystem.IntegrateShips(_ships, dt);

        foreach (var ship in _ships)
            WrapSystem.WrapShip(ship);

        WrapSystem.WrapBullets(_bullets);
        WrapSystem.WrapAsteroids(_asteroids);

        _collisionSystem.Update(_ships, _bullets, _asteroids);
        UpdateLeaderHighlight();
        _context.Audio.Update();
    }

    private void UpdateLeaderHighlight()
    {
        int leader = GetLeaderPlayerIndex();
        int leaderKills = _ships[leader].Kills;
        if (leaderKills <= 0)
        {
            _leaderPlayerIndex = -1;
            return;
        }

        if (leader == _leaderPlayerIndex)
            return;

        _leaderPlayerIndex = leader;
        _ships[leader].LeaderHighlightTimer = GameConstants.LeaderHighlightDuration;
    }

    private int GetLeaderPlayerIndex()
    {
        return _ships.OrderByDescending(s => s.Kills).ThenBy(s => s.PlayerIndex).First().PlayerIndex;
    }

    public void Draw(GameTime gameTime)
    {
        var device = _context.GraphicsDevice;
        device.SetRenderTarget(_context.SceneTarget);
        device.Clear(Color.Black);

        _context.Renderer.DrawWorld(_ships, _bullets, _asteroids, _debris, _elapsedTime);

        _context.PostProcess.Apply(_context.SpriteBatch, _context.SceneTarget, _elapsedTime, _context.RenderSettings);
        _context.Renderer.DrawLeaderHighlights(_ships, _elapsedTime);
        _context.Renderer.DrawMatchHud(_ships, _matchTimer);
        _context.Renderer.DrawShaderTuningHud(_context.RenderSettings);
    }

    private void UpdateRespawn(Ship ship, float dt)
    {
        if (ship.IsAlive)
            return;

        ship.RespawnTimer -= dt;
        if (ship.RespawnTimer > 0f)
            return;

        ship.ResetToSpawn();
        ship.ShieldActive = true;
        ship.ShieldActiveTimer = GameConstants.SpawnShieldDuration;
        ship.IsSpawnProtection = true;
        _context.Input.RequestSpinnerHeadingSync(ship.PlayerIndex);
    }

    private static void UpdateShield(Ship ship, bool shieldPressed, float dt)
    {
        if (ship.ShieldActive)
        {
            ship.ShieldActiveTimer -= dt;
            if (ship.ShieldActiveTimer <= 0f)
            {
                ship.ShieldActive = false;
                ship.ShieldSuppressed = false;
                ship.ShieldBreachTimer = 0f;
                ship.ShieldBreachBullet = null;
                if (!ship.IsSpawnProtection)
                    ship.ShieldCooldownTimer = GameConstants.ShieldCooldown;
                ship.IsSpawnProtection = false;
            }

            return;
        }

        if (ship.ShieldCooldownTimer > 0f)
        {
            ship.ShieldCooldownTimer -= dt;
            return;
        }

        if (shieldPressed && ship.IsAlive)
        {
            ship.ShieldActive = true;
            ship.ShieldActiveTimer = GameConstants.ShieldDuration;
            ship.IsSpawnProtection = false;
        }
    }
}
