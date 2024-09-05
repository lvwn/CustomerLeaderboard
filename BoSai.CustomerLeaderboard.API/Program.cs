using BoSai.CustomerLeaderboard.Domain.Interfaces;
using BoSai.CustomerLeaderboard.Domain.Services;

namespace BoSai.CustomerLeaderboard.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddSingleton<ILeaderboardService, LeaderboardService>();
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAllOrigins", builder =>
                {
                    builder.AllowAnyOrigin()
                           .AllowAnyMethod()
                           .AllowAnyHeader();
                });
            });

            // ���� Kestrel ������
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.ListenAnyIP(5000); // ���� HTTP �˿�
                options.ListenAnyIP(5001, listenOptions =>
                {
                    listenOptions.UseHttps(); // ���� HTTPS �˿�
                });
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                // ʹ��CORS����
                app.UseCors("AllowAllOrigins");
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "API�ĵ� v1");
                    c.RoutePrefix = "swagger";
                });
            }
            else
            { 
                app.UseHttpsRedirection();
            }

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}