using FluentAssertions;
using SendellVoice.Domain.Entities;
using SendellVoice.Domain.Enums;

namespace SendellVoice.UnitTests.Domain;

public class ConversationTests
{
    [Fact]
    public void Conversation_Creation_SetsDefaultValues()
    {
        // Act
        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            CustomerId = "customer-123"
        };

        // Assert
        conversation.Id.Should().NotBeEmpty();
        conversation.CustomerId.Should().Be("customer-123");
        conversation.Messages.Should().BeEmpty();
        conversation.Intents.Should().BeEmpty();
        conversation.Metadata.Should().BeEmpty();
    }

    [Fact]
    public void Conversation_CanHaveMultipleMessages()
    {
        // Arrange
        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            CustomerId = "customer-123"
        };

        // Act
        conversation.Messages.Add(new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = conversation.Id,
            Content = "Hello",
            Direction = MessageDirection.Inbound
        });

        conversation.Messages.Add(new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = conversation.Id,
            Content = "Hi! How can I help?",
            Direction = MessageDirection.Outbound,
            IsFromBot = true
        });

        // Assert
        conversation.Messages.Should().HaveCount(2);
    }

    [Fact]
    public void Conversation_CanAssignAgent()
    {
        // Arrange
        var agent = new Agent
        {
            Id = Guid.NewGuid(),
            Name = "John Doe",
            Email = "john@example.com"
        };

        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            CustomerId = "customer-123"
        };

        // Act
        conversation.AssignedAgent = agent;
        conversation.AssignedAgentId = agent.Id;

        // Assert
        conversation.AssignedAgent.Should().Be(agent);
        conversation.AssignedAgentId.Should().Be(agent.Id);
    }

    [Fact]
    public void Conversation_StatusTransitions()
    {
        // Arrange
        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            CustomerId = "customer-123",
            Status = ConversationStatus.Active
        };

        // Act & Assert
        conversation.Status.Should().Be(ConversationStatus.Active);

        conversation.Status = ConversationStatus.OnHold;
        conversation.Status.Should().Be(ConversationStatus.OnHold);

        conversation.Status = ConversationStatus.Escalated;
        conversation.Status.Should().Be(ConversationStatus.Escalated);

        conversation.Status = ConversationStatus.Resolved;
        conversation.Status.Should().Be(ConversationStatus.Resolved);

        conversation.Status = ConversationStatus.Closed;
        conversation.Status.Should().Be(ConversationStatus.Closed);
    }

    [Fact]
    public void Conversation_CanSetPrimaryIntent()
    {
        // Arrange
        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            CustomerId = "customer-123"
        };

        // Act
        conversation.PrimaryIntent = IntentCategory.Billing;
        conversation.IntentConfidence = 0.95;

        // Assert
        conversation.PrimaryIntent.Should().Be(IntentCategory.Billing);
        conversation.IntentConfidence.Should().Be(0.95);
    }
}

public class MessageTests
{
    [Fact]
    public void Message_Creation_SetsDefaultValues()
    {
        // Act
        var message = new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = Guid.NewGuid(),
            Content = "Test message"
        };

        // Assert
        message.Id.Should().NotBeEmpty();
        message.Content.Should().Be("Test message");
        message.Metadata.Should().BeEmpty();
        message.IsFromBot.Should().BeFalse();
    }

    [Fact]
    public void Message_CanHaveAudioProperties()
    {
        // Arrange & Act
        var message = new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = Guid.NewGuid(),
            Content = "Audio message",
            Type = MessageType.Audio,
            AudioUrl = "https://example.com/audio.wav",
            AudioDuration = TimeSpan.FromSeconds(30),
            Transcription = "Transcribed text"
        };

        // Assert
        message.Type.Should().Be(MessageType.Audio);
        message.AudioUrl.Should().Be("https://example.com/audio.wav");
        message.AudioDuration.Should().Be(TimeSpan.FromSeconds(30));
        message.Transcription.Should().Be("Transcribed text");
    }

    [Fact]
    public void Message_CanHaveIntentAnalysis()
    {
        // Arrange & Act
        var message = new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = Guid.NewGuid(),
            Content = "I want to cancel my subscription",
            DetectedIntent = IntentCategory.Cancellation,
            IntentConfidence = 0.88,
            Sentiment = "negative",
            SentimentScore = -0.6
        };

        // Assert
        message.DetectedIntent.Should().Be(IntentCategory.Cancellation);
        message.IntentConfidence.Should().Be(0.88);
        message.Sentiment.Should().Be("negative");
        message.SentimentScore.Should().Be(-0.6);
    }
}

public class IntentTests
{
    [Fact]
    public void Intent_Creation_WithRequiredProperties()
    {
        // Arrange & Act
        var intent = new Intent
        {
            Id = Guid.NewGuid(),
            ConversationId = Guid.NewGuid(),
            Category = IntentCategory.Technical,
            Confidence = 0.92,
            Reasoning = "User mentioned error and system not working"
        };

        // Assert
        intent.Category.Should().Be(IntentCategory.Technical);
        intent.Confidence.Should().Be(0.92);
        intent.Reasoning.Should().Contain("error");
    }

    [Fact]
    public void Intent_CanHaveExtractedEntities()
    {
        // Arrange & Act
        var intent = new Intent
        {
            Id = Guid.NewGuid(),
            ConversationId = Guid.NewGuid(),
            Category = IntentCategory.Billing,
            Confidence = 0.95,
            OriginalText = "I need to pay my invoice #12345",
            ExtractedEntities = new List<string> { "invoice:12345" }
        };

        // Assert
        intent.ExtractedEntities.Should().Contain("invoice:12345");
    }
}
