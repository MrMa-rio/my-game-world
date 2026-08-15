using MyGameWorld.Shared.Core;

namespace MyGameWorld.Shared.NpcCognition
{
    public readonly struct NpcBrainContext
    {
        public NpcBrainContext(ulong simulationTick, SimulationLod simulationLod)
        {
            SimulationTick = simulationTick;
            SimulationLod = simulationLod;
        }

        public ulong SimulationTick { get; }

        public SimulationLod SimulationLod { get; }
    }
}
