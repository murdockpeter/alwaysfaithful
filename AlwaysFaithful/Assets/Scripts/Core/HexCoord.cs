using System;

namespace AlwaysFaithful.Core
{
    [Serializable]
    public struct HexCoord : IEquatable<HexCoord>
    {
        public int Q;
        public int R;

        public HexCoord(int q, int r)
        {
            Q = q;
            R = r;
        }

        public bool Equals(HexCoord other) => Q == other.Q && R == other.R;
        public override bool Equals(object obj) => obj is HexCoord other && Equals(other);
        public override int GetHashCode() => (Q * 397) ^ R;
        public override string ToString() => $"{ColumnName(Q)}{R + 1}";

        public static int Distance(HexCoord a, HexCoord b)
        {
            int ax = a.Q;
            int az = a.R - (a.Q - (a.Q & 1)) / 2;
            int ay = -ax - az;
            int bx = b.Q;
            int bz = b.R - (b.Q - (b.Q & 1)) / 2;
            int by = -bx - bz;
            return (Math.Abs(ax - bx) + Math.Abs(ay - by) + Math.Abs(az - bz)) / 2;
        }

        private static string ColumnName(int value)
        {
            string result = string.Empty;
            for (int index = value + 1; index > 0; index = (index - 1) / 26)
                result = (char)('A' + (index - 1) % 26) + result;
            return result;
        }
    }
}
