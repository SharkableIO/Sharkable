namespace Sharkable.Sample
{
    public class Todo
    {
        public Todo() { }
        public Todo(int Id, string? Title, DateTime DueBy, bool IsComplete = false)
        {
            this.Id = Id;
            this.Title = Title;
            this.DueBy = DueBy;
            this.IsComplete = IsComplete;
        }
        public int Id { get; set; }
        public string? Title { get; set; }
        public DateTime DueBy { get; set; }
        public bool IsComplete { get; set; }
    }
}
