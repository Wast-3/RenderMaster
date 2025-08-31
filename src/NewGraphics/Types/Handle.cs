namespace RenderMaster.src.NewGraphics.Types
{
    readonly record struct Handle<T>(int Id)
    {
        public bool IsValid => Id >= 0;
        public static Handle<T> Invalid => new(-1);
        public override string ToString() => IsValid ? $"{typeof(T).Name}#{Id}" : $"{typeof(T).Name}<Invalid>";
    }
}

