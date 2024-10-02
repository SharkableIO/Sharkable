using Microsoft.AspNetCore.Mvc;

namespace Sharkable.NativeTest;

[SharkEndpoint]
public class AttrTestEndpoint(ILogger<AttrTestEndpoint> logger)
{
    [SharkMethod("test/{age}")]
    public async Task<Todo[]?> Lobster([FromQuery]string? name, 
        string? age, 
        [FromHeader]string? lover,
        [FromBody]Todo[]? todo)
    {
        await Task.Delay(100);
        logger.LogInformation("{a}.{b},{c}",name,age?.ToString(),lover);
        return todo;
    }
    public async void LetGo()
    {
        await Task.Delay(3000);
        logger.LogInformation("Let Go");
    }
}