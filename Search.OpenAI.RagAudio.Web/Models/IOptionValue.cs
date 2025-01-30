namespace Search.OpenAI.RagAudio.Web.Models;

public interface IOptionValue
{
    string Value { get; }

    string Name { get; }

    bool IsDefault { get; }
}

public static class IOptionValueExtensions
{
    public static IOptionValue[] ToOptionValues<T>(
        this IEnumerable<T> values,
        Func<T, string> valueFactory,
        Func<T, string> nameFactory,
        Func<T, bool> isDefault)
    {
        return
        [
            ..values.Select(value => new OptionValue(
                valueFactory(value),
                nameFactory(value),
                isDefault(value)))
        ];
    }
}

file sealed class OptionValue(string value, string name, bool isDefault) : IOptionValue
{
    string IOptionValue.Value { get; } = value;
    string IOptionValue.Name { get; } = name;
    bool IOptionValue.IsDefault { get; } = isDefault;
}