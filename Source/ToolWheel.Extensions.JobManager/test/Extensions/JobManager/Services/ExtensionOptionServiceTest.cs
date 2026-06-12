using System;
using NUnit.Framework;
using ToolWheel.Extensions.JobManager.Storage;

namespace ToolWheel.Extensions.JobManager.Services;

[TestFixture]
public class ExtensionOptionServiceTest
{
    private ExtensionOptionService CreateService()
    {
        var storage = new InMemoryExtensionOptionStorage();
        return new ExtensionOptionService(storage);
    }

    // ==================== CREATE Tests ====================

    [Test]
    public void Create_WithValidData_StoresOption()
    {
        // Arrange
        var service = CreateService();
        var ownerId = "owner1";
        var itemKey = "key1";
        var option = new TestOption { Value = "test" };

        // Act
        service.Create(ownerId, itemKey, option);

        // Assert
        var retrieved = service.Read<TestOption>(ownerId, itemKey);
        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved!.Value, Is.EqualTo("test"));
    }

    [Test]
    public void Create_WithMultipleOptions_StoresAllOptions()
    {
        // Arrange
        var service = CreateService();
        var ownerId = "owner1";
        var option1 = new TestOption { Value = "option1" };
        var option2 = new TestOption { Value = "option2" };

        // Act
        service.Create(ownerId, "key1", option1);
        service.Create(ownerId, "key2", option2);

        // Assert
        Assert.That(service.Read<TestOption>(ownerId, "key1")!.Value, Is.EqualTo("option1"));
        Assert.That(service.Read<TestOption>(ownerId, "key2")!.Value, Is.EqualTo("option2"));
    }

    [Test]
    public void Create_ReplaceExisting_OverwritesOption()
    {
        // Arrange
        var service = CreateService();
        var ownerId = "owner1";
        var itemKey = "key1";
        var option1 = new TestOption { Value = "first" };
        var option2 = new TestOption { Value = "second" };

        // Act
        service.Create(ownerId, itemKey, option1);
        service.Create(ownerId, itemKey, option2);

        // Assert
        var retrieved = service.Read<TestOption>(ownerId, itemKey);
        Assert.That(retrieved!.Value, Is.EqualTo("second"));
    }

    [Test]
    public void Create_WithNullOwnerId_ThrowsArgumentNullException()
    {
        // Arrange
        var service = CreateService();
        var option = new TestOption { Value = "test" };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => service.Create(null!, "key1", option));
    }

    [Test]
    public void Create_WithNullItemKey_ThrowsArgumentNullException()
    {
        // Arrange
        var service = CreateService();
        var option = new TestOption { Value = "test" };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => service.Create("owner1", null!, option));
    }

    [Test]
    public void Create_WithNullOption_ThrowsArgumentNullException()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => service.Create<TestOption>("owner1", "key1", null!));
    }

    // ==================== READ Tests ====================

    [Test]
    public void Read_WithExistingOption_ReturnsOption()
    {
        // Arrange
        var service = CreateService();
        var ownerId = "owner1";
        var itemKey = "key1";
        var option = new TestOption { Value = "test" };
        service.Create(ownerId, itemKey, option);

        // Act
        var retrieved = service.Read<TestOption>(ownerId, itemKey);

        // Assert
        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved!.Value, Is.EqualTo("test"));
    }

    [Test]
    public void Read_WithNonExistingOption_ReturnsNull()
    {
        // Arrange
        var service = CreateService();

        // Act
        var retrieved = service.Read<TestOption>("owner1", "nonexistent");

        // Assert
        Assert.That(retrieved, Is.Null);
    }

    [Test]
    public void Read_WithNonExistingOwner_ReturnsNull()
    {
        // Arrange
        var service = CreateService();

        // Act
        var retrieved = service.Read<TestOption>("nonexistent", "key1");

        // Assert
        Assert.That(retrieved, Is.Null);
    }

    [Test]
    public void Read_WithNullOwnerId_ThrowsArgumentNullException()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => service.Read<TestOption>(null!, "key1"));
    }

    [Test]
    public void Read_WithNullItemKey_ThrowsArgumentNullException()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => service.Read<TestOption>("owner1", null!));
    }

    // ==================== UPDATE Tests ====================

    [Test]
    public void Update_WithExistingOption_ReturnsTrue()
    {
        // Arrange
        var service = CreateService();
        var ownerId = "owner1";
        var itemKey = "key1";
        var option1 = new TestOption { Value = "first" };
        var option2 = new TestOption { Value = "second" };
        service.Create(ownerId, itemKey, option1);

        // Act
        var result = service.Update(ownerId, itemKey, option2);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void Update_WithExistingOption_UpdatesValue()
    {
        // Arrange
        var service = CreateService();
        var ownerId = "owner1";
        var itemKey = "key1";
        var option1 = new TestOption { Value = "first" };
        var option2 = new TestOption { Value = "second" };
        service.Create(ownerId, itemKey, option1);

        // Act
        service.Update(ownerId, itemKey, option2);
        var retrieved = service.Read<TestOption>(ownerId, itemKey);

        // Assert
        Assert.That(retrieved!.Value, Is.EqualTo("second"));
    }

    [Test]
    public void Update_WithNonExistingOption_ReturnsFalse()
    {
        // Arrange
        var service = CreateService();
        var option = new TestOption { Value = "test" };

        // Act
        var result = service.Update("owner1", "nonexistent", option);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void Update_WithNonExistingOwner_ReturnsFalse()
    {
        // Arrange
        var service = CreateService();
        var option = new TestOption { Value = "test" };

        // Act
        var result = service.Update("nonexistent", "key1", option);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void Update_WithNullOwnerId_ThrowsArgumentNullException()
    {
        // Arrange
        var service = CreateService();
        var option = new TestOption { Value = "test" };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => service.Update(null!, "key1", option));
    }

    [Test]
    public void Update_WithNullItemKey_ThrowsArgumentNullException()
    {
        // Arrange
        var service = CreateService();
        var option = new TestOption { Value = "test" };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => service.Update("owner1", null!, option));
    }

    [Test]
    public void Update_WithNullOption_ThrowsArgumentNullException()
    {
        // Arrange
        var service = CreateService();
        var option = new TestOption { Value = "first" };
        service.Create(ownerId: "owner1", itemKey: "key1", option);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => service.Update<TestOption>("owner1", "key1", null!));
    }

    [Test]
    public void Update_WithValueType_ReturnsFalse()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.Update("owner1", "key1", 42);

        // Assert
        Assert.That(result, Is.False);
    }

    // ==================== DELETE Tests ====================

    [Test]
    public void Delete_WithExistingOption_ReturnsTrue()
    {
        // Arrange
        var service = CreateService();
        var ownerId = "owner1";
        var itemKey = "key1";
        var option = new TestOption { Value = "test" };
        service.Create(ownerId, itemKey, option);

        // Act
        var result = service.Delete(ownerId, itemKey);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void Delete_WithExistingOption_RemovesOption()
    {
        // Arrange
        var service = CreateService();
        var ownerId = "owner1";
        var itemKey = "key1";
        var option = new TestOption { Value = "test" };
        service.Create(ownerId, itemKey, option);

        // Act
        service.Delete(ownerId, itemKey);
        var retrieved = service.Read<TestOption>(ownerId, itemKey);

        // Assert
        Assert.That(retrieved, Is.Null);
    }

    [Test]
    public void Delete_WithNonExistingOption_ReturnsFalse()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.Delete("owner1", "nonexistent");

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void Delete_WithNonExistingOwner_ReturnsFalse()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.Delete("nonexistent", "key1");

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void Delete_WithNullOwnerId_ThrowsArgumentNullException()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => service.Delete(null!, "key1"));
    }

    [Test]
    public void Delete_WithNullItemKey_ThrowsArgumentNullException()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => service.Delete("owner1", null!));
    }

    // ==================== READALL Tests ====================

    [Test]
    public void ReadAll_WithStoredOptions_ReturnsAllOptions()
    {
        // Arrange
        var service = CreateService();
        var ownerId = "owner1";
        var option1 = new TestOption { Value = "option1" };
        var option2 = new TestOption { Value = "option2" };
        service.Create(ownerId, "key1", option1);
        service.Create(ownerId, "key2", option2);

        // Act
        var retrieved = service.ReadAll<TestOption>(ownerId);

        // Assert
        Assert.That(retrieved, Has.Count.EqualTo(2));
        Assert.That(retrieved[0].Value, Is.EqualTo("option1"));
        Assert.That(retrieved[1].Value, Is.EqualTo("option2"));
    }

    [Test]
    public void ReadAll_WithNoOptions_ReturnsEmptyList()
    {
        // Arrange
        var service = CreateService();

        // Act
        var retrieved = service.ReadAll<TestOption>("owner1");

        // Assert
        Assert.That(retrieved, Is.Empty);
    }

    [Test]
    public void ReadAll_WithNonExistingOwner_ReturnsEmptyList()
    {
        // Arrange
        var service = CreateService();

        // Act
        var retrieved = service.ReadAll<TestOption>("nonexistent");

        // Assert
        Assert.That(retrieved, Is.Empty);
    }

    [Test]
    public void ReadAll_WithNullOwnerId_ThrowsArgumentNullException()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => service.ReadAll<TestOption>(null!));
    }

    // ==================== DELETEALL Tests ====================

    [Test]
    public void DeleteAll_WithStoredOptions_RemovesAllOptions()
    {
        // Arrange
        var service = CreateService();
        var ownerId = "owner1";
        var option1 = new TestOption { Value = "option1" };
        var option2 = new TestOption { Value = "option2" };
        service.Create(ownerId, "key1", option1);
        service.Create(ownerId, "key2", option2);

        // Act
        service.DeleteAll(ownerId);
        var retrieved = service.ReadAll<TestOption>(ownerId);

        // Assert
        Assert.That(retrieved, Is.Empty);
    }

    [Test]
    public void DeleteAll_WithMultipleOwners_ClearsOnlyTarget()
    {
        // Arrange
        var service = CreateService();
        var option = new TestOption { Value = "test" };
        service.Create("owner1", "key1", option);
        service.Create("owner2", "key1", option);

        // Act
        service.DeleteAll("owner1");

        // Assert
        Assert.That(service.ReadAll<TestOption>("owner1"), Is.Empty);
        Assert.That(service.ReadAll<TestOption>("owner2"), Has.Count.EqualTo(1));
    }

    [Test]
    public void DeleteAll_WithNullOwnerId_ThrowsArgumentNullException()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => service.DeleteAll(null!));
    }

    // ==================== GETOWNERIDS Tests ====================

    [Test]
    public void GetOwnerIds_WithNoOwners_ReturnsEmptyList()
    {
        // Arrange
        var service = CreateService();

        // Act
        var ownerIds = service.GetOwnerIds();

        // Assert
        Assert.That(ownerIds, Is.Empty);
    }

    [Test]
    public void GetOwnerIds_WithOwners_ReturnsAllOwnerIds()
    {
        // Arrange
        var service = CreateService();
        var option = new TestOption { Value = "test" };
        service.Create("owner1", "key1", option);
        service.Create("owner2", "key1", option);
        service.Create("owner3", "key1", option);

        // Act
        var ownerIds = service.GetOwnerIds();

        // Assert
        Assert.That(ownerIds, Has.Count.EqualTo(3));
        Assert.That(ownerIds, Does.Contain("owner1"));
        Assert.That(ownerIds, Does.Contain("owner2"));
        Assert.That(ownerIds, Does.Contain("owner3"));
    }

    [Test]
    public void GetOwnerIds_AfterDeleteAll_ExcludesDeleted()
    {
        // Arrange
        var service = CreateService();
        var option = new TestOption { Value = "test" };
        service.Create("owner1", "key1", option);
        service.Create("owner2", "key1", option);
        service.DeleteAll("owner1");

        // Act
        var ownerIds = service.GetOwnerIds();

        // Assert
        Assert.That(ownerIds, Has.Count.EqualTo(1));
        Assert.That(ownerIds, Does.Contain("owner2"));
    }

    // ==================== Integration Tests ====================

    [Test]
    public void CRUD_CompleteWorkflow_AllOperationsSucceed()
    {
        // Arrange
        var service = CreateService();
        var ownerId = "job123";
        var option1 = new TestOption { Value = "config1" };
        var option2 = new TestOption { Value = "config2" };

        // Act & Assert
        // Create
        service.Create(ownerId, "config1", option1);
        Assert.That(service.Read<TestOption>(ownerId, "config1"), Is.Not.Null);

        // Read All
        service.Create(ownerId, "config2", option2);
        var all = service.ReadAll<TestOption>(ownerId);
        Assert.That(all, Has.Count.EqualTo(2));

        // Update
        var updated = new TestOption { Value = "updated" };
        Assert.That(service.Update(ownerId, "config1", updated), Is.True);
        Assert.That(service.Read<TestOption>(ownerId, "config1")!.Value, Is.EqualTo("updated"));

        // Delete One
        Assert.That(service.Delete(ownerId, "config1"), Is.True);
        Assert.That(service.Read<TestOption>(ownerId, "config1"), Is.Null);

        // Delete All
        service.DeleteAll(ownerId);
        Assert.That(service.ReadAll<TestOption>(ownerId), Is.Empty);
    }

    [Test]
    public void ConcurrentCreate_WithMultipleOwners_AllOptionsStored()
    {
        // Arrange
        var service = CreateService();
        var options = new[] { "opt1", "opt2", "opt3", "opt4", "opt5" };

        // Act
        foreach (var opt in options)
        {
            service.Create($"owner_{opt}", $"key_{opt}", new TestOption { Value = opt });
        }

        // Assert
        var ownerIds = service.GetOwnerIds();
        Assert.That(ownerIds, Has.Count.EqualTo(5));
        foreach (var opt in options)
        {
            var retrieved = service.Read<TestOption>($"owner_{opt}", $"key_{opt}");
            Assert.That(retrieved!.Value, Is.EqualTo(opt));
        }
    }

    // ==================== Test Helpers ====================

    private class TestOption
    {
        public string Value { get; set; } = string.Empty;
    }
}
