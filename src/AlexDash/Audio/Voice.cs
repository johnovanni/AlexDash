using System.IO;
using AlexDash.Game;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace AlexDash.Audio;

/// <summary>
/// Alex's voice. Each line plays from a recorded Sounds\{line}.wav next to the game when one exists
/// (any PCM or float WAV, mono or stereo, any sample rate). Any missing line falls back to
/// pitch-shifted text-to-speech (see <see cref="KidTts"/>). Lines don't overlap: a new line is
/// skipped while one is playing, unless it interrupts.
/// </summary>
public sealed class Voice : IDisposable
{
    private readonly AudioOutput _output = new();
    private readonly Dictionary<VoiceLine, float[]> _clips = [];
    private readonly object _lock = new();

    public Voice()
    {
        LoadRecordings();
        if (Enum.GetValues<VoiceLine>().Any(line => !_clips.ContainsKey(line)))
            Task.Run(RenderMissing);
    }

    /// <param name="interrupt">Cut off whatever is playing. Otherwise the line is skipped if busy.</param>
    public void Play(VoiceLine line, bool interrupt = false)
    {
        float[]? clip;
        lock (_lock) _clips.TryGetValue(line, out clip);
        if (clip is null) return; // still rendering, or no audio available
        if (!interrupt && _output.IsBusy) return;

        try
        {
            _output.Play(clip);
        }
        catch
        {
            // Audio problems should never crash the game.
        }
    }

    /// <summary>Writes every text-to-speech line as a .wav file, for tuning by ear.</summary>
    public static void Export(string dir)
    {
        Directory.CreateDirectory(dir);
        foreach (var (line, wav) in KidTts.RenderAll())
            File.WriteAllBytes(Path.Combine(dir, $"{line}.wav"), wav);
    }

    public void Dispose() => _output.Dispose();

    private void LoadRecordings()
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "Sounds");
        foreach (var line in Enum.GetValues<VoiceLine>())
        {
            try
            {
                var path = Path.Combine(dir, $"{line}.wav");
                if (File.Exists(path)) _clips[line] = Decode(File.ReadAllBytes(path));
            }
            catch
            {
                // Unreadable file: fall back to text-to-speech for this line.
            }
        }
    }

    private void RenderMissing()
    {
        try
        {
            var rendered = KidTts.RenderAll();
            lock (_lock)
                foreach (var (line, wav) in rendered)
                    if (!_clips.ContainsKey(line)) _clips[line] = Decode(wav);
        }
        catch
        {
            // No TTS on this machine: the missing lines stay silent.
        }
    }

    /// <summary>Decodes a WAV file into the output's format. Throws if it can't be read.</summary>
    private static float[] Decode(byte[] wav)
    {
        using var reader = new WaveFileReader(new MemoryStream(wav));
        ISampleProvider source = reader.ToSampleProvider();
        source = source.WaveFormat.Channels switch
        {
            1 => new MonoToStereoSampleProvider(source),
            2 => source,
            _ => throw new NotSupportedException("Only mono or stereo WAVs are supported."),
        };
        if (source.WaveFormat.SampleRate != AudioOutput.Format.SampleRate)
            source = new WdlResamplingSampleProvider(source, AudioOutput.Format.SampleRate);

        var samples = new List<float>();
        var buffer = new float[8192];
        int read;
        while ((read = source.Read(buffer, 0, buffer.Length)) > 0)
            samples.AddRange(new ArraySegment<float>(buffer, 0, read));
        return [.. samples];
    }
}
