using Microsoft.Extensions.Logging;

namespace Neatoo.RemoteFactory.Internal;

/// <summary>
/// Trace-level log message definitions for Neatoo entity/list serialization diagnostics.
/// Category: "Neatoo.Trace" — enable via Serilog/appsettings override.
/// </summary>
internal static partial class NeatooTraceLog
{
    // ===== Entity Converter Read (100xx) =====

    [LoggerMessage(
        EventId = 10001,
        Level = LogLevel.Trace,
        Message = "Entity Read: resolving type {TypeName}")]
    public static partial void EntityReadResolvingType(
        this ILogger logger,
        string typeName);

    [LoggerMessage(
        EventId = 10002,
        Level = LogLevel.Trace,
        Message = "Entity Read: {TypeName} resolved+created in {ElapsedMs}ms")]
    public static partial void EntityReadTypeResolved(
        this ILogger logger,
        string typeName,
        long elapsedMs);

    [LoggerMessage(
        EventId = 10003,
        Level = LogLevel.Trace,
        Message = "  Property {PropName} deserialized in {ElapsedMs}ms")]
    public static partial void EntityReadPropertyDeserialized(
        this ILogger logger,
        string propName,
        long elapsedMs);

    [LoggerMessage(
        EventId = 10004,
        Level = LogLevel.Trace,
        Message = "Entity Read: PropertyManager for {TypeName} — {PropCount} properties in {ElapsedMs}ms")]
    public static partial void EntityReadPropertyManagerComplete(
        this ILogger logger,
        string typeName,
        int propCount,
        long elapsedMs);

    [LoggerMessage(
        EventId = 10005,
        Level = LogLevel.Trace,
        Message = "Entity Read: SetProperties for {TypeName} in {ElapsedMs}ms")]
    public static partial void EntityReadSetProperties(
        this ILogger logger,
        string typeName,
        long elapsedMs);

    // ===== Entity Converter Write (101xx) =====

    [LoggerMessage(
        EventId = 10100,
        Level = LogLevel.Trace,
        Message = "Entity Write: {TypeName}")]
    public static partial void EntityWriteStarted(
        this ILogger logger,
        string typeName);

    [LoggerMessage(
        EventId = 10101,
        Level = LogLevel.Trace,
        Message = "Entity Write: {TypeName} completed in {ElapsedMs}ms ({PropCount} props)")]
    public static partial void EntityWriteCompleted(
        this ILogger logger,
        string typeName,
        long elapsedMs,
        int propCount);

    // ===== List Converter Read (102xx) =====

    [LoggerMessage(
        EventId = 10200,
        Level = LogLevel.Trace,
        Message = "List Read: resolving type {ListType}")]
    public static partial void ListReadResolvingType(
        this ILogger logger,
        string listType);

    [LoggerMessage(
        EventId = 10201,
        Level = LogLevel.Trace,
        Message = "  List item[{Index}] ({ItemType}) deserialized in {ElapsedMs}ms")]
    public static partial void ListReadItemDeserialized(
        this ILogger logger,
        int index,
        string itemType,
        long elapsedMs);

    [LoggerMessage(
        EventId = 10202,
        Level = LogLevel.Trace,
        Message = "List Read: {ListType} — {ItemCount} items in {ElapsedMs}ms")]
    public static partial void ListReadComplete(
        this ILogger logger,
        string listType,
        int itemCount,
        long elapsedMs);

    // ===== Entity Lifecycle (104xx) =====

    [LoggerMessage(
        EventId = 10400,
        Level = LogLevel.Trace,
        Message = "FactoryComplete {Operation} started for {TypeName} (IsNew={IsNew}, IsDeleted={IsDeleted}, IsModified={IsModified})")]
    public static partial void FactoryCompleteStarted(
        this ILogger logger,
        string operation,
        string typeName,
        bool isNew,
        bool isDeleted,
        bool isModified);

    [LoggerMessage(
        EventId = 10401,
        Level = LogLevel.Trace,
        Message = "FactoryComplete {Operation} finished for {TypeName} in {ElapsedMs}ms")]
    public static partial void FactoryCompleteFinished(
        this ILogger logger,
        string operation,
        string typeName,
        long elapsedMs);

    // ===== List Converter Write (103xx) =====

    [LoggerMessage(
        EventId = 10300,
        Level = LogLevel.Trace,
        Message = "List Write: {ListType}, {ActiveCount} active items")]
    public static partial void ListWriteStarted(
        this ILogger logger,
        string listType,
        int activeCount);

    [LoggerMessage(
        EventId = 10301,
        Level = LogLevel.Trace,
        Message = "List Write: {ListType} includes {DeletedCount} deleted items")]
    public static partial void ListWriteDeletedItems(
        this ILogger logger,
        string listType,
        int deletedCount);

    [LoggerMessage(
        EventId = 10302,
        Level = LogLevel.Trace,
        Message = "List Write: {ListType} completed in {ElapsedMs}ms ({TotalItems} total items)")]
    public static partial void ListWriteCompleted(
        this ILogger logger,
        string listType,
        long elapsedMs,
        int totalItems);
}
