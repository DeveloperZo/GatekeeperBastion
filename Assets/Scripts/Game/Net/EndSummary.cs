using Unity.Netcode;

namespace GatekeepersProtocol.Game
{
    /// <summary>How the host run ended. Byte on the wire; names the three end screens.</summary>
    public enum RunEndKind : byte
    {
        Cleared = 0,
        Hatch = 1,
        Player = 2
    }

    /// <summary>One-shot payload for the client's end screen. Host numbers only — not a guest purse.</summary>
    public struct EndSummary : INetworkSerializable
    {
        public RunEndKind Kind;
        public long GrossEarned;
        public long RepairSpent;
        public long Net;
        public int DeepestWave;
        public long Banked;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            byte kind = (byte)Kind;
            serializer.SerializeValue(ref kind);
            Kind = (RunEndKind)kind;
            serializer.SerializeValue(ref GrossEarned);
            serializer.SerializeValue(ref RepairSpent);
            serializer.SerializeValue(ref Net);
            serializer.SerializeValue(ref DeepestWave);
            serializer.SerializeValue(ref Banked);
        }
    }
}
