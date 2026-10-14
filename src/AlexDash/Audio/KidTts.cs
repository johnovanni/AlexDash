using System.IO;
using System.Speech.AudioFormat;
using System.Speech.Synthesis;
using AlexDash.Game;

namespace AlexDash.Audio;

/// <summary>
/// Fallback voice lines made with Windows text-to-speech. TTS voices largely ignore pitch requests,
/// so each line is pitch-shifted here: the voice is synthesized slowly, then sped up by resampling,
/// which raises the pitch into a kid's range.
/// </summary>
public static class KidTts
{
    private const int SampleRate = 22050;
    private const string StupidText = "I'm not stupid, you stupid.";

    /// <summary>Renders every line as a WAV file's bytes (22.05 kHz, 16-bit mono).</summary>
    public static Dictionary<VoiceLine, byte[]> RenderAll()
    {
        using var synth = new SpeechSynthesizer();
        try { synth.SelectVoiceByHints(VoiceGender.Male, VoiceAge.Child); } catch { /* keep default voice */ }

        return new()
        {
            [VoiceLine.TwennyWon] = Kid(synth, "Twenny won!", 2.5),
            [VoiceLine.NotStupid] = Kid(synth, StupidText, 2.5),
            [VoiceLine.NotStupidHigh] = Kid(synth, StupidText, 2.9),
            [VoiceLine.NotStupidLow] = Kid(synth, StupidText, 2.1),
            [VoiceLine.HawkCry] = HawkCry(synth),
            [VoiceLine.Waah] = Waah(synth),
            [VoiceLine.Hackers] = Kid(synth, "Hackers!", 2.7),
        };
    }

    /// <summary>A kid's voice: raise the pitch by <paramref name="pitch"/>, mostly undoing the speed-up.</summary>
    private static byte[] Kid(SpeechSynthesizer synth, string text, double pitch)
    {
        // Each SAPI rate step is roughly 11.6% faster/slower. Undo ~80% of the resample speed-up,
        // leaving the kid talking a little fast.
        int rate = -(int)Math.Round(0.8 * Math.Log(pitch) / Math.Log(1.116));
        var pcm = Synthesize(synth, text, rate);
        return ToWav(Resample(pcm, _ => pitch));
    }

    /// <summary>
    /// A kid imitating a hawk's screech: a stretched "Keeee-yaaahhh" pitched way up,
    /// holding high, then diving down with a warble.
    /// </summary>
    private static byte[] HawkCry(SpeechSynthesizer synth)
    {
        var pcm = Synthesize(synth, "Keeeeeee yaaaaaaahhhhh!", -9);
        return ToWav(Resample(pcm, progress =>
        {
            double sweep = progress < 0.3 ? 3.6 : 3.6 - (progress - 0.3) / 0.7 * 1.6;
            double warble = 1 + 0.04 * Math.Sin(progress * Math.PI * 2 * 9);
            return sweep * warble;
        }));
    }

    /// <summary>A kid bawling: a long "Waaaahh" that wobbles like sobbing and sags at the end.</summary>
    private static byte[] Waah(SpeechSynthesizer synth)
    {
        var pcm = Synthesize(synth, "Waaaaaaahhh!", -7);
        return ToWav(Resample(pcm, progress =>
        {
            double sag = progress < 0.4 ? 2.9 : 2.9 - (progress - 0.4) / 0.6 * 0.7;
            double sob = 1 + 0.07 * Math.Sin(progress * Math.PI * 2 * 7);
            return sag * sob;
        }));
    }

    private static float[] Synthesize(SpeechSynthesizer synth, string text, int rate)
    {
        using var ms = new MemoryStream();
        synth.Rate = Math.Clamp(rate, -10, 10);
        synth.SetOutputToAudioStream(ms, new SpeechAudioFormatInfo(SampleRate, AudioBitsPerSample.Sixteen, AudioChannel.Mono));
        synth.Speak(text);
        synth.SetOutputToNull();

        var bytes = ms.ToArray();
        var samples = new float[bytes.Length / 2];
        for (int i = 0; i < samples.Length; i++)
            samples[i] = BitConverter.ToInt16(bytes, i * 2) / 32768f;
        return TrimSilence(samples);
    }

    private static float[] TrimSilence(float[] s)
    {
        const float threshold = 0.01f;
        int start = 0, end = s.Length - 1;
        while (start < end && Math.Abs(s[start]) < threshold) start++;
        while (end > start && Math.Abs(s[end]) < threshold) end--;
        return s[start..(end + 1)];
    }

    /// <summary>
    /// Plays the input back faster by <paramref name="ratioAt"/>(progress 0..1), raising pitch by
    /// the same factor. Low-passes first so the shifted-up highs don't alias into hiss.
    /// </summary>
    private static float[] Resample(float[] input, Func<double, double> ratioAt)
    {
        double maxRatio = 1;
        for (int i = 0; i <= 20; i++) maxRatio = Math.Max(maxRatio, ratioAt(i / 20.0));
        var filtered = LowPass(LowPass(input, 0.9 * SampleRate / 2 / maxRatio), 0.9 * SampleRate / 2 / maxRatio);

        var output = new List<float>(input.Length);
        for (double pos = 0; pos < filtered.Length - 1; pos += ratioAt(pos / filtered.Length))
        {
            int i = (int)pos;
            double frac = pos - i;
            output.Add((float)(filtered[i] * (1 - frac) + filtered[i + 1] * frac));
        }
        return Normalize(output.ToArray(), 0.9f);
    }

    /// <summary>RBJ biquad low-pass.</summary>
    private static float[] LowPass(float[] x, double cutoff)
    {
        double w = 2 * Math.PI * cutoff / SampleRate, alpha = Math.Sin(w) / (2 * 0.7071), cos = Math.Cos(w);
        double a0 = 1 + alpha;
        double b0 = (1 - cos) / 2 / a0, b1 = (1 - cos) / a0, b2 = b0, a1 = -2 * cos / a0, a2 = (1 - alpha) / a0;

        var y = new float[x.Length];
        double x1 = 0, x2 = 0, y1 = 0, y2 = 0;
        for (int i = 0; i < x.Length; i++)
        {
            double v = b0 * x[i] + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2;
            x2 = x1; x1 = x[i]; y2 = y1; y1 = v;
            y[i] = (float)v;
        }
        return y;
    }

    private static float[] Normalize(float[] s, float peak)
    {
        float max = s.Length == 0 ? 0 : s.Max(Math.Abs);
        if (max < 1e-6f) return s;
        float gain = peak / max;
        for (int i = 0; i < s.Length; i++) s[i] *= gain;
        return s;
    }

    private static byte[] ToWav(float[] samples)
    {
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);
        int dataLen = samples.Length * 2;
        w.Write("RIFF"u8); w.Write(36 + dataLen); w.Write("WAVE"u8);
        w.Write("fmt "u8); w.Write(16); w.Write((short)1); w.Write((short)1);
        w.Write(SampleRate); w.Write(SampleRate * 2); w.Write((short)2); w.Write((short)16);
        w.Write("data"u8); w.Write(dataLen);
        foreach (var s in samples) w.Write((short)Math.Clamp(s * 32767, -32768, 32767));
        w.Flush();
        return ms.ToArray();
    }
}
