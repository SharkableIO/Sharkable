namespace Sharkable;

public class UnifiedResultFactory
{
    public TOut GetResultObject<TIn, TOut>(Func<TIn, TOut> func, TIn input)
    {
        return func.Invoke(input);
    }
}