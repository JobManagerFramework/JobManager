using NUnit.Framework;
using ToolWheel.Extensions.JobManager.Storage;

namespace ToolWheel.Extensions.JobManager;

[TestFixture]
public class InMemoryExtensionOptionStorageTest
{
    private InMemoryExtensionOptionStorage CreateStorage()
    {
        return new InMemoryExtensionOptionStorage();
    }

    // ==================== SET Tests ====================

    [Test]
    public void Set_WithValidData_StoresItem()
    {
        // Arrange
        var storage = CreateStorage();
        var ownerId = "owner1";
        var itemKey = "key1";
        var item = new TestOption { Value = "test" };

        // Act
        storage.Set(ownerId, itemKey, item);

        // Assert
        var retrieved = storage.Get<TestOption>(ownerId, itemKey);
        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved!.Value, Is.EqualTo("test"));
    }

    [Test]
    public void Set_WithMultipleItems_StoresAllItems()
    {
        // Arrange
        var storage = CreateStorage();
        var ownerId = "owner1";
        var item1 = new TestOption { Value = "item1" };
        var item2 = new TestOption { Value = "item2" };

        // Act
        storage.Set(ownerId, "key1", item1);
        storage.Set(ownerId, "key2", item2);

        // Assert
        Assert.That(storage.Get<TestOption>(ownerId, "key1")!.Value, Is.EqualTo("item1"));
        Assert.That(storage.Get<TestOption>(ownerId, "key2")!.Value, Is.EqualTo("item2"));
    }

    [Test]
    public void Set_WithMultipleOwners_StoresItemsPerOwner()
    {
        // Arrange
        var storage = CreateStorage();
        var item1 = new TestOption { Value = "owner1_item" };
        var item2 = new TestOption { Value = "owner2_item" };

        // Act
        storage.Set("owner1", "key1", item1);
        storage.Set("owner2", "key1", item2);

        // Assert
        Assert.That(storage.Get<TestOption>("owner1", "key1")!.Value, Is.EqualTo("owner1_item"));
        Assert.That(storage.Get<TestOption>("owner2", "key1")!.Value, Is.EqualTo("owner2_item"));
    }

    [Test]
    public void Set_ReplaceExisting_OverwritesItem()
    {
        // Arrange
        var storage = CreateStorage();
        var ownerId = "owner1";
        var itemKey = "key1";
        var item1 = new TestOption { Value = "first" };
        var item2 = new TestOption { Value = "second" };

        // Act
        storage.Set(ownerId, itemKey, item1);
        storage.Set(ownerId, itemKey, item2);

        // Assert
        var retrieved = storage.Get<TestOption>(ownerId, itemKey);
        Assert.That(retrieved!.Value, Is.EqualTo("second"));
    }

    [Test]
    public void Set_WithDifferentTypes_StoresMultipleTypes()
    {
        // Arrange
        var storage = CreateStorage();
        var ownerId = "owner1";
        var testOption = new TestOption { Value = "test" };
        var anotherOption = new AnotherTestOption { Data = "another" };

        // Act
        storage.Set(ownerId, "key1", testOption);
        storage.Set(ownerId, "key2", anotherOption);

        // Assert
        Assert.That(storage.Get<TestOption>(ownerId, "key1"), Is.Not.Null);
        Assert.That(storage.Get<AnotherTestOption>(ownerId, "key2"), Is.Not.Null);
    }

    // ==================== TRYADD Tests ====================

    [Test]
    public void TryAdd_WithNewKey_ReturnsTrue()
    {
        // Arrange
        var storage = CreateStorage();
        var ownerId = "owner1";
        var itemKey = "key1";
        var item = new TestOption { Value = "test" };

        // Act
        var result = storage.TryAdd(ownerId, itemKey, item);

        // Assert
        Assert.That(result, Is.True);
        Assert.That(storage.Get<TestOption>(ownerId, itemKey), Is.Not.Null);
    }

    [Test]
    public void TryAdd_WithExistingKey_ReturnsFalseAndDoesNotOverwrite()
    {
        // Arrange
        var storage = CreateStorage();
        var ownerId = "owner1";
        var itemKey = "key1";
        var item1 = new TestOption { Value = "first" };
        var item2 = new TestOption { Value = "second" };

        // Act
        storage.TryAdd(ownerId, itemKey, item1);
        var result = storage.TryAdd(ownerId, itemKey, item2);

        // Assert
        Assert.That(result, Is.False);
        Assert.That(storage.Get<TestOption>(ownerId, itemKey)!.Value, Is.EqualTo("first"));
    }

    [Test]
    public void TryAdd_SameDifferentOwners_BothSucceed()
    {
        // Arrange
        var storage = CreateStorage();
        var itemKey = "key1";
        var item1 = new TestOption { Value = "owner1_item" };
        var item2 = new TestOption { Value = "owner2_item" };

        // Act
        var result1 = storage.TryAdd("owner1", itemKey, item1);
        var result2 = storage.TryAdd("owner2", itemKey, item2);

        // Assert
        Assert.That(result1, Is.True);
        Assert.That(result2, Is.True);
    }

    // ==================== GET Tests ====================

    [Test]
    public void Get_WithExistingKey_ReturnsItem()
    {
        // Arrange
        var storage = CreateStorage();
        var ownerId = "owner1";
        var itemKey = "key1";
        var item = new TestOption { Value = "test" };
        storage.Set(ownerId, itemKey, item);

        // Act
        var retrieved = storage.Get<TestOption>(ownerId, itemKey);

        // Assert
        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved!.Value, Is.EqualTo("test"));
    }

    [Test]
    public void Get_WithNonExistingKey_ReturnsNull()
    {
        // Arrange
        var storage = CreateStorage();
        var ownerId = "owner1";

        // Act
        var retrieved = storage.Get<TestOption>(ownerId, "nonexistent");

        // Assert
        Assert.That(retrieved, Is.Null);
    }

    [Test]
    public void Get_WithNonExistingOwner_ReturnsNull()
    {
        // Arrange
        var storage = CreateStorage();

        // Act
        var retrieved = storage.Get<TestOption>("nonexistent", "key1");

        // Assert
        Assert.That(retrieved, Is.Null);
    }

    [Test]
    public void Get_WithWrongType_ReturnsNull()
    {
        // Arrange
        var storage = CreateStorage();
        var ownerId = "owner1";
        var itemKey = "key1";
        var item = new TestOption { Value = "test" };
        storage.Set(ownerId, itemKey, item);

        // Act
        var retrieved = storage.Get<AnotherTestOption>(ownerId, itemKey);

        // Assert
        Assert.That(retrieved, Is.Null);
    }

    // ==================== GETALL Tests ====================

    [Test]
    public void GetAll_WithStoredItems_ReturnsAllItemsOfType()
    {
        // Arrange
        var storage = CreateStorage();
        var ownerId = "owner1";
        var item1 = new TestOption { Value = "item1" };
        var item2 = new TestOption { Value = "item2" };
        storage.Set(ownerId, "key1", item1);
        storage.Set(ownerId, "key2", item2);

        // Act
        var retrieved = storage.GetAll<TestOption>(ownerId);

        // Assert
        Assert.That(retrieved, Has.Count.EqualTo(2));
        Assert.That(retrieved, Does.Contain(item1));
        Assert.That(retrieved, Does.Contain(item2));
    }

    [Test]
    public void GetAll_WithNoItems_ReturnsEmptyList()
    {
        // Arrange
        var storage = CreateStorage();
        var ownerId = "owner1";

        // Act
        var retrieved = storage.GetAll<TestOption>(ownerId);

        // Assert
        Assert.That(retrieved, Is.Empty);
    }

    [Test]
    public void GetAll_WithMixedTypes_ReturnsOnlyRequestedType()
    {
        // Arrange
        var storage = CreateStorage();
        var ownerId = "owner1";
        var testOption = new TestOption { Value = "test" };
        var anotherOption = new AnotherTestOption { Data = "another" };
        storage.Set(ownerId, "key1", testOption);
        storage.Set(ownerId, "key2", anotherOption);

        // Act
        var retrieved = storage.GetAll<TestOption>(ownerId);

        // Assert
        Assert.That(retrieved, Has.Count.EqualTo(1));
        Assert.That(retrieved[0].Value, Is.EqualTo("test"));
    }

    [Test]
    public void GetAll_WithNonExistingOwner_ReturnsEmptyList()
    {
        // Arrange
        var storage = CreateStorage();

        // Act
        var retrieved = storage.GetAll<TestOption>("nonexistent");

        // Assert
        Assert.That(retrieved, Is.Empty);
    }

    // ==================== REMOVE Tests ====================

    [Test]
    public void Remove_WithExistingKey_ReturnsTrue()
    {
        // Arrange
        var storage = CreateStorage();
        var ownerId = "owner1";
        var itemKey = "key1";
        var item = new TestOption { Value = "test" };
        storage.Set(ownerId, itemKey, item);

        // Act
        var result = storage.Remove(ownerId, itemKey);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void Remove_WithExistingKey_DeletesItem()
    {
        // Arrange
        var storage = CreateStorage();
        var ownerId = "owner1";
        var itemKey = "key1";
        var item = new TestOption { Value = "test" };
        storage.Set(ownerId, itemKey, item);

        // Act
        storage.Remove(ownerId, itemKey);
        var retrieved = storage.Get<TestOption>(ownerId, itemKey);

        // Assert
        Assert.That(retrieved, Is.Null);
    }

    [Test]
    public void Remove_WithNonExistingKey_ReturnsFalse()
    {
        // Arrange
        var storage = CreateStorage();
        var ownerId = "owner1";

        // Act
        var result = storage.Remove(ownerId, "nonexistent");

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void Remove_WithNonExistingOwner_ReturnsFalse()
    {
        // Arrange
        var storage = CreateStorage();

        // Act
        var result = storage.Remove("nonexistent", "key1");

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void Remove_OneOfMany_DeletesOnlyTarget()
    {
        // Arrange
        var storage = CreateStorage();
        var ownerId = "owner1";
        var item1 = new TestOption { Value = "item1" };
        var item2 = new TestOption { Value = "item2" };
        storage.Set(ownerId, "key1", item1);
        storage.Set(ownerId, "key2", item2);

        // Act
        storage.Remove(ownerId, "key1");

        // Assert
        Assert.That(storage.Get<TestOption>(ownerId, "key1"), Is.Null);
        Assert.That(storage.Get<TestOption>(ownerId, "key2"), Is.Not.Null);
    }

    // ==================== CLEAR Tests ====================

    [Test]
    public void Clear_WithExistingOwner_RemovesAllItems()
    {
        // Arrange
        var storage = CreateStorage();
        var ownerId = "owner1";
        var item1 = new TestOption { Value = "item1" };
        var item2 = new TestOption { Value = "item2" };
        storage.Set(ownerId, "key1", item1);
        storage.Set(ownerId, "key2", item2);

        // Act
        storage.Clear(ownerId);

        // Assert
        Assert.That(storage.GetAll<TestOption>(ownerId), Is.Empty);
    }

    [Test]
    public void Clear_WithMultipleOwners_ClearsOnlyTarget()
    {
        // Arrange
        var storage = CreateStorage();
        var item1 = new TestOption { Value = "owner1_item" };
        var item2 = new TestOption { Value = "owner2_item" };
        storage.Set("owner1", "key1", item1);
        storage.Set("owner2", "key1", item2);

        // Act
        storage.Clear("owner1");

        // Assert
        Assert.That(storage.GetAll<TestOption>("owner1"), Is.Empty);
        Assert.That(storage.GetAll<TestOption>("owner2"), Has.Count.EqualTo(1));
    }

    [Test]
    public void Clear_WithNonExistingOwner_DoesNotThrow()
    {
        // Arrange
        var storage = CreateStorage();

        // Act & Assert
        Assert.DoesNotThrow(() => storage.Clear("nonexistent"));
    }

    // ==================== GETOWNERIDS Tests ====================

    [Test]
    public void GetOwnerIds_WithNoOwners_ReturnsEmptyList()
    {
        // Arrange
        var storage = CreateStorage();

        // Act
        var ownerIds = storage.GetOwnerIds();

        // Assert
        Assert.That(ownerIds, Is.Empty);
    }

    [Test]
    public void GetOwnerIds_WithOwners_ReturnsAllOwnerIds()
    {
        // Arrange
        var storage = CreateStorage();
        var item = new TestOption { Value = "test" };
        storage.Set("owner1", "key1", item);
        storage.Set("owner2", "key1", item);
        storage.Set("owner3", "key1", item);

        // Act
        var ownerIds = storage.GetOwnerIds();

        // Assert
        Assert.That(ownerIds, Has.Count.EqualTo(3));
        Assert.That(ownerIds, Does.Contain("owner1"));
        Assert.That(ownerIds, Does.Contain("owner2"));
        Assert.That(ownerIds, Does.Contain("owner3"));
    }

    [Test]
    public void GetOwnerIds_AfterClear_ExcludesCleared()
    {
        // Arrange
        var storage = CreateStorage();
        var item = new TestOption { Value = "test" };
        storage.Set("owner1", "key1", item);
        storage.Set("owner2", "key1", item);
        storage.Clear("owner1");

        // Act
        var ownerIds = storage.GetOwnerIds();

        // Assert
        Assert.That(ownerIds, Has.Count.EqualTo(1));
        Assert.That(ownerIds, Does.Contain("owner2"));
    }

    // ==================== Test Helpers ====================

    private class TestOption
    {
        public string Value { get; set; } = string.Empty;
    }

    private class AnotherTestOption
    {
        public string Data { get; set; } = string.Empty;
    }
}
