using System.Collections.Generic;

namespace FrontierDepths.Combat
{
    public interface IStatModifierSource
    {
        IEnumerable<StatModifier> GetModifiers();
    }
}
