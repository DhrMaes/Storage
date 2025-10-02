namespace DhrMaes.Storage.Core.FileSystem
{
    using System.Diagnostics.CodeAnalysis;

    public struct FileSize : IEquatable<FileSize>, IComparable<FileSize>
    {
        public static readonly FileSize Empty = new FileSize(0);
        public static readonly FileSize Unknown = new FileSize(-1);

        private static readonly List<string> Units = new List<string>
        {
            "B",
            "kB",
            "MB",
            "GB",
            "TB",
        };

        public FileSize(long size)
        {
            Size = size;
        }

        public long Size { get; }

        // Implicit conversions
        public static implicit operator FileSize(long size) => new FileSize(size);
        public static implicit operator long(FileSize fs) => fs.Size;

        // Comparison operators
        public static bool operator ==(FileSize left, FileSize right) => left.Size == right.Size;
        public static bool operator !=(FileSize left, FileSize right) => left.Size != right.Size;
        public static bool operator <(FileSize left, FileSize right) => left.Size < right.Size;
        public static bool operator >(FileSize left, FileSize right) => left.Size > right.Size;
        public static bool operator <=(FileSize left, FileSize right) => left.Size <= right.Size;
        public static bool operator >=(FileSize left, FileSize right) => left.Size >= right.Size;

        // Arithmetic operators
        public static FileSize operator +(FileSize left, FileSize right) => new FileSize(left.Size + right.Size);
        public static FileSize operator -(FileSize left, FileSize right) => new FileSize(left.Size - right.Size);
        public static FileSize operator *(FileSize left, long factor) => new FileSize(left.Size * factor);
        public static FileSize operator /(FileSize left, long divisor) => new FileSize(left.Size / divisor);

        public override string ToString()
        {
            if (Size < 0) return "Unknown";
            if (Size == 0) return "0 B";

            double value = Size;
            int unitIndex = 0;

            // Always climb until the value is under 1000 or we run out of units
            while (value >= 1024 && unitIndex < Units.Count - 1)
            {
                value /= 1024.0;
                unitIndex++;
            }

            // Two decimals if needed, but strip trailing zeros
            return $"{value:0.##} {Units[unitIndex]}";
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return obj is FileSize && Equals((FileSize)obj);
        }

        public override int GetHashCode()
        {
            return Size.GetHashCode();
        }

        public bool Equals(FileSize other)
        {
            return Size == other.Size;
        }


        public int CompareTo(FileSize other)
        {
            return Size.CompareTo(other.Size);
        }
    }
}
