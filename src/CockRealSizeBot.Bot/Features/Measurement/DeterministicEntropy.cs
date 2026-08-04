using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace CockRealSizeBot.Bot.Features.Measurement;

/// <summary>
/// Источник «случайности», который на самом деле полностью детерминирован:
/// HMAC-SHA256(соль, seed) даёт 32 байта, которые мы читаем как четыре
/// независимых 64-битных значения.
/// </summary>
/// <remarks>
/// Почему HMAC, а не <c>string.GetHashCode</c>: GetHashCode рандомизирован
/// между запусками процесса, то есть после перезапуска бота у всех сменились бы
/// результаты. HMAC стабилен всегда и при этом не даёт восстановить соль по выдаче.
/// </remarks>
internal readonly record struct DeterministicEntropy(ulong First, ulong Second, ulong Third, ulong Fourth)
{
    public static DeterministicEntropy Derive(string salt, string seed)
    {
        Span<byte> hash = stackalloc byte[HMACSHA256.HashSizeInBytes];
        HMACSHA256.HashData(Encoding.UTF8.GetBytes(salt), Encoding.UTF8.GetBytes(seed), hash);

        return new DeterministicEntropy(
            BinaryPrimitives.ReadUInt64LittleEndian(hash[..8]),
            BinaryPrimitives.ReadUInt64LittleEndian(hash[8..16]),
            BinaryPrimitives.ReadUInt64LittleEndian(hash[16..24]),
            BinaryPrimitives.ReadUInt64LittleEndian(hash[24..]));
    }

    /// <summary>
    /// Переводит 64-битное значение в равномерное число из [0, 1).
    /// Старшие 53 бита — ровно столько, сколько double представляет без потерь.
    /// </summary>
    public static double ToUnitInterval(ulong value) => (value >> 11) * (1.0 / (1UL << 53));
}
