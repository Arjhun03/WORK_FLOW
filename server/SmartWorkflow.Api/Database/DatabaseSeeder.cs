using SmartWorkflow.Api.Models;
using SmartWorkflow.Api.Repositories;

namespace SmartWorkflow.Api.Database;

public class DatabaseSeeder
{
    private readonly IMongoDbContext _context;
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IWorkflowRepository _workflowRepository;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(
        IMongoDbContext context,
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IWorkflowRepository workflowRepository,
        ILogger<DatabaseSeeder> logger)
    {
        _context = context;
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _workflowRepository = workflowRepository;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        try
        {
            var isMongoUp = await _context.PingAsync();
            if (isMongoUp)
            {
                await _context.InitializeIndexesAsync();
            }
            else
            {
                _logger.LogInformation("Operating in development mode. Seeding in-memory store.");
            }

            // 1. Seed Roles
            var existingRoles = await _roleRepository.GetAllAsync();
            if (!existingRoles.Any())
            {
                _logger.LogInformation("Seeding default roles...");
                var rolesToSeed = new[]
                {
                    new Role { Name = SystemRoles.Admin, Description = "Full system administration and workflow design" },
                    new Role { Name = SystemRoles.Manager, Description = "Line manager approving team requests" },
                    new Role { Name = SystemRoles.HR, Description = "Human resources department approver" },
                    new Role { Name = SystemRoles.IT, Description = "IT department hardware/software approver" },
                    new Role { Name = SystemRoles.Finance, Description = "Finance department budget approver" },
                    new Role { Name = SystemRoles.Employee, Description = "Standard organizational employee" }
                };

                foreach (var role in rolesToSeed)
                {
                    await _roleRepository.CreateAsync(role);
                }
            }

            // 2. Seed Users
            var existingUsers = await _userRepository.GetAllAsync();
            if (!existingUsers.Any())
            {
                _logger.LogInformation("Seeding default users...");
                var defaultPasswordHash = BCrypt.Net.BCrypt.HashPassword("password123");

                var manager = new User
                {
                    Name = "Sarah Manager",
                    Email = "manager@example.com",
                    PasswordHash = defaultPasswordHash,
                    Roles = new List<string> { SystemRoles.Manager, SystemRoles.Employee },
                    Department = "Engineering",
                    IsActive = true
                };
                await _userRepository.CreateAsync(manager);

                var admin = new User
                {
                    Name = "Alex Admin",
                    Email = "admin@example.com",
                    PasswordHash = defaultPasswordHash,
                    Roles = new List<string> { SystemRoles.Admin },
                    Department = "Administration",
                    IsActive = true
                };
                await _userRepository.CreateAsync(admin);

                var hr = new User
                {
                    Name = "Helen HR",
                    Email = "hr@example.com",
                    PasswordHash = defaultPasswordHash,
                    Roles = new List<string> { SystemRoles.HR, SystemRoles.Employee },
                    Department = "Human Resources",
                    IsActive = true
                };
                await _userRepository.CreateAsync(hr);

                var it = new User
                {
                    Name = "Ian IT",
                    Email = "it@example.com",
                    PasswordHash = defaultPasswordHash,
                    Roles = new List<string> { SystemRoles.IT, SystemRoles.Employee },
                    Department = "Information Technology",
                    IsActive = true
                };
                await _userRepository.CreateAsync(it);

                var finance = new User
                {
                    Name = "Fiona Finance",
                    Email = "finance@example.com",
                    PasswordHash = defaultPasswordHash,
                    Roles = new List<string> { SystemRoles.Finance, SystemRoles.Employee },
                    Department = "Finance",
                    IsActive = true
                };
                await _userRepository.CreateAsync(finance);

                var employee = new User
                {
                    Name = "Evan Employee",
                    Email = "employee@example.com",
                    PasswordHash = defaultPasswordHash,
                    Roles = new List<string> { SystemRoles.Employee },
                    Department = "Engineering",
                    ManagerId = manager.Id,
                    IsActive = true
                };
                await _userRepository.CreateAsync(employee);

                _logger.LogInformation("Default users seeded successfully (Password: password123)");
            }

            // 3. Seed Workflows
            var existingWorkflows = await _workflowRepository.GetAllAsync();
            if (!existingWorkflows.Any())
            {
                _logger.LogInformation("Seeding default workflows...");
                var adminUser = await _userRepository.GetByEmailAsync("admin@example.com");
                var createdById = adminUser?.Id ?? string.Empty;

                // Workflow 1: Leave Request (Manager -> HR)
                var leaveWorkflow = new Workflow
                {
                    Name = "Leave Request",
                    Description = "Standard leave approval workflow for vacation, sick, and personal leave.",
                    RequestType = "Leave",
                    Version = 1,
                    IsActive = true,
                    CreatedBy = createdById,
                    Steps = new List<WorkflowStep>
                    {
                        new()
                        {
                            Name = "Direct Manager Approval",
                            Order = 1,
                            ApproverType = ApproverTypes.Manager,
                            IsRequired = true
                        },
                        new()
                        {
                            Name = "HR Department Approval",
                            Order = 2,
                            ApproverType = ApproverTypes.Role,
                            ApproverRole = SystemRoles.HR,
                            IsRequired = true
                        }
                    }
                };
                await _workflowRepository.CreateAsync(leaveWorkflow);

                // Workflow 2: Equipment Request (Manager -> IT -> Finance)
                var equipmentWorkflow = new Workflow
                {
                    Name = "Equipment Request",
                    Description = "Hardware, software, and office equipment provisioning workflow.",
                    RequestType = "Equipment",
                    Version = 1,
                    IsActive = true,
                    CreatedBy = createdById,
                    Steps = new List<WorkflowStep>
                    {
                        new()
                        {
                            Name = "Manager Approval",
                            Order = 1,
                            ApproverType = ApproverTypes.Manager,
                            IsRequired = true
                        },
                        new()
                        {
                            Name = "IT Department Review",
                            Order = 2,
                            ApproverType = ApproverTypes.Role,
                            ApproverRole = SystemRoles.IT,
                            IsRequired = true
                        },
                        new()
                        {
                            Name = "Finance Budget Approval",
                            Order = 3,
                            ApproverType = ApproverTypes.Role,
                            ApproverRole = SystemRoles.Finance,
                            IsRequired = true
                        }
                    }
                };
                await _workflowRepository.CreateAsync(equipmentWorkflow);

                // Workflow 3: Simple Request (Manager)
                var simpleWorkflow = new Workflow
                {
                    Name = "Simple Request",
                    Description = "Quick single-step general request routed to your direct line manager.",
                    RequestType = "General",
                    Version = 1,
                    IsActive = true,
                    CreatedBy = createdById,
                    Steps = new List<WorkflowStep>
                    {
                        new()
                        {
                            Name = "Manager Sign-off",
                            Order = 1,
                            ApproverType = ApproverTypes.Manager,
                            IsRequired = true
                        }
                    }
                };
                await _workflowRepository.CreateAsync(simpleWorkflow);

                _logger.LogInformation("Default workflows seeded successfully.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while seeding database: {Message}", ex.Message);
        }
    }
}
