namespace WSY.CertHub.Core;

public static class Utility
{

    public static async Task<byte[]> StreamGetBytes(Stream stream)
    {
        using MemoryStream memoryStream = new();
        await stream.CopyToAsync(memoryStream);
        return memoryStream.ToArray();
    }
}
