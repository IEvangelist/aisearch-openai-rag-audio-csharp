namespace Search.OpenAI.RagAudio.Types;

[IJSWrapperConverter]
public class Int8Array : TypedArray<byte, Int8Array>, IJSCreatable<Int8Array>
{
    /// <inheritdoc/>
    public static Task<Int8Array> CreateAsync(IJSRuntime jSRuntime, IJSObjectReference jSReference)
    {
        return Task.FromResult(new Int8Array(jSRuntime, jSReference, new()));
    }

    /// <inheritdoc/>
    public static Task<Int8Array> CreateAsync(IJSRuntime jSRuntime, IJSObjectReference jSReference, CreationOptions options)
    {
        return Task.FromResult(new Int8Array(jSRuntime, jSReference, options));
    }

    /// <inheritdoc cref="CreateAsync(IJSRuntime, IJSObjectReference, CreationOptions)"/>
    protected Int8Array(IJSRuntime jSRuntime, IJSObjectReference jSReference, CreationOptions options) : base(jSRuntime, jSReference, options) { }
}

[IJSWrapperConverter]
public class Int16Array : TypedArray<short, Int16Array>, IJSCreatable<Int16Array>
{
    /// <inheritdoc/>
    public static Task<Int16Array> CreateAsync(IJSRuntime jSRuntime, IJSObjectReference jSReference)
    {
        return Task.FromResult(new Int16Array(jSRuntime, jSReference, new()));
    }

    /// <inheritdoc/>
    public static Task<Int16Array> CreateAsync(IJSRuntime jSRuntime, IJSObjectReference jSReference, CreationOptions options)
    {
        return Task.FromResult(new Int16Array(jSRuntime, jSReference, options));
    }

    /// <inheritdoc cref="CreateAsync(IJSRuntime, IJSObjectReference, CreationOptions)"/>
    protected Int16Array(IJSRuntime jSRuntime, IJSObjectReference jSReference, CreationOptions options) : base(jSRuntime, jSReference, options) { }
}

