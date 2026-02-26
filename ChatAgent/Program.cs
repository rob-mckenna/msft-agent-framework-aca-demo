using ChatAgent.Agents;
using ChatAgent.Services;
using Microsoft.Agents.Hosting.AspNetCore;
using Microsoft.Agents.Storage;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "ChatAgent API",
        Version = "v1",
        Description = "A basic AI chat agent built using the Microsoft Agent Framework and Azure OpenAI."
    });
});

// Register in-memory storage for agent conversation state
builder.Services.AddSingleton<IStorage, MemoryStorage>();

// Register the Microsoft Agent Framework agent and its infrastructure (CloudAdapter, etc.)
builder.AddAgentApplicationOptions();
builder.AddAgent<BasicChatAgent>();

// Register the REST chat service (backs the POST /chat endpoint)
builder.Services.AddSingleton<IChatAgentService, ChatAgentService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

// Map the Bot Framework Activity Protocol endpoint (POST /api/messages)
// requireAuth: false is suitable for development; set to true for production with proper Bot Framework credentials
app.MapAgentEndpoints(requireAuth: false);

app.Run();
