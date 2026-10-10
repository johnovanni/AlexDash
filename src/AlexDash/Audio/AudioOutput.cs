using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace AlexDash.Audio;

/// <summary>
/// One audio stream that stays open for the whole game, playing silence between sounds.
/// Bluetooth speakers (and some USB/HDMI outputs) power down when nothing is playing and clip
/// the start of the next sound while they wake up; a stream that never stops keeps them awake.
/// Plays one clip at a time: starting a clip cuts off the current one.
/// </summary>
public sealed class AudioOutput : IDisposable
{
    public static readonly WaveFormat Format = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);

    private readonly MixingSampleProvider _mixer = new(Format) { ReadFully = true }; // silence when empty
    private WaveOutEvent? _device;
    private Clip? _current;

    public AudioOutput() => Open();

    public bool IsBusy => _current is { Finished: false };

    /// <param name="samples">Interleaved stereo samples in <see cref="Format"/>.</param>
    public void Play(float[] samples)
    {
        EnsureOpen();
        if (_current != null) _mixer.RemoveMixerInput(_current);
        _current = new Clip(samples);
        _mixer.AddMixerInput(_current);
    }

    public void Dispose()
    {
        _device?.Dispose();
        _device = null;
    }

    private void EnsureOpen()
    {
        // The device can drop out (e.g. the Bluetooth speaker disconnects); reopen on the next sound.
        if (_device?.PlaybackState == PlaybackState.Playing) return;
        _device?.Dispose();
        Open();
    }

    private void Open()
    {
        try
        {
            _device = new WaveOutEvent { DesiredLatency = 120 };
            _device.Init(_mixer);
            _device.Play();
        }
        catch
        {
            // No usable audio device: sounds are silently dropped.
            _device?.Dispose();
            _device = null;
        }
    }

    /// <summary>Feeds one clip into the mixer; the mixer drops it once it runs out.</summary>
    private sealed class Clip(float[] samples) : ISampleProvider
    {
        private int _position;
        private volatile bool _finished;

        public bool Finished => _finished;
        public WaveFormat WaveFormat => Format;

        public int Read(float[] buffer, int offset, int count)
        {
            int n = Math.Min(count, samples.Length - _position);
            Array.Copy(samples, _position, buffer, offset, n);
            _position += n;
            if (_position >= samples.Length) _finished = true;
            return n;
        }
    }
}
