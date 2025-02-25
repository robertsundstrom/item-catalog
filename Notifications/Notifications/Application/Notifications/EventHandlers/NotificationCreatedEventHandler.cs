﻿
using Catalog.Notifications.Application.Common.Interfaces;
using Catalog.Notifications.Application.Common.Models;
using Catalog.Notifications.Domain.Events;

using Hangfire;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace Catalog.Notifications.Application.Notifications.EventHandlers;

public class NotificationCreatedEventHandler : INotificationHandler<DomainEventNotification<NotificationCreatedEvent>>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly INotificationPublisher _notficationPublisher;
    private readonly IBackgroundJobClient _recurringJobManager;

    public NotificationCreatedEventHandler(IServiceProvider serviceProvider, INotificationPublisher notficationSender, IBackgroundJobClient recurringJobManager)
    {
        _serviceProvider = serviceProvider;
        _notficationPublisher = notficationSender;
        _recurringJobManager = recurringJobManager;
    }

    public async Task Handle(DomainEventNotification<NotificationCreatedEvent> notification2, CancellationToken cancellationToken)
    {
        var domainEvent = notification2.DomainEvent;

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<INotificationsContext>();

        var notification = await context.Notifications
            .FirstAsync(i => i.Id == domainEvent.NotificationId, cancellationToken);

        if (notification.ScheduledFor is not null)
        {
            if (notification.ScheduledFor < DateTime.UtcNow)
            {
                throw new InvalidOperationException("Cannot send notification back in time!");
            }

            await ScheduleNotification(context, notification);
        }
        else
        {
            await PublishNotification(notification);
        }
    }

    private async Task PublishNotification(Domain.Entities.Notification notification)
    {
        await _notficationPublisher.PublishNotification(notification);
    }

    private async Task ScheduleNotification(INotificationsContext context, Domain.Entities.Notification notification)
    {
        var delay = notification.ScheduledFor.GetValueOrDefault() - DateTime.UtcNow;

        var jobId = _recurringJobManager.Schedule<INotificationPublisher>(
            (sender) => sender.PublishNotification(notification),
                delay);

        notification.ScheduledJobId = jobId;

        await context.SaveChangesAsync();
    }
}