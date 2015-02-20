using EnergyProjects.Domain.Model;

namespace EnergyProjects.Domain.Services
{
    public abstract class IdGenerator
    {
        public abstract Id<T> NewId<T>(params object[] args);
    }
}