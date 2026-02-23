using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NettoyerPc.Core;

namespace NettoyerPc.Modules
{
    public interface ICleaningModule
    {
        string Name { get; }
        List<CleaningStep> GetSteps(CleaningMode mode);
        Task ExecuteStepAsync(CleaningStep step, CancellationToken cancellationToken);
    }
}
