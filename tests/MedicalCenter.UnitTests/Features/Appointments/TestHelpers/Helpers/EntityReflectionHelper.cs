using System.Reflection;

namespace MedicalCenter.UnitTests.Features.Appointments.TestHelpers.Helpers;

/// <summary>
/// Helper class for setting private/protected properties on entities via reflection.
/// Used by builders to construct entities with read-only properties.
/// </summary>
public static class EntityReflectionHelper
{
    /// <summary>
    /// Sets a property value on an object using reflection, including private setters.
    /// </summary>
    /// <typeparam name="T">The type of the object</typeparam>
    /// <param name="obj">The object instance</param>
    /// <param name="propertyName">The name of the property to set</param>
    /// <param name="value">The value to set</param>
    /// <exception cref="ArgumentException">Thrown when property is not found</exception>
    public static void SetProperty<T>(T obj, string propertyName, object? value)
    {
        if (obj is null)
        {
            throw new ArgumentNullException(nameof(obj));
        }

        var property = typeof(T).GetProperty(
            propertyName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (property is null)
        {
            throw new ArgumentException($"Property '{propertyName}' not found on type '{typeof(T).Name}'", nameof(propertyName));
        }

        property.SetValue(obj, value);
    }

    /// <summary>
    /// Sets a field value on an object using reflection, including private fields.
    /// </summary>
    /// <typeparam name="T">The type of the object</typeparam>
    /// <param name="obj">The object instance</param>
    /// <param name="fieldName">The name of the field to set</param>
    /// <param name="value">The value to set</param>
    /// <exception cref="ArgumentException">Thrown when field is not found</exception>
    public static void SetField<T>(T obj, string fieldName, object? value)
    {
        if (obj is null)
        {
            throw new ArgumentNullException(nameof(obj));
        }

        var field = typeof(T).GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (field is null)
        {
            throw new ArgumentException($"Field '{fieldName}' not found on type '{typeof(T).Name}'", nameof(fieldName));
        }

        field.SetValue(obj, value);
    }

    /// <summary>
    /// Gets a property value from an object using reflection, including private getters.
    /// </summary>
    /// <typeparam name="T">The type of the object</typeparam>
    /// <typeparam name="TValue">The expected type of the property value</typeparam>
    /// <param name="obj">The object instance</param>
    /// <param name="propertyName">The name of the property to get</param>
    /// <returns>The property value</returns>
    /// <exception cref="ArgumentException">Thrown when property is not found</exception>
    public static TValue? GetProperty<T, TValue>(T obj, string propertyName)
    {
        if (obj is null)
        {
            throw new ArgumentNullException(nameof(obj));
        }

        var property = typeof(T).GetProperty(
            propertyName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (property is null)
        {
            throw new ArgumentException($"Property '{propertyName}' not found on type '{typeof(T).Name}'", nameof(propertyName));
        }

        return (TValue?)property.GetValue(obj);
    }
}
