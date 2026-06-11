namespace CasualtiesMiner.Shared.Models;

public sealed partial class LiquidType
{
    /// <summary>
    /// Dictionary key in <c>Liquids.Registry</c> (e.g. <c>icetea</c>). Differs from <see cref="localeName"/> (e.g. <c>icedtea</c>).
    /// </summary>
    public string registryId = "";
}
