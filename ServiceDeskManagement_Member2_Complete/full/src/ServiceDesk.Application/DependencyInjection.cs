using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using ServiceDesk.Application.Catalog.Services;
using ServiceDesk.Application.Catalog.Validators;
using ServiceDesk.Application.Queues.Services;
using ServiceDesk.Application.Tickets.Services;

namespace ServiceDesk.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Member 2: catalog, queues, tickets
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IServiceItemService, ServiceItemService>();
        services.AddScoped<IQueueService, QueueService>();

        services.AddScoped<ITicketAccessService, TicketAccessService>();
        services.AddScoped<ITicketResponseBuilder, TicketResponseBuilder>();
        services.AddScoped<ITicketService, TicketService>();
        services.AddScoped<ITicketCommentService, TicketCommentService>();
        services.AddScoped<ITicketAttachmentService, TicketAttachmentService>();
        services.AddScoped<ITicketAssignmentService, TicketAssignmentService>();
        services.AddScoped<IAgentWorkloadService, AgentWorkloadService>();

        // Registers every validator in this assembly (all members' validators live here).
        services.AddValidatorsFromAssemblyContaining<CreateCategoryRequestValidator>();
        return services;
    }
}
