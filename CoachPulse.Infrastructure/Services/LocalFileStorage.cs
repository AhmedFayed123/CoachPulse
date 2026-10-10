
using CoachPulse.Application.Interfaces;
using Microsoft.Extensions.Hosting;

namespace CoachPulse.Infrastructure.Services
{
    public class LocalFileStorage : IFileStorage
    {
        private readonly string _rootPath;

        public LocalFileStorage(IHostEnvironment environment)
        {
            _rootPath = Path.GetFullPath(
                Path.Combine(
                    environment.ContentRootPath,
                    "App_Data",
                    "uploads"));

            Directory.CreateDirectory(_rootPath);
        }

        public async Task<string> SaveAsync(
            Stream content,
            string folder,
            string extension,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(folder))
                throw new ArgumentException("Folder is required.");

            if (extension is not ".jpg" and not ".png" and not ".webp")
                throw new ArgumentException("Unsupported file extension.");

            var safeFolder = string.Join(
                Path.DirectorySeparatorChar,
                folder.Split(
                    '/',
                    '\\',
                    StringSplitOptions.RemoveEmptyEntries));

            if (safeFolder.Split(Path.DirectorySeparatorChar)
                .Any(segment => segment is "." or ".."))
            {
                throw new ArgumentException("Invalid folder path.");
            }

            var fileName = $"{Guid.NewGuid():N}{extension}";
            var key = Path.Combine(safeFolder, fileName);

            var fullPath = GetFullPath(key);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

            await using var output = new FileStream(
                fullPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                useAsync: true);

            await content.CopyToAsync(output, cancellationToken);

            return key.Replace('\\', '/');
        }

        public Task<Stream?> OpenReadAsync(
            string storageKey,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var fullPath = GetFullPath(storageKey);

            if (!File.Exists(fullPath))
                return Task.FromResult<Stream?>(null);

            Stream stream = new FileStream(
                fullPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 81920,
                useAsync: true);

            return Task.FromResult<Stream?>(stream);
        }

        public Task DeleteAsync(
            string storageKey,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var fullPath = GetFullPath(storageKey);

            if (File.Exists(fullPath))
                File.Delete(fullPath);

            return Task.CompletedTask;
        }

        private string GetFullPath(string storageKey)
        {
            if (string.IsNullOrWhiteSpace(storageKey) ||
                Path.IsPathRooted(storageKey))
            {
                throw new ArgumentException("Invalid storage key.");
            }

            var normalizedKey = storageKey.Replace(
                '/',
                Path.DirectorySeparatorChar);

            var fullPath = Path.GetFullPath(
                Path.Combine(_rootPath, normalizedKey));

            var relativePath = Path.GetRelativePath(
                _rootPath,
                fullPath);

            if (relativePath == ".." ||
                relativePath.StartsWith(
                    $"..{Path.DirectorySeparatorChar}",
                    StringComparison.Ordinal) ||
                Path.IsPathRooted(relativePath))
            {
                throw new ArgumentException("Invalid storage key.");
            }

            return fullPath;
        }
    }
}
