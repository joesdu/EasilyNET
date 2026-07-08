namespace EasilyNET.Mongo.Core.Enums;

/// <summary>
///     <para xml:lang="en">Automatic vector quantization type for vector search indexes</para>
///     <para xml:lang="zh">向量搜索索引的自动向量量化类型</para>
/// </summary>
public enum EVectorQuantization
{
    /// <summary>
    ///     <para xml:lang="en">No quantization — index full-fidelity 32-bit float vectors. Highest accuracy, highest memory usage.</para>
    ///     <para xml:lang="zh">不量化 — 索引全精度 32 位浮点向量。精度最高，内存占用最大。</para>
    /// </summary>
    None = 0,

    /// <summary>
    ///     <para xml:lang="en">Scalar quantization — converts vectors to 8-bit integers, reducing memory usage by about 4x with minor accuracy loss.</para>
    ///     <para xml:lang="zh">标量量化 — 将向量转换为 8 位整数，内存占用约降低 4 倍，精度损失较小。</para>
    /// </summary>
    Scalar = 1,

    /// <summary>
    ///     <para xml:lang="en">Binary quantization — converts vectors to 1-bit representations, reducing memory usage by about 32x. Best for high-dimensional vectors from models trained for binary quantization.</para>
    ///     <para xml:lang="zh">二值量化 — 将向量转换为 1 位表示，内存占用约降低 32 倍。最适合针对二值量化训练的模型输出的高维向量。</para>
    /// </summary>
    Binary = 2
}
