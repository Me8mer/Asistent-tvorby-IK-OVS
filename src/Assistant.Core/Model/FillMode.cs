namespace Assistant.Core.Model
{
    [System.Flags]
    public enum FillMode
    {
        None = 0,
        Deterministic = 1,
        Ai = 2,
        Chatbot = 4
    }
}
