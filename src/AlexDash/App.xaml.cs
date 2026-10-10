using System.Windows;
using AlexDash.Audio;

namespace AlexDash;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // AlexDash.exe --export-voices <folder> : write Alex's voice lines as .wav files and quit.
        if (e.Args is ["--export-voices", var dir])
        {
            Voice.Export(dir);
            Shutdown();
            return;
        }

        new MainWindow().Show();
    }
}
