using CoachPulse.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoachPulse.Infrastructure.Persistence.Configurations;

public sealed class PaymobWebhookEventConfiguration : IEntityTypeConfiguration<PaymobWebhookEvent>
{
    public void Configure(EntityTypeBuilder<PaymobWebhookEvent> builder)
    {
        builder.ToTable("WebhookEvents");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.GatewayTransactionId).IsRequired().HasMaxLength(100);
        builder.Property(x => x.GatewayOrderId).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Payload).IsRequired();
        builder.HasIndex(x => x.GatewayTransactionId).IsUnique();
    }
}
