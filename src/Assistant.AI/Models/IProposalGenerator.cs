using System.Threading;
using System.Threading.Tasks;
using Assistant.AI.Models;
using Assistant.Core.Model;

namespace Assistant.AI.Abstractions
{
    public interface IProposalGenerator
    {
        Task<Proposal> GenerateAsync(GenerationRequest request, CancellationToken cancellationToken);
    }
}
