using Microsoft.EntityFrameworkCore;
using QueueDemo.Data;
using QueueDemo.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSingleton<UserQueue>();
builder.Services.AddHostedService<UserQueueWorker>();

var app = builder.Build();


app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();