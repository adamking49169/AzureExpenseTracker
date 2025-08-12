using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace ExpenseTracker.Web.Services;

public class ReceiptStorage
{
    private readonly BlobServiceClient _svc;
    private readonly string _container;

    public ReceiptStorage(BlobServiceClient svc, IConfiguration cfg)
    {
        _svc = svc;
        _container = cfg["Blob:Container"] ?? "receipts";
    }

    public async Task<string> UploadAsync(Stream file, string fileName, string contentType, CancellationToken ct)
    {
        var container = _svc.GetBlobContainerClient(_container);
        await container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: ct);
        var blob = container.GetBlobClient($"{Guid.NewGuid()}-{fileName}");
        await blob.UploadAsync(file, new BlobHttpHeaders { ContentType = contentType }, cancellationToken: ct);
        return blob.Uri.ToString();
    }
}
