using ChatAgent.Services;

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
        Description = "A basic AI chat agent built using the Microsoft Semantic Kernel Agent Framework and Azure AI Foundry."
    });
});

// Register the chat agent service
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

app.Run();
