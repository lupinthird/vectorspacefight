using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace VectorSpaceFight.Game.Audio;

public sealed class ProceduralAudioSystem : IDisposable
{
    private const int SampleRate = 44100;
    private const int Channels = 1;
    private const int BufferSampleCount = 2048;

    private readonly ThrustVoice[] _thrustVoices = new ThrustVoice[4];
    private readonly ShieldVoice[] _shieldVoices = new ShieldVoice[4];
    private readonly SoundEffect _shootEffect;
    private readonly SoundEffect _rumbleEffect;
    private readonly SoundEffect _explosionEffect;
    private readonly SoundEffectInstance[] _shootInstances = new SoundEffectInstance[4];
    private readonly SoundEffectInstance _rumbleInstance;
    private readonly SoundEffectInstance _explosionInstance;

    public ProceduralAudioSystem()
    {
        for (int i = 0; i < _thrustVoices.Length; i++)
            _thrustVoices[i] = new ThrustVoice(SampleRate, Channels, BufferSampleCount);

        for (int i = 0; i < _shieldVoices.Length; i++)
            _shieldVoices[i] = new ShieldVoice(SampleRate, Channels, BufferSampleCount);

        _shootEffect = CreateShootEffect();
        _rumbleEffect = CreateRumbleEffect();
        _explosionEffect = CreateExplosionEffect();
        for (int i = 0; i < _shootInstances.Length; i++)
        {
            _shootInstances[i] = _shootEffect.CreateInstance();
            _shootInstances[i].Pan = GetPan(i);
        }

        _rumbleInstance = _rumbleEffect.CreateInstance();
        _explosionInstance = _explosionEffect.CreateInstance();
    }

    public void Update()
    {
        foreach (var voice in _thrustVoices)
            voice.Update();

        foreach (var voice in _shieldVoices)
            voice.Update();
    }

    public void UpdateThrust(int playerIndex, bool thrusting, bool active)
    {
        if (playerIndex < 0 || playerIndex >= _thrustVoices.Length)
            return;

        _thrustVoices[playerIndex].SetThrusting(thrusting && active);
    }

    public void UpdateShield(int playerIndex, bool shieldActive)
    {
        if (playerIndex < 0 || playerIndex >= _shieldVoices.Length)
            return;

        _shieldVoices[playerIndex].SetActive(shieldActive);
    }

    public void PlayShoot(int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= _shootInstances.Length)
            return;

        var instance = _shootInstances[playerIndex];
        if (instance.State == SoundState.Playing)
            instance.Stop();

