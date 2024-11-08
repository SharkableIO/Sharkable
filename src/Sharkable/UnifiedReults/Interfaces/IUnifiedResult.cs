namespace Sharkable;

public interface IUnifiedResult<in TData, out TResult>
{
    Func<TData, TResult> Result { get; }
}