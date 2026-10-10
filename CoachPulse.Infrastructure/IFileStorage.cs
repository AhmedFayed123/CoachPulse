using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace CoachPulse.Application.Interfaces
{
    public interface IFileStorage
    {
        Task<string> SaveAsync(
            Stream content,
            string folder,
            string extension,
            CancellationToken cancellationToken = default);

        Task<Stream?> OpenReadAsync(
            string storageKey,
            CancellationToken cancellationToken = default);

        Task DeleteAsync(
            string storageKey,
            CancellationToken cancellationToken = default);
    }
}
