using Swarmops.Logic.Swarm;

namespace Swarmops.Logic.Financial
{
    public interface IValidatable
    {
        void Validate (Person validator);
        void RetractValidation (Person retractor);
    }
}