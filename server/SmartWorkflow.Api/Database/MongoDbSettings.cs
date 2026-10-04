namespace SmartWorkflow.Api.Database;

public class MongoDbSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = "SmartWorkflowDb";
}
