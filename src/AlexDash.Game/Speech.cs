namespace AlexDash.Game;

public enum VoiceLine { TwennyWon, NotStupid, NotStupidHigh, NotStupidLow, HawkCry }

/// <summary>Something an Alex says: shown as a speech bubble and played as a voice line.</summary>
/// <param name="Interrupt">Cut off whatever is playing, rather than being skipped if busy.</param>
public sealed record Speech(Alex Speaker, string Text, VoiceLine Line, bool Interrupt = false);
