namespace Fingo.Shared.Configuration
{
    public sealed class DatabaseOptions
    {
        public const string SectionName = "ConnectionStrings";

        public string PostgresConnection { get; set; } = string.Empty;
    }
}