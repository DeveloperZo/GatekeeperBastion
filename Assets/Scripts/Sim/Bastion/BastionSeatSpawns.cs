namespace GatekeepersProtocol.Sim
{
    /// <summary>Which spawn mouths stay live at a seat count. Author the layout for three;
    /// two-seat runs mute the minSeats=3 marks. Hatch open/sealed is <see cref="ShipHatches"/>.</summary>
    public static class BastionSeatSpawns
    {
        public static bool IsLive(int minSeats, int seatCount)
        {
            int need = minSeats < 1 ? 1 : minSeats;
            int seats = seatCount < 1 ? 1 : seatCount;
            return seats >= need;
        }
    }
}
