namespace TelegramMediaGrabberBot.DataStructures.Medias;

public record Media : IDisposable
{
    public Stream? Stream { get; set; }
    public Uri? Uri { get; set; }
    public MediaType? Type { get; set; }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (Stream != null)
        {
            Stream.Dispose();
            Stream = null;
        }

        Uri = null;
        Type = null;
    }
}