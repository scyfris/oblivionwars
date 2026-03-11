using Godot;

public static class EnumStringNames<T> where T : struct, System.Enum
{
    private static readonly StringName[] _names = Build();

    private static StringName[] Build()
    {
        var values = System.Enum.GetValues<T>();
        int max = 0;
        foreach (var v in values)
        {
            int i = System.Convert.ToInt32(v);
            if (i > max) max = i;
        }

        var names = new StringName[max + 1];
        foreach (var v in values)
        {
            int i = System.Convert.ToInt32(v);
            names[i] = new StringName(System.Enum.GetName(v));
        }
        return names;
    }

    public static StringName Get(T value)
    {
        int i = System.Convert.ToInt32(value);
        return _names[i];
    }
}
