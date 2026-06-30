namespace CasualtiesMiner.Dumper.Game;

internal sealed class AssetsParser : IDisposable
{
    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    ~AssetsParser()
    {
    }
}
