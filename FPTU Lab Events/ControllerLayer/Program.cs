using System.Text;
using InfrastructureLayer.Core.JWT;
using InfrastructureLayer.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Application.Services.Auth;
using DotNetEnv;
using Application.Services.User;
using InfrastructureLayer.Core.Mail;
using Application.Services.Role;
using DomainLayer.Entities;
using DomainLayer.Enum;
using System.IdentityModel.Tokens.Jwt;

namespace ControllerLayer
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
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "FPTU Lab Events API", Version = "v1" });
                var xmlFilename = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
                c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename), includeControllerXmlComments: true);
                var securityScheme = new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                    Description = "Nhập JWT Bearer token"
                };
                c.AddSecurityDefinition("Bearer", securityScheme);
                var securityRequirement = new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
                {
                    {
                        new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                        {
                            Reference = new Microsoft.OpenApi.Models.OpenApiReference
                            {
                                Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        new string[] {}
                    }
                };
                c.AddSecurityRequirement(securityRequirement);
            });

            // CORS cho frontend - cho phép tất cả origins
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAllOrigins", policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                });
            });

            // Cấu hình DbContext
            builder.Services.AddDbContext<LabDbContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

            // HttpClient
            builder.Services.AddHttpClient();

			// Mail service
			builder.Services.AddSingleton<IMailService>(sp =>
			{
				var cfg = builder.Configuration;
				var server = cfg["Mail:SmtpServer"] ?? "smtp.gmail.com";
				var portStr = cfg["Mail:SmtpPort"];
				var port = int.TryParse(portStr, out var p) ? p : 587;
				var user = cfg["Mail:Username"] ?? cfg["SMTPEmail"] ?? string.Empty;
				var pass = cfg["Mail:Password"] ?? cfg["SMTPPassword"] ?? string.Empty;
				return new MailService(server, port, user, pass);
			});

            // JWT Service
            builder.Services.AddScoped<IJwtService, JwtService>();
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<Application.Services.Booking.IBookingService, Application.Services.Booking.BookingService>();
            builder.Services.AddScoped<Application.Services.User.ILabMemberService, Application.Services.User.LabMemberService>();
            
            // New services for Tú's features
            builder.Services.AddScoped<Application.Services.Notification.INotificationService, Application.Services.Notification.NotificationService>();
            builder.Services.AddScoped<Application.Services.Report.IReportService, Application.Services.Report.ReportService>();
            builder.Services.AddScoped<Application.Services.Room.IRoomService, Application.Services.Room.RoomService>();
            builder.Services.AddScoped<Application.Services.Equipment.IEquipmentService, Application.Services.Equipment.EquipmentService>();
            builder.Services.AddScoped<Application.Services.Lab.ILabService, Application.Services.Lab.LabService>();
            builder.Services.AddScoped<Application.Services.Event.IEventService, Application.Services.Event.EventService>();
            builder.Services.AddScoped<IRoleService, RoleService>();

            // Load .env (optional)
            try { Env.Load(); } catch { }

            // Authentication - JWT Bearer
            // Keep original claim types from token (do not remap to WS-* URIs)
            JwtSecurityTokenHandler.DefaultMapInboundClaims = false;
            var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET")
                             ?? builder.Configuration["Jwt:Secret"]
                             ?? "0ebe2440a9eba77bed3a7a081b9bb26d792baaec3fcac1eae95b7148bfdcb8c5";
            var keyBytes = Encoding.ASCII.GetBytes(jwtSecret);

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero,
                    RoleClaimType = "role"
                };
            });

            // Authorization policies
            builder.Services.AddAuthorization(options =>
            {
                // Use RequireRole so it honors TokenValidationParameters.RoleClaimType = "role"
                options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            //if (app.Environment.IsDevelopment())
            //{
                app.UseSwagger();
                app.UseSwaggerUI();
            //}

            app.UseHttpsRedirection();

            app.UseCors("AllowAllOrigins");

            app.UseAuthentication();
            app.UseAuthorization();


            app.MapControllers();

            // Auto apply EF Core migrations at startup
            ApplyPendingMigrations(app);

            // Ensure default Admin user exists (moved to Infrastructure)
            DbSeeder.EnsureAdminUser(app.Services);

            app.Run();
        }

		private static void ApplyPendingMigrations(WebApplication app)
		{
			using var scope = app.Services.CreateScope();
			var dbContext = scope.ServiceProvider.GetRequiredService<LabDbContext>();
			dbContext.Database.Migrate();
		}

        // seeding moved to InfrastructureLayer.Data.DbSeeder
    }
}
