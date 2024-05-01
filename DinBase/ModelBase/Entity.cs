using DibBase.Extensions;

namespace DibBase.ModelBase;

public abstract class Entity
{
    long id;
    public long Id
    {
        get => id;
        set
        {
            if (id != default) //clumsy workaround to access EF-set value
                Guid = this.Obfuscate();

            id = value;
            if (id != default) //for user setting
                Guid = this.Obfuscate();
        }
    }

    public Guid Guid { get; set; }
}
