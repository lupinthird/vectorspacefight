using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using VectorSpaceFight.Config;
using VectorSpaceFight.Game.Audio;
using VectorSpaceFight.Game.Constants;
using VectorSpaceFight.Game.Entities;
using VectorSpaceFight.Game.Rendering;
using VectorSpaceFight.Game.Shaders;
using VectorSpaceFight.Game.States;
using VectorSpaceFight.Game.Systems;

namespace VectorSpaceFight.Game;

public class VectorSpaceFightGame : Microsoft.Xna.Framework.Game
{
    private readonly GameConfig _config;
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch = null!;
    private LineBatch _lineBatch = null!;
    private GameRenderer _renderer = null!;
    private PostProcessEffect _postProcess = null!;
    private RenderTarget2D _sceneTarget = null!;
    private ProceduralAudioSystem _audio = null!;
    private RenderSettings _renderSettings = null!;
    private ShaderTuningInput _shaderTuningInput = null!;
    private InputSystem _inputSystem = null!;
    private GameContext _context = null!;

    private IGameState _currentState = null!;
    private MenuState _menuState = null!;
    private PlayingState _playingState = null!;
    private ResultsState _resultsState = null!;
    private Ship[] _lastResults = Array.Empty<Ship>();

    public VectorSpaceFightGame()
    {
        _config = GameConfig.Load();
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = false;

        if (_config.Display.Force60Hz)
        {
            IsFixedTimeStep = true;
            TargetElapsedTime = TimeSpan.FromSeconds(1.0 / 60.0);
        }
    }

    protected override void Initialize()
    {
        ConfigureDisplay();
        base.Initialize();
    }

    private void ConfigureDisplay()
    {
        var display = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
        int width = _config.Display.Width;
        int height = _config.Display.Height;

        if (width <= 0 || height <= 0)
        {
            width = display.Width;
            height = width * 9 / 16;
            if (height > display.Height)
            {
                height = display.Height;
                width = height * 16 / 9;
            }
        }

        _graphics.HardwareModeSwitch = false;
        _graphics.IsFullScreen = _config.Display.Fullscreen;
        _graphics.SynchronizeWithVerticalRetrace = _config.Display.Vsync;
        _graphics.PreferredBackBufferWidth = width;
        _graphics.PreferredBackBufferHeight = height;
        _graphics.ApplyChanges();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _lineBatch = new LineBatch(GraphicsDevice);
        _renderer = new GameRenderer(_lineBatch);
        _sceneTarget = new RenderTarget2D(
            GraphicsDevice,
            GameConstants.WorldWidth,
            GameConstants.WorldHeight,
            false,
            SurfaceFormat.Color,
            DepthFormat.None);

        _renderSettings = new RenderSettings();
        _renderSettings.ApplyFromConfig(_config.Rendering);

        Effect? postShader = null;
        if (_renderSettings.PostProcessEnabled)
            postShader = Content.Load<Effect>("Shaders/CRT");

        _postProcess = new PostProcessEffect(postShader, GraphicsDevice);
        _shaderTuningInput = new ShaderTuningInput();
        _lineBatch.SetRenderSettings(_renderSettings);
        _audio = new ProceduralAudioSystem();
        _inputSystem = new InputSystem();

        _context = new GameContext
        {
            Config = _config,
            GraphicsDevice = GraphicsDevice,
            Content = Content,
            SpriteBatch = _spriteBatch,
            LineBatch = _lineBatch,
            Renderer = _renderer,
            PostProcess = _postProcess,
            RenderSettings = _renderSettings,
            SceneTarget = _sceneTarget,
            Audio = _audio,
            Game = this,
            Input = _inputSystem
        };

        _menuState = new MenuState(_context, StartMatch);
        _playingState = new PlayingState(_context, EndMatch);
        _resultsState = new ResultsState(_context, _lastResults, StartMatch, ReturnToMenu);

        ChangeState(_menuState);
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
            Keyboard.GetState().IsKeyDown(Keys.Escape))
        {
            Exit();
        }

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _inputSystem.Update(dt);
        _shaderTuningInput.Update(_renderSettings);
        _renderSettings.Update(dt);

        _currentState.Update(gameTime);
        _inputSystem.CaptureStartFrame();
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        _currentState.Draw(gameTime);
        base.Draw(gameTime);
    }

    protected override void UnloadContent()
    {
        _lineBatch.Dispose();
        _postProcess.Dispose();
        _sceneTarget.Dispose();
        _audio.Dispose();
        base.UnloadContent();
    }

    private void StartMatch()
    {
        ChangeState(_playingState);
    }

    private void EndMatch(Ship[] ships)
    {
        _lastResults = ships;
        _resultsState = new ResultsState(_context, ships, StartMatch, ReturnToMenu);
        ChangeState(_resultsState);
    }

    private void ReturnToMenu()
    {
        ChangeState(_menuState);
    }

    private void ChangeState(IGameState newState)
    {
        _currentState?.Exit();
        _currentState = newState;
        _currentState.Enter();
    }
}