        instance.Volume = 0.5f;
        instance.Pitch = 0f;
        instance.Play();
    }

    public void PlayExplosion()
    {
        if (_explosionInstance.State == SoundState.Playing)
            _explosionInstance.Stop();

        _explosionInstance.Volume = 1f;
        _explosionInstance.Pitch = 0f;
        _explosionInstance.Play();
    }

    public void PlayRumble()
    {
        if (_rumbleInstance.State == SoundState.Playing)
            _rumbleInstance.Stop();

        _rumbleInstance.Volume = 1f;
        _rumbleInstance.Pitch = 0f;
        _rumbleInstance.Play();
    }

    public void StopAll()
    {
        foreach (var voice in _thrustVoices)
            voice.StopImmediate();

        foreach (var voice in _shieldVoices)
            voice.StopImmediate();
    }

    public void Dispose()
    {
        StopAll();
        foreach (var voice in _thrustVoices)
            voice.Dispose();

        foreach (var voice in _shieldVoices)
            voice.Dispose();

        foreach (var instance in _shootInstances)
            instance.Dispose();

        _rumbleInstance.Dispose();
        _explosionInstance.Dispose();
        _shootEffect.Dispose();
        _rumbleEffect.Dispose();
        _explosionEffect.Dispose();
    }

    private static float GetPan(int playerIndex) => playerIndex switch
    {
        0 => -0.6f,
        1 => 0.6f,
        2 => -0.35f,
        3 => 0.35f,
        _ => 0f
    };

    private static SoundEffect CreateShootEffect()
    {
        const float duration = 0.1f;
        int sampleCount = (int)(SampleRate * duration);
        var samples = new byte[sampleCount * 2];

        var rng = new Random(90210);
        float filterState = 0f;
        const float filterCutoff = 0.12f;
        const float attackSeconds = 0.004f;
        int attackSamples = Math.Max(1, (int)(SampleRate * attackSeconds));

        for (int i = 0; i < sampleCount; i++)
        {
            float envelope = i < attackSamples
                ? 1f
                : MathF.Max(0f, 1f - (i - attackSamples) / (float)(sampleCount - attackSamples));

            float white = (float)(rng.NextDouble() * 2.0 - 1.0);
            filterState += filterCutoff * (white - filterState);
            float sample = filterState * envelope;

            WriteSample(samples, i, sample);
        }

        return new SoundEffect(samples, SampleRate, AudioChannels.Mono);
    }

    private static SoundEffect CreateRumbleEffect()
    {
        const float duration = 0.4f;
        const float attackSeconds = 0.12f;
        const float subFrequency = 52f;
        const float filterCutoff = 0.022f;
        const float maxVolume = 0.7f;

        int sampleCount = (int)(SampleRate * duration);
        var samples = new byte[sampleCount * 2];
        var rng = new Random(44001);
        float filterState = 0f;
        double phase = 0.0;
        double phaseStep = MathF.Tau * subFrequency / SampleRate;

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)SampleRate;
            float envelope = t < attackSeconds
                ? t / attackSeconds
                : MathF.Max(0f, 1f - (t - attackSeconds) / (duration - attackSeconds));

            float white = (float)(rng.NextDouble() * 2.0 - 1.0);
            filterState += filterCutoff * (white - filterState);
            float rumble = filterState * 0.72f + MathF.Sin((float)phase) * 0.28f;
            float sample = rumble * envelope * maxVolume;

            WriteSample(samples, i, sample);
            phase += phaseStep;
        }

        return new SoundEffect(samples, SampleRate, AudioChannels.Mono);
    }

    private static SoundEffect CreateExplosionEffect()
    {
        // Low-pass white noise with ADSR: instant attack, short decay, no sustain, long release.
        const float decaySeconds = 0.005f;
        const float releaseSeconds = 1.5f;
        const float decayLevel = 0.38f;
        const float noiseCutoff = 0.025f;
        const float maxVolume = 0.9f;

        float duration = decaySeconds + releaseSeconds;
        int sampleCount = (int)(SampleRate * duration);
        var samples = new byte[sampleCount * 2];
        var rng = new Random(77301);
        float filterState = 0f;

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)SampleRate;
            float envelope;
            if (t <= decaySeconds)
                envelope = MathHelper.Lerp(1f, decayLevel, t / decaySeconds);
            else
                envelope = MathHelper.Lerp(decayLevel, 0f, (t - decaySeconds) / releaseSeconds);

            float white = (float)(rng.NextDouble() * 2.0 - 1.0);
            filterState += noiseCutoff * (white - filterState);
            float sample = filterState * envelope * maxVolume;

            WriteSample(samples, i, sample);
        }

        return new SoundEffect(samples, SampleRate, AudioChannels.Mono);
    }

    private static void WriteSample(byte[] buffer, int index, float sample)
    {
        short pcm = (short)Math.Clamp(sample * short.MaxValue, short.MinValue, short.MaxValue);
        buffer[index * 2] = (byte)(pcm & 0xFF);
        buffer[index * 2 + 1] = (byte)((pcm >> 8) & 0xFF);
    }

    private sealed class ThrustVoice : IDisposable
    {
        private const float AttackSeconds = 0.12f;
        private const float ReleaseSeconds = 0.025f;
        private const float FilterCutoff = 0.035f;
        private const float MaxVolume = 0.55f;

        private readonly DynamicSoundEffectInstance _instance;
        private readonly int _bufferSampleCount;
        private readonly int _sampleRate;
        private readonly Random _rng = new();

        private bool _thrustRequested;
        private float _envelope;
        private float _filterState;

        public ThrustVoice(int sampleRate, int channels, int bufferSampleCount)
        {
            _sampleRate = sampleRate;
            _bufferSampleCount = bufferSampleCount;
            _instance = new DynamicSoundEffectInstance(sampleRate, (AudioChannels)channels);
        }

        public void SetThrusting(bool thrusting) => _thrustRequested = thrusting;

        public void StopImmediate()
        {
            _thrustRequested = false;
            _envelope = 0f;
            _instance.Stop();
        }

        public void Update()
        {
            if (_instance.State == SoundState.Stopped && !_thrustRequested && _envelope <= 0.001f)
                return;

            if (_thrustRequested && _instance.State == SoundState.Stopped)
            {
                SubmitBuffer(allowWhileStopped: true);
                SubmitBuffer(allowWhileStopped: true);
                _instance.Play();
            }

            if (_instance.State != SoundState.Playing)
                return;

            int submitted = 0;
            while (_instance.PendingBufferCount < 3 && submitted < 3)
            {
                SubmitBuffer();
                submitted++;
            }

            if (!_thrustRequested && _envelope <= 0.001f)
                _instance.Stop();
        }

        private void SubmitBuffer(bool allowWhileStopped = false)
        {
            if (_instance.State == SoundState.Stopped && !allowWhileStopped)
                return;

            var buffer = new byte[_bufferSampleCount * 2];
            float attackStep = MaxVolume / (AttackSeconds * _sampleRate);
            float releaseStep = MaxVolume / (ReleaseSeconds * _sampleRate);

            for (int i = 0; i < _bufferSampleCount; i++)
            {
                if (_thrustRequested)
                    _envelope = Math.Min(MaxVolume, _envelope + attackStep);
                else
                    _envelope = Math.Max(0f, _envelope - releaseStep);

                float white = (float)(_rng.NextDouble() * 2.0 - 1.0);
                _filterState += FilterCutoff * (white - _filterState);
                float sample = _filterState * _envelope;

                WriteSample(buffer, i, sample);
            }

            _instance.SubmitBuffer(buffer);
        }

        public void Dispose() => _instance.Dispose();
    }

    private sealed class ShieldVoice : IDisposable
    {
        // Two slightly detuned carriers beat together; same freq + π phase would cancel to silence.
        private const float FrequencyA = 220f;
        private const float FrequencyB = 222.5f;
        private const float BasePhaseOffset = MathF.PI * 0.35f;
        private const float PhaserLfoRate = 1.6f;
        private const float TremoloRate = 6f;
        private const float TremoloDepth = 0.5f;
        private const float RayGunSweepSeconds = 0.09f;
        private const float RayGunSweepRatio = 1.38f;
        private const float ReleaseSeconds = 0.04f;
        private const float MaxVolume = 0.35f;

        private readonly DynamicSoundEffectInstance _instance;
        private readonly int _bufferSampleCount;
        private readonly int _sampleRate;

        private bool _activeRequested;
        private float _envelope;
        private float _activeSeconds;
        private double _phaseA;
        private double _phaseB;
        private double _lfoPhase;
        private double _tremoloPhase;

        public ShieldVoice(int sampleRate, int channels, int bufferSampleCount)
        {
            _sampleRate = sampleRate;
            _bufferSampleCount = bufferSampleCount;
            _instance = new DynamicSoundEffectInstance(sampleRate, (AudioChannels)channels);
        }

        public void SetActive(bool active)
        {
            if (active && !_activeRequested)
            {
                _envelope = MaxVolume;
                _activeSeconds = 0f;
            }

            _activeRequested = active;
        }

        public void StopImmediate()
        {
            _activeRequested = false;
            _envelope = 0f;
            _activeSeconds = 0f;
            _instance.Stop();
        }

        public void Update()
        {
            if (_instance.State == SoundState.Stopped && !_activeRequested && _envelope <= 0.001f)
                return;

            if (_activeRequested && _instance.State == SoundState.Stopped)
            {
                SubmitBuffer(allowWhileStopped: true);
                SubmitBuffer(allowWhileStopped: true);
                _instance.Play();
            }

            if (_instance.State != SoundState.Playing)
                return;

            int submitted = 0;
            while (_instance.PendingBufferCount < 3 && submitted < 3)
            {
                SubmitBuffer();
                submitted++;
            }

            if (!_activeRequested && _envelope <= 0.001f)
                _instance.Stop();
        }

        private void SubmitBuffer(bool allowWhileStopped = false)
        {
            if (_instance.State == SoundState.Stopped && !allowWhileStopped)
                return;

            var buffer = new byte[_bufferSampleCount * 2];
            float releaseStep = MaxVolume / (ReleaseSeconds * _sampleRate);
            float lfoStep = MathF.Tau * PhaserLfoRate / _sampleRate;
            float tremoloStep = MathF.Tau * TremoloRate / _sampleRate;

            for (int i = 0; i < _bufferSampleCount; i++)
            {
                if (_activeRequested)
                {
                    _envelope = MaxVolume;
                    _activeSeconds += 1f / _sampleRate;
                }
                else
                {
                    _envelope = Math.Max(0f, _envelope - releaseStep);
                }

                float sweep = _activeSeconds < RayGunSweepSeconds
                    ? MathHelper.Lerp(RayGunSweepRatio, 1f, _activeSeconds / RayGunSweepSeconds)
                    : 1f;

                float freqA = FrequencyA * sweep;
                float freqB = FrequencyB * sweep;
                double phaseStepA = MathF.Tau * freqA / _sampleRate;
                double phaseStepB = MathF.Tau * freqB / _sampleRate;

                _lfoPhase += lfoStep;
                _tremoloPhase += tremoloStep;
                float phaserMod = MathF.Sin((float)_lfoPhase) * MathF.PI * 0.55f;
                float stereoMix = (MathF.Sin((float)(_lfoPhase * 0.65)) + 1f) * 0.5f;
                float tremolo = 1f - TremoloDepth + TremoloDepth * (0.5f + 0.5f * MathF.Sin((float)_tremoloPhase));

                float waveA = MathF.Sin((float)_phaseA);
                float waveB = MathF.Sin((float)(_phaseB + BasePhaseOffset + phaserMod));
                float harmonic = MathF.Sin((float)(_phaseA * 2.0)) * 0.1f;

                float mixed = waveA * (0.55f + stereoMix * 0.45f)
                            + waveB * (0.55f + (1f - stereoMix) * 0.45f);
                float sample = (mixed * 0.42f + harmonic) * _envelope * tremolo;

                WriteSample(buffer, i, sample);

                _phaseA += phaseStepA;
                _phaseB += phaseStepB;
            }

            _instance.SubmitBuffer(buffer);
        }

        public void Dispose() => _instance.Dispose();
    }
}
