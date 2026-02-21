using System.Collections.Generic;

/// <summary>
/// Static registry of all available hero powers (T114).
/// Returns fresh instances for each call to prevent shared state.
/// </summary>
public static class HeroPowerDatabase
{
    /// <summary>Get all available hero powers (fresh instances).</summary>
    public static List<HeroPowerBase> GetAllPowers()
    {
        return new List<HeroPowerBase>
        {
            // Swords (2)
            new BladeMasterPower(),
            new WarChiefPower(),
            // Cups (2)
            new HealerPower(),
            new LifeTapPower(),
            // Pentacles (2)
            new MidasTouchPower(),
            new FortifyPower(),
            // Wands (2)
            new ArcaneBoltPower(),
            new EmpowerPower(),
            // Neutral (4)
            new RecruiterPower(),
            new EconomistPower(),
            new TacticianPower(),
            new RerollerPower()
        };
    }

    /// <summary>Get a hero power by name.</summary>
    public static HeroPowerBase GetByName(string name)
    {
        foreach (var power in GetAllPowers())
        {
            if (power.PowerName == name)
                return power;
        }
        return null;
    }
}
