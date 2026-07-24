"""Generate the cCoder.CodeAnalysis website and rule documentation assets."""

from __future__ import annotations

import html
import json
import re
from datetime import datetime, timezone
from pathlib import Path


ASSETS_ROOT = Path(__file__).resolve().parents[1]
PAGES = ASSETS_ROOT / "Data" / "ccoder.co.uk" / "Default" / "Pages"
PAGE_ROLES = ASSETS_ROOT / "Data" / "ccoder.co.uk" / "Default" / "PageRoles"
ANALYZER = (
    ASSETS_ROOT.parent
    / "cCoder.CodeAnalysis"
    / "src"
    / "cCoder.CodeAnalysis"
    / "Analyzers"
    / "ArchitectureDiagnosticAnalyzer.cs"
)
RULE_SOURCES = ANALYZER.parents[1] / "Services" / "Processings" / "Rules"
COORDINATION_SOURCES = ANALYZER.parents[1] / "Services" / "Coordinations" / "Rules"

PREFIXES = {
    "STX": ("The Standard", "rules shared by Standard elements and service contracts"),
    "STXA": ("Aggregation services", "rules for combining several services of one variation"),
    "STXAPI": ("API controllers", "rules for HTTP-facing exposure controllers"),
    "STXAPP": ("Applications", "rules for .NET application composition and startup files"),
    "STXB": ("Brokers", "rules for thin integrations with storage and external systems"),
    "STXC": ("Coordination services", "rules for coordinating orchestration services"),
    "STXD": ("Dependencies", "rules for adapters that inherit from types supplied by external libraries"),
    "STXE": ("Exposures", "rules for boundary adapters that expose business capabilities"),
    "STXEX": ("Exception handling", "rules for classifying, wrapping and preserving exceptions"),
    "STXF": ("Foundation services", "rules for the lowest business-service layer"),
    "STXFORMAT": ("Source formatting", "rules that keep source layout predictable"),
    "STXMG": ("Management services", "rules for managing coordination services"),
    "STXM": ("Models", "rules for data-only model types"),
    "STXO": ("Orchestration services", "rules for orchestrating foundation or processing services"),
    "STXP": ("Processing services", "rules for entity-focused business processing"),
    "STXSTRUCT": ("Project structure", "rules for the conventional source-folder layout"),
    "STXTEST": ("Tests", "rules for explicit, independent and complete test suites"),
}

FALLBACK_DESCRIPTIONS = {
    "STX0001": "A Standard architectural element must carry either data or functionality, not both.",
    "STX0005": "A public service method must not call another public method on the same service.",
    "STX0019": "create model parameters must use the new prefix.",
    "STX0020": "update model parameters must use the updated prefix.",
    "STX0021": "delete model parameters must use the deleted prefix.",
    "STXC001": "The service must have two or three OrchestrationService dependencies.",
    "STXD001": "Dependency elements may only be consumed by brokers.",
    "STXF001": "A foundation service must not loop over its service model type.",
    "STXF002": "A foundation service may only depend on brokers, exposures, or nothing.",
    "STXMG001": "The service must have two or three CoordinationService dependencies.",
    "STXO001": "An orchestration must have two or three foundation or processing dependencies and must not mix them.",
    "STXO002": "An orchestration service name must contain the Orchestration identifier.",
}

RULE_EXAMPLES = {
    "STX0001": (
        "public class Order\n{\n    public Guid Id { get; set; }\n    public ValueTask Submit() => default;\n}",
        "public class Order\n"
        "{\n"
        "    public Guid Id { get; set; }\n"
        "}\n\n"
        "internal interface IOrderService\n"
        "{\n"
        "    ValueTask<Order> AddOrder(Order newOrder);\n"
        "}\n\n"
        "internal partial class OrderService(IOrderBroker orderBroker)\n"
        "    : IOrderService\n"
        "{\n"
        "    public ValueTask<Order> AddOrder(Order newOrder) => TryCatch(async () =>\n"
        "    {\n"
        "        await Validate(newOrder);\n\n"
        "        return await orderBroker.InsertOrderAsync(newOrder);\n"
        "    });\n"
        "}",
    ),
    "STX0002": (
        "internal interface IOrderService\n"
        "{\n"
        "    ValueTask<Order> RetrieveOrder(Guid orderId);\n"
        "}\n\n"
        "internal partial class OrderService(IOrderBroker orderBroker) : IOrderService\n"
        "{\n"
        "    public string Status { get; set; }\n\n"
        "    public ValueTask<Order> RetrieveOrder(Guid orderId) => TryCatch(async () =>\n"
        "    {\n"
        "        await Validate(orderId);\n\n"
        "        return await orderBroker.SelectOrderAsync(orderId);\n"
        "    });\n"
        "}",
        "public class Order\n"
        "{\n"
        "    public string Status { get; set; }\n"
        "}\n\n"
        "internal interface IOrderService\n"
        "{\n"
        "    ValueTask<Order> RetrieveOrder(Guid orderId);\n"
        "}\n\n"
        "internal partial class OrderService(IOrderBroker orderBroker) : IOrderService\n"
        "{\n"
        "    public ValueTask<Order> RetrieveOrder(Guid orderId) => TryCatch(async () =>\n"
        "    {\n"
        "        await Validate(orderId);\n\n"
        "        return await orderBroker.SelectOrderAsync(orderId);\n"
        "    });\n"
        "}",
    ),
    "STX0003": (
        "internal interface IOrderProcessingService\n"
        "{\n"
        "    ValueTask<IQueryable<Order>> RetrieveOrders();\n"
        "    ValueTask AddOrder(Order newOrder);\n"
        "    ValueTask UpdateOrder(Order updatedOrder);\n"
        "    ValueTask DeleteOrder(Order deletedOrder);\n"
        "}\n\n"
        "internal class OrderProcessingService(IOrderService orderService)\n"
        "    : IOrderProcessingService\n"
        "{\n"
        "    public ValueTask<IQueryable<Order>> RetrieveOrders() =>\n"
        "        orderService.RetrieveOrders();\n\n"
        "    public ValueTask AddOrder(Order newOrder) =>\n"
        "        orderService.AddOrder(newOrder);\n\n"
        "    public ValueTask UpdateOrder(Order updatedOrder) =>\n"
        "        orderService.UpdateOrder(updatedOrder);\n\n"
        "    public ValueTask DeleteOrder(Order deletedOrder) =>\n"
        "        orderService.DeleteOrder(deletedOrder);\n"
        "}",
        "// No code, file deleted",
    ),
    "STX0004": (
        "internal class OrderProcessingService(\n    ICustomerProcessingService customers) : IOrderProcessingService { }",
        "internal class OrderProcessingService(\n    IOrderService orders) : IOrderProcessingService { }",
    ),
    "STX0005": (
        "public ValueTask<Order> GetOrder(Guid orderId) =>\n    GetOrderById(orderId);",
        "public ValueTask<Order> GetOrder(Guid orderId) =>\n    GetOrderByIdAsync(orderId);",
    ),
    "STX0006": (
        "public class OrderProcessingService(IOrderService orders) : IOrderProcessingService { }",
        "internal class OrderProcessingService(IOrderService orders) : IOrderProcessingService { }",
    ),
    "STX0007": (
        "public ValueTask<Receipt> AddOrder(Order order, Customer customer) => default;",
        "public ValueTask<Order> AddOrder(Order newOrder) => default;",
    ),
    "STX0008": (
        "// StudentService.cs\n"
        "internal partial class StudentService(IStudentBroker studentBroker)\n"
        "    : IStudentService\n"
        "{\n"
        "    public ValueTask<Student> AddStudentAsync(Student newStudent) =>\n"
        "        TryCatch<Student>(operation: async () =>\n"
        "        {\n"
        "            await ValidateStudentOnAdd(newStudent);\n"
        "            await studentBroker.InsertStudentAsync(newStudent: newStudent);\n\n"
        "            return newStudent;\n"
        "        });\n\n"
        "    private static ValueTask ValidateStudentOnAdd(Student newStudent)\n"
        "    {\n"
        "        ValidationRulesEngine.Validate(inputs: newStudent);\n\n"
        "        return ValueTask.CompletedTask;\n"
        "    }\n"
        "}",
        "// StudentService.cs\n"
        "internal partial class StudentService(IStudentBroker studentBroker)\n"
        "    : IStudentService\n"
        "{\n"
        "    public ValueTask<Student> AddStudentAsync(Student newStudent) =>\n"
        "        TryCatch<Student>(operation: async () =>\n"
        "        {\n"
        "            await ValidateStudentOnAdd(newStudent);\n"
        "            await studentBroker.InsertStudentAsync(newStudent: newStudent);\n\n"
        "            return newStudent;\n"
        "        });\n"
        "}\n\n"
        "// StudentService.Validations.cs\n"
        "internal partial class StudentService\n"
        "{\n"
        "    private static ValueTask ValidateStudentOnAdd(Student newStudent)\n"
        "    {\n"
        "        ValidationRulesEngine.Validate(inputs: newStudent);\n\n"
        "        return ValueTask.CompletedTask;\n"
        "    }\n"
        "}",
    ),
    "STX0009": (
        "// StudentService.cs\n"
        "internal partial class StudentService(IStudentBroker studentBroker)\n"
        "    : IStudentService\n"
        "{\n"
        "    public ValueTask<Student> AddStudentAsync(Student newStudent) =>\n"
        "        TryCatch<Student>(operation: async () =>\n"
        "        {\n"
        "            await ValidateStudentOnAdd(newStudent);\n"
        "            await studentBroker.InsertStudentAsync(newStudent: newStudent);\n\n"
        "            return newStudent;\n"
        "        });\n\n"
        "    private static async ValueTask<T> TryCatch<T>(Func<ValueTask<T>> operation)\n"
        "    {\n"
        "        try\n"
        "        {\n"
        "            return await operation();\n"
        "        }\n"
        "        catch (ArgumentException innerException)\n"
        "        {\n"
        "            throw new StudentServiceValidationException(innerException);\n"
        "        }\n"
        "        catch (InvalidOperationException innerException)\n"
        "        {\n"
        "            throw new StudentServiceDependencyException(innerException);\n"
        "        }\n"
        "        catch (Exception innerException)\n"
        "        {\n"
        "            throw new StudentServiceException(innerException);\n"
        "        }\n"
        "    }\n"
        "}",
        "// StudentService.cs\n"
        "internal partial class StudentService(IStudentBroker studentBroker)\n"
        "    : IStudentService\n"
        "{\n"
        "    public ValueTask<Student> AddStudentAsync(Student newStudent) =>\n"
        "        TryCatch<Student>(operation: async () =>\n"
        "        {\n"
        "            await ValidateStudentOnAdd(newStudent);\n"
        "            await studentBroker.InsertStudentAsync(newStudent: newStudent);\n\n"
        "            return newStudent;\n"
        "        });\n"
        "}\n\n"
        "// StudentService.Exceptions.cs\n"
        "internal partial class StudentService\n"
        "{\n"
        "    private static async ValueTask<T> TryCatch<T>(Func<ValueTask<T>> operation)\n"
        "    {\n"
        "        try\n"
        "        {\n"
        "            return await operation();\n"
        "        }\n"
        "        catch (ArgumentException innerException)\n"
        "        {\n"
        "            throw new StudentServiceValidationException(innerException);\n"
        "        }\n"
        "        catch (InvalidOperationException innerException)\n"
        "        {\n"
        "            throw new StudentServiceDependencyException(innerException);\n"
        "        }\n"
        "        catch (Exception innerException)\n"
        "        {\n"
        "            throw new StudentServiceException(innerException);\n"
        "        }\n"
        "    }\n"
        "}",
    ),
    "STX0010": (
        "public ValueTask<Order> GetOrder(Guid orderId) =>\n    GetOrderByIdAsync(orderId);",
        "public ValueTask<Order> GetOrder(Guid orderId) =>\n    TryCatch(() => GetOrderByIdAsync(orderId));",
    ),
    "STX0011": (
        "public ValueTask<Order> AddOrderAsync(Order newOrder) =>\n    TryCatch(() => broker.InsertOrderAsync(newOrder));",
        "public ValueTask<Order> AddOrderAsync(Order newOrder) => TryCatch(async () =>\n{\n    ValidateOrderOnAdd(newOrder: newOrder);\n    return await broker.InsertOrderAsync(newOrder: newOrder);\n});",
    ),
    "STX0012": (
        "private static void ValidateOrderOnAdd(Order newOrder)\n{\n    if (newOrder is null)\n    {\n        throw new NullOrderException();\n    }\n}",
        "private static void ValidateOrderOnAdd(Order newOrder) =>\n    Validate(\n        message: \"Order is invalid.\",\n        (Rule: IsInvalid(newOrder.Id), Parameter: nameof(Order.Id)),\n        (Rule: IsInvalid(newOrder.Name), Parameter: nameof(Order.Name)));\n\n"
        "private static void Validate(\n    string message,\n    params (dynamic Rule, string Parameter)[] validations)\n{\n    var invalidOrderException = new InvalidOrderException(message);\n\n    foreach ((dynamic rule, string parameter) in validations)\n    {\n        if (rule.Condition)\n        {\n            invalidOrderException.UpsertDataList(\n                key: parameter,\n                value: rule.Message);\n        }\n    }\n\n    invalidOrderException.ThrowIfContainsErrors();\n}",
    ),
    "STX0013": (
        "internal class OrderProcessingService { }",
        "internal class OrderProcessingService : IOrderProcessingService { }",
    ),
    "STX0014": (
        "internal interface IOrders { }\ninternal class OrderProcessingService : IOrders { }",
        "internal interface IOrderProcessingService { }\ninternal class OrderProcessingService : IOrderProcessingService { }",
    ),
    "STX0015": (
        "interface IOrderService { ValueTask<Order> GetOrder(Guid orderId); }\nclass OrderService : IOrderService\n{\n    public ValueTask DeleteOrder(Guid orderId) => default;\n}",
        "interface IOrderService\n{\n    ValueTask<Order> GetOrder(Guid orderId);\n    ValueTask DeleteOrder(Guid orderId);\n}",
    ),
    "STX0016": (
        "public ValueTask<Order> InsertOrder(Order newOrder) => default;",
        "public ValueTask<Order> AddOrder(Order newOrder) => default;",
    ),
    "STX0017": (
        "public ValueTask<Order> GetOrder(Guid id) => default;",
        "public ValueTask<Order> GetOrder(Guid orderId) => default;",
    ),
    "STX0018": (
        "public ValueTask<Order> Add(Order newOrder) => default;",
        "public ValueTask<Order> AddOrder(Order newOrder) => default;",
    ),
    "STX0019": (
        "public ValueTask<Order> AddOrder(Order order) => default;",
        "public ValueTask<Order> AddOrder(Order newOrder) => default;",
    ),
    "STX0020": (
        "public ValueTask<Order> UpdateOrder(Order order) => default;",
        "public ValueTask<Order> UpdateOrder(Order updatedOrder) => default;",
    ),
    "STX0021": (
        "public ValueTask DeleteOrder(Order order) => default;",
        "public ValueTask DeleteOrder(Order deletedOrder) => default;",
    ),
    "STX0022": (
        "public ValueTask<Order> Create() => ValueTask.FromResult(new Order());",
        "public ValueTask<Order> CreateOrder() => ValueTask.FromResult(new Order());",
    ),
    "STX0023": (
        "public ValueTask<Order> AddOrderAsync(Order newOrder) => TryCatch(async () =>\n{\n    Validate(newOrder);\n    return await broker.InsertOrderAsync(newOrder: newOrder);\n});",
        "public ValueTask<Order> AddOrderAsync(Order newOrder) => TryCatch(async () =>\n{\n    ValidateOrderOnAdd(newOrder: newOrder);\n    return await broker.InsertOrderAsync(newOrder: newOrder);\n});",
    ),
    "STXAPP001": (
        "// Services/Startup/IServiceCollectionExtensions.cs\npublic static class IServiceCollectionExtensions { }",
        "// IServiceCollectionExtensions.cs (project root)\npublic static class IServiceCollectionExtensions { }",
    ),
    "STXAPP002": (
        "public static IServiceCollection ConfigureServices(\n    this IServiceCollection services) =>\n        services;",
        "public static IServiceCollection AddSchools(\n    this IServiceCollection services) =>\n        services;",
    ),
    "STXAPP003": (
        "services.AddScoped<SchoolConfiguration>();",
        "services.AddSingleton<SchoolConfiguration>();",
    ),
    "STXAPP004": (
        "public static WebApplication StartSchoolServices(\n    this WebApplication application) =>\n        application;",
        "public static WebApplication StartSchoolServices(\n    this WebApplication application)\n{\n    application.Services\n        .GetRequiredService<ISchoolBackgroundService>()\n        .Start();\n\n    return application;\n}",
    ),
    "STXAPP005": (
        "using Schools.Controllers;\nusing Schools.Services;\n\nWebApplicationBuilder builder = WebApplication.CreateBuilder(args);",
        "using Schools;\n\nWebApplicationBuilder builder = WebApplication.CreateBuilder(args);\nbuilder.Services.AddSchools();",
    ),
    "STXA001": (
        "class OrderAggregationService(\n    IOrderService orders,\n    ICustomerProcessingService customers) : IOrderAggregationService { }",
        "class OrderAggregationService(\n    IOrderService orders,\n    ICustomerService customers) : IOrderAggregationService { }",
    ),
    "STXA002": (
        "class OrderService(IOrderService orders) : IOrderService { }",
        "class OrderAggregationService(IOrderService orders) : IOrderAggregationService { }",
    ),
    "STXAPI001": (
        "class OrdersController(IOrderService orders, ICustomerService customers) { }",
        "class OrdersController(IOrderService orders) { }",
    ),
    "STXAPI002": (
        "class OrdersController\n{\n    public ValueTask<Order> Get() => default;\n    public ValueTask<Customer> Post(Customer customer) => default;\n}",
        "class OrdersController\n{\n    public ValueTask<Order> Get() => default;\n    public ValueTask<Order> Post(Order order) => default;\n}",
    ),
    "STXAPI003": ("class OrdersEndpoint { }", "class OrdersController { }"),
    "STXAPI004": (
        "public ValueTask<Order> Add(Order order) => service.AddOrder(order);",
        "public ValueTask<Order> Post(Order order) => service.AddOrder(order);",
    ),
    "STXB001": (
        "class OrderBroker(IOrderDb db, IAuditApi audit) { }",
        "class OrderBroker(IOrderDb db) { }",
    ),
    "STXB002": (
        "ValueTask<Order> SelectOrder(Guid id) => id == Guid.Empty ? default : db.SelectAsync(id);",
        "ValueTask<Order> SelectOrder(Guid id) => db.SelectAsync(id);",
    ),
    "STXB003": (
        "async ValueTask InsertOrders(Order[] orders)\n{\n    foreach (Order order in orders)\n    {\n        await db.InsertAsync(order);\n    }\n}",
        "ValueTask InsertOrders(Order[] orders) => db.InsertAsync(orders);",
    ),
    "STXB004": ("class OrderBroker { }", "class OrderBroker : IOrderBroker { }"),
    "STXB005": (
        "async ValueTask<Order> SelectOrder(Guid id)\n{\n    try { return await db.SelectAsync(id); } catch { return null; }\n}",
        "ValueTask<Order> SelectOrder(Guid id) => db.SelectAsync(id);",
    ),
    "STXB006": (
        "class OrderBroker(IOrderProcessingService orders) { }",
        "class OrderBroker(IOrderDbDependency orders) { }",
    ),
    "STXB007": (
        "ValueTask<Order> GetOrder(Guid orderId) => db.GetAsync(orderId);",
        "ValueTask<Order> SelectOrder(Guid orderId) => db.SelectAsync(orderId);",
    ),
    "STXC001": (
        "class OrderCoordinationService(IOrderProcessingService orders) : IOrderCoordinationService { }",
        "class OrderCoordinationService(\n    IOrderOrchestrationService orders,\n    IPaymentOrchestrationService payments) : IOrderCoordinationService { }",
    ),
    "STXC002": (
        "class OrderService(IOrderOrchestrationService orders) : IOrderService { }",
        "class OrderCoordinationService(IOrderOrchestrationService orders) : IOrderCoordinationService { }",
    ),
    "STXE001": (
        "ValueTask<Order> Get(Guid id) => id == Guid.Empty ? default : service.GetOrder(id);",
        "ValueTask<Order> Get(Guid id) => service.GetOrder(id);",
    ),
    "STXE002": (
        "async ValueTask Post(Order[] orders)\n{\n    foreach (Order order in orders)\n    {\n        await service.AddOrder(order);\n    }\n}",
        "ValueTask Post(Order[] orders) => service.AddOrders(orders);",
    ),
    "STXE003": (
        "class OrdersController(IOrderService orders, IPaymentService payments) { }",
        "class OrdersController(IOrderService orders) { }",
    ),
    "STXE004": (
        "class OrdersController(IOrderBroker orders) { }",
        "class OrdersController(IOrderService orders) { }",
    ),
    "STXE005": (
        "async ValueTask<Order> Get(Guid id)\n{\n    Order order = await service.GetOrder(id);\n    return await service.EnrichOrder(order);\n}",
        "ValueTask<Order> Get(Guid id) => service.GetEnrichedOrder(id);",
    ),
    "STXEX001": (
        "catch (OrderValidationException exception) { throw; }",
        "catch (OrderValidationException exception)\n{\n    throw new OrderServiceValidationException(message, exception);\n}",
    ),
    "STXEX002": (
        "catch (OrderDependencyException exception) { throw; }",
        "catch (OrderDependencyException exception)\n{\n    throw new OrderServiceDependencyException(message, exception);\n}",
    ),
    "STXEX003": (
        "catch (Exception exception) { throw new Exception(message); }",
        "catch (Exception exception)\n{\n    throw new OrderServiceException(message, exception);\n}",
    ),
    "STXF001": (
        "foreach (Order order in orders) broker.InsertOrder(order);",
        "broker.InsertOrders(orders);",
    ),
    "STXF002": (
        "class OrderService(IOrderProcessingService orders) : IOrderService { }",
        "class OrderService(IOrderBroker orders) : IOrderService { }",
    ),
    "STXF003": (
        "ValueTask AddOrders(IEnumerable<Order> newOrders) => broker.InsertOrders(newOrders);",
        "ValueTask<Order> AddOrder(Order newOrder) => broker.InsertOrder(newOrder);",
    ),
    "STXFORMAT001": ("class Order { }\n\n", "class Order { }"),
    "STXFORMAT002": (
        "ValueTask<Order> GetOrder() => service.GetOrder();",
        "ValueTask<Order> GetOrder() =>\n    service.GetOrder();",
    ),
    "STXFORMAT003": (
        "Validate(order);\nif (order.IsReady)\n{\n    Submit(order);\n}\nNotify(order);",
        "Validate(order);\n\nif (order.IsReady)\n{\n    Submit(order);\n}\n\nNotify(order);",
    ),
    "STXFORMAT004": (
        "ValueTask Add() => default;\nValueTask Delete() => default;",
        "ValueTask Add() => default;\n\nValueTask Delete() => default;",
    ),
    "STXFORMAT005": (
        "service.AddOrder(order, true);",
        "service.AddOrder(newOrder: order, notify: true);",
    ),
    "STXFORMAT006": (
        "ValueTask<Order> Get()\n{\n    return service.GetOrder(\n        includeItems: true);\n}",
        "ValueTask<Order> Get() =>\n    service.GetOrder(\n        includeItems: true);",
    ),
    "STXFORMAT007": (
        "if (isReady)\n    Submit();",
        "if (isReady)\n{\n    Submit();\n}",
    ),
    "STXFORMAT008": (
        "Validate();\nOrder order = service.GetOrder(\n    orderId: orderId);\nSubmit(order);",
        "Validate();\n\nOrder order = service.GetOrder(\n    orderId: orderId);\n\nSubmit(order);",
    ),
    "STXFORMAT009": (
        "orders.Where(order => order.IsReady).ToArray();",
        "orders\n    .Where(order => order.IsReady)\n    .ToArray();",
    ),
    "STXFORMAT010": (
        "// Submit the order.\nSubmit(order);",
        "/// <summary>Submits an order.</summary>\nValueTask SubmitOrder(Order order) => default;",
    ),
    "STXFORMAT011": (
        "namespace Orders;\n\nclass Order { }",
        "// ---------------------------------------------------------------\n// Copyright (c) Paul.Ward@ccoder.co.uk\n// ---------------------------------------------------------------\n\nnamespace Orders;",
    ),
    "STXFORMAT012": (
        "async ValueTask<Order> GetOrderAsync() => await service.GetOrderAsync();",
        "ValueTask<Order> GetOrderAsync() => service.GetOrderAsync();",
    ),
    "STXFORMAT013": (
        "// Student.cs uses LF, while the project uses CRLF\nclass Student { }",
        "// Every C# file in this project uses the same line endings\nclass Student { }",
    ),
    "STXMG001": (
        "class OrderManagementService(IOrderProcessingService orders) : IOrderManagementService { }",
        "class OrderManagementService(\n    IOrderCoordinationService orders,\n    IPaymentCoordinationService payments) : IOrderManagementService { }",
    ),
    "STXMG002": (
        "class OrderService(IOrderCoordinationService orders) : IOrderService { }",
        "class OrderManagementService(IOrderCoordinationService orders) : IOrderManagementService { }",
    ),
    "STXM001": (
        "class Order\n{\n    ValueTask<string> GetStatus() => ValueTask.FromResult(Status);\n    string Status { get; set; }\n}",
        "class Order\n{\n    string Status { get; set; }\n}",
    ),
    "STXM002": (
        "class Student\n{\n    public string FirstName { get; set; } = string.Empty;\n}",
        "class Student\n{\n    public string FirstName { get; set; }\n}",
    ),
    "STXM003": (
        "class Student\n"
        "{\n"
        "    [Required]\n"
        "    public required string FirstName { get; set; }\n"
        "}",
        "class Student\n"
        "{\n"
        "    public string FirstName { get; set; }\n"
        "}\n\n"
        "partial class StudentService\n"
        "{\n"
        "    public ValueTask<Student> AddStudentAsync(Student newStudent) =>\n"
        "        TryCatch(async () =>\n"
        "        {\n"
        "            ValidateStudentOnAdd(newStudent: newStudent);\n"
        "            return await studentBroker.InsertStudentAsync(newStudent: newStudent);\n"
        "        });\n"
        "\n"
        "    private static void ValidateStudentOnAdd(Student newStudent) =>\n"
        "        Validate(\n"
        "            message: \"Student is invalid.\",\n"
        "            (Rule: IsInvalid(newStudent.FirstName), Parameter: nameof(Student.FirstName)));\n"
        "}",
    ),
    "STXD001": (
        "internal class StudentProcessingService(\n"
        "    ExternalFrameworkDependency frameworkDependency)\n"
        "    : IStudentProcessingService\n"
        "{\n"
        "}",
        "internal class StudentBroker(\n"
        "    ExternalFrameworkDependency frameworkDependency)\n"
        "    : IStudentBroker\n"
        "{\n"
        "}",
    ),
    "STXO001": (
        "class OrderOrchestrationService(\n    IOrderService orders,\n    IPaymentProcessingService payments) : IOrderOrchestrationService { }",
        "class OrderOrchestrationService(\n    IOrderService orders,\n    IPaymentService payments) : IOrderOrchestrationService { }",
    ),
    "STXO002": (
        "class OrderService(IOrderService orders, IPaymentService payments) : IOrderService { }",
        "class OrderOrchestrationService(IOrderService orders, IPaymentService payments) : IOrderOrchestrationService { }",
    ),
    "STXP001": (
        "class OrderProcessingService(\n    IOrderService orders,\n    ICustomerService customers) : IOrderProcessingService { }",
        "class OrderProcessingService(IOrderService orders) : IOrderProcessingService { }",
    ),
    "STXP002": (
        "class OrderService(IOrderService orders) : IOrderService { }",
        "class OrderProcessingService(IOrderService orders) : IOrderProcessingService { }",
    ),
    "STXP003": (
        "class CustomerProcessingService(IOrderService orders) : ICustomerProcessingService { }",
        "class OrderProcessingService(IOrderService orders) : IOrderProcessingService { }",
    ),
    "STXSTRUCT001": (
        "// Services/OrderBroker.cs\nclass OrderBroker { }",
        "// Brokers/OrderBroker.cs\nclass OrderBroker { }",
    ),
    "STXTEST001": (
        "class OrderServiceTests<TOrder> { }",
        "class OrderServiceTests { }",
    ),
    "STXTEST002": (
        "class OrderServiceTests : ServiceTestsBase { }",
        "class OrderServiceTests { }",
    ),
    "STXTEST003": (
        "class OrdersTests { } // Tests OrderService",
        "class OrderServiceTests { }",
    ),
    "STXTEST004": (
        "class OrderServiceTests { }",
        "partial class OrderServiceTests { }",
    ),
    "STXTEST005": (
        "[Fact]\nasync ValueTask ShouldAddOrder()\n{\n    Order order = CreateOrder();\n    await service.AddOrder(order);\n    broker.VerifyInsert(order);\n}",
        "[Fact]\nasync ValueTask ShouldAddOrder()\n{\n    // given\n    Order order = CreateOrder();\n\n    // when\n    await service.AddOrder(order);\n\n    // then\n    broker.VerifyInsert(order);\n}",
    ),
    "STXTEST006": (
        "// Acceptance suite covers only Get and Post.",
        "[Fact] ValueTask ShouldGet() => default;\n[Fact] ValueTask ShouldPost() => default;\n[Fact] ValueTask ShouldPut() => default;\n[Fact] ValueTask ShouldDelete() => default;",
    ),
}

SAMPLE_VOCABULARY = (
    ("Orders", "Students"),
    ("orders", "students"),
    ("Order", "Student"),
    ("order", "student"),
    ("Customers", "Courses"),
    ("customers", "courses"),
    ("Customer", "Course"),
    ("customer", "course"),
    ("Payments", "Schools"),
    ("payments", "schools"),
    ("Payment", "School"),
    ("payment", "school"),
)


def use_sample_vocabulary(example: str) -> str:
    """Keep documentation examples in the domain language used by the sample project."""
    for existing, replacement in SAMPLE_VOCABULARY:
        example = example.replace(existing, replacement)

    return example


STANDARD_SOURCE_LINKS = {
    "STX": "https://github.com/hassanhabib/The-Standard/blob/master/2.%20Services/2.%20Services.md",
    "STXA": "https://github.com/hassanhabib/The-Standard/blob/master/2.%20Services/2.4%20Aggregations/2.4%20Aggregations.md",
    "STXAPI": "https://github.com/hassanhabib/The-Standard/blob/master/3.%20Exposers/3.1%20Communication%20Protocols/3.1%20Communication%20Protocols.md",
    "STXAPP": "https://github.com/hassanhabib/The-Standard",
    "STXB": "https://github.com/hassanhabib/The-Standard/blob/master/1.%20Brokers/1.%20Brokers.md",
    "STXC": "https://github.com/hassanhabib/The-Standard/blob/master/2.%20Services/2.%20Services.md",
    "STXD": "https://github.com/hassanhabib/The-Standard/blob/master/0.%20Introduction/0.0%20The%20Theory.md#0021-dependency",
    "STXE": "https://github.com/hassanhabib/The-Standard/blob/master/3.%20Exposers/3.%20Exposers.md",
    "STXEX": "https://github.com/hassanhabib/The-Standard/blob/master/2.%20Services/2.1%20Foundations/2.1%20Foundations.md",
    "STXF": "https://github.com/hassanhabib/The-Standard/blob/master/2.%20Services/2.1%20Foundations/2.1%20Foundations.md",
    "STXFORMAT": "https://github.com/hassanhabib/The-Standard",
    "STXMG": "https://github.com/hassanhabib/The-Standard/blob/master/2.%20Services/2.%20Services.md",
    "STXM": "https://github.com/hassanhabib/The-Standard/blob/master/0.%20Introduction/0.1%20Purposing%2C%20Modeling%20%26%20Simulation.md",
    "STXO": "https://github.com/hassanhabib/The-Standard/blob/master/2.%20Services/2.3%20Orchestrations/2.3%20Orchestrations.md",
    "STXP": "https://github.com/hassanhabib/The-Standard/blob/master/2.%20Services/2.2%20Processings/2.2%20Processings.md",
    "STXSTRUCT": "https://github.com/hassanhabib/The-Standard",
    "STXTEST": "https://github.com/hassanhabib/The-Standard",
}

C_SHARP_KEYWORDS = {
    "abstract", "as", "async", "await", "bool", "break", "byte", "case", "catch",
    "char", "class", "const", "continue", "decimal", "default", "delegate", "do",
    "double", "else", "enum", "event", "explicit", "extern", "false", "finally",
    "fixed", "float", "for", "foreach", "if", "implicit", "in", "int", "interface",
    "internal", "is", "lock", "long", "namespace", "new", "null", "object", "operator",
    "out", "override", "params", "private", "protected", "public", "readonly", "record",
    "ref", "return", "sealed", "short", "static", "string", "struct", "switch", "this",
    "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort",
    "using", "var", "virtual", "void", "volatile", "while", "with", "yield",
}

METHOD_DECLARATION = re.compile(
    r"(?m)^\s*"
    r"(?:(?:public|private|protected|internal|static|partial|virtual|override|abstract|async)\s+)*"
    r"(?!class\b|interface\b|record\b|struct\b|throw\b|return\b|await\b|foreach\b|if\b|catch\b)"
    r"([A-Za-z_]\w*(?:<[^;\n]+>)?(?:\[\])?)\s+\w+\([^;\n]*\)\s*(?:=>|\{|;)"
)


def validate_rule_examples() -> None:
    """Keep examples aligned with the baseline conventions they are not illustrating."""
    errors: list[str] = []

    for code, examples in RULE_EXAMPLES.items():
        for label, example in zip(("bad", "good"), examples):
            has_property_initializer = bool(
                re.search(r"\{\s*get;\s*set;\s*\}\s*=", example)
            )

            if has_property_initializer and (code, label) != ("STXM002", "bad"):
                errors.append(
                    f"{code} {label}: initializes a model property outside STXM002 bad"
                )

            if (code, label) == ("STXM002", "bad") and not has_property_initializer:
                errors.append("STXM002 bad: must demonstrate a property initializer")

            if re.search(r"\bsealed\b", example):
                errors.append(f"{code} {label}: uses sealed")

            if "FoundationService" in example:
                errors.append(f"{code} {label}: uses FoundationService in a type name")

            for return_type in METHOD_DECLARATION.findall(example):
                if code not in {"STX0012", "STXM003"} and not code.startswith("STXAPP") and not return_type.startswith("ValueTask"):
                    errors.append(
                        f"{code} {label}: method returns {return_type}, not ValueTask"
                    )

    if errors:
        raise ValueError("Invalid rule examples:\n" + "\n".join(errors))

CODE_STYLES = """<style class="code-analysis-syntax">
.docs-content pre{overflow-x:auto;margin:1rem 0 1.75rem;padding:1.25rem 1.4rem;border:1px solid rgba(20,42,72,.16);border-left:4px solid #0e6fae;border-radius:4px;background:#f7fbfe;color:#10263a}
.docs-content pre code{white-space:pre;color:inherit;background:transparent;font-size:1rem;line-height:1.65}
.language-csharp .syntax-comment{color:#587287;font-style:italic}.language-csharp .syntax-string{color:#8a3d12}.language-csharp .syntax-keyword{color:#6542a6;font-weight:700}.language-csharp .syntax-number{color:#096b65}.language-csharp .syntax-type{color:#0a6680;font-weight:600}
</style>"""


def analyzer_codes() -> list[str]:
    source = ANALYZER.read_text(encoding="utf-8-sig")
    return re.findall(r'"(STX[A-Z]*\d+)"', source.split("};", 1)[0])


def rule_descriptions() -> dict[str, str]:
    sources = list(RULE_SOURCES.glob("*.cs")) + list(COORDINATION_SOURCES.glob("*.cs"))
    text = "\n".join(path.read_text(encoding="utf-8-sig") for path in sources)
    descriptions = dict(FALLBACK_DESCRIPTIONS)
    for code in analyzer_codes():
        match = re.search(rf'"{code}"\s*,\s*"([^"]+)"', text)
        if match and code not in {"STX0019", "STX0020", "STX0021"}:
            descriptions[code] = match.group(1)
    return descriptions


def prefix_for(code: str) -> str:
    return re.match(r"[A-Z]+", code).group(0)


def example_for(code: str, description: str) -> tuple[str, str]:
    bad, good = (use_sample_vocabulary(example) for example in RULE_EXAMPLES[code])
    return (f"// Violates {code}: {description}\n{bad}", f"// Follows {code}\n{good}")


def highlight_csharp(source: str) -> str:
    token_pattern = re.compile(
        r'(/\*[\s\S]*?\*/|//.*?$|@?"(?:""|\\.|[^"\\])*"|\'(?:\\.|[^\'\\])\'|\b\d+(?:\.\d+)?\b|\b[A-Za-z_][A-Za-z0-9_]*\b)',
        re.MULTILINE,
    )
    output: list[str] = []
    cursor = 0
    for match in token_pattern.finditer(source):
        output.append(html.escape(source[cursor:match.start()]))
        token = match.group(0)
        class_name = ""
        if token.startswith("//") or token.startswith("/*"):
            class_name = "syntax-comment"
        elif token.startswith(('"', '@"', "'")):
            class_name = "syntax-string"
        elif token[0].isdigit():
            class_name = "syntax-number"
        elif token in C_SHARP_KEYWORDS:
            class_name = "syntax-keyword"
        elif token[0].isupper():
            class_name = "syntax-type"
        escaped = html.escape(token)
        output.append(f'<span class="{class_name}">{escaped}</span>' if class_name else escaped)
        cursor = match.end()
    output.append(html.escape(source[cursor:]))
    return "".join(output)


def page(
    path: str,
    name: str,
    order: int,
    layout: str,
    body: str,
    description: str,
    title: str | None = None,
) -> dict:
    return {
        "Path": path,
        "Name": name,
        "ResourceKey": "Default",
        "ShowOnMenus": True,
        "Order": order,
        "LastUpdated": datetime.now(timezone.utc).isoformat(),
        "Layout": layout,
        "Contents": [{"CultureId": "", "Name": "body", "Html": body}],
        "PageInfo": [{"CultureId": "", "Description": description, "Title": title or name}],
    }


def write_page(filename: str, value: dict) -> None:
    (PAGES / filename).write_text(json.dumps(value, indent=2) + "\n", encoding="utf-8")


def write_role(filename: str, path: str, role: str) -> None:
    value = {"Path": path, "Role": role}
    (PAGE_ROLES / filename).write_text(json.dumps(value, indent=2) + "\n", encoding="utf-8")


def docs_shell(introduction: str, include_navigation: bool = False) -> str:
    navigation = "[component[DetailedNav]]" if include_navigation else ""
    return (
        '<header class="docs-page-header"><h1>[page[title]]</h1>'
        f"<p>{html.escape(introduction)}</p></header>{navigation}"
    )


def main() -> None:
    validate_rule_examples()
    codes = analyzer_codes()
    descriptions = rule_descriptions()

    domain_body = """<div class="public-flow"><section class="public-flow-section public-section"><div class="public-section-frame"><div class="public-article public-domain-article">
  <h1>[page[title]]</h1>
  <p class="public-copy">Turn The Standard into immediate, navigable feedback while developers write and build C#.</p>
  <div class="public-actions"><a class="btn btn-primary" href="/Platform-Domains"><span class="k-icon k-i-grid-layout"></span>All platform domains</a><a class="btn btn-secondary" href="https://github.com/ccoder-co-uk/cCoder.CodeAnalysis" target="_blank" rel="noreferrer"><span class="k-icon k-i-source-code"></span>View the GitHub repository</a><a class="btn btn-secondary" href="/Documentation/CodeAnalysis"><span class="k-icon k-i-file"></span>Read the documentation</a></div>
</div></div></section><section class="public-flow-section public-section"><div class="public-section-frame"><article class="public-article public-domain-article"><h2>What it owns</h2><ul><li>Roslyn diagnostics for cCoder architecture, naming, structure, formatting and tests.</li><li>Architecture reports produced from the same rule evaluations.</li><li>Rule identifiers and documentation links that make every warning actionable.</li></ul><h2>Why the boundary matters</h2><p>The package keeps The Standard executable and independently versioned. A warning identifies both the breached rule and the guidance needed to correct it, reducing interpretation differences between developers and AI coding tools.</p><h2>How it fits</h2><p>Applications consume cCoder.CodeAnalysis as an analyser package. It observes the compilation, classifies Standard elements and reports warnings without becoming a runtime dependency.</p><h2>Getting started</h2><h3>Reference the package</h3><pre><code class="language-xml">&lt;PackageReference Include="cCoder.CodeAnalysis" Version="YOUR_VERSION" PrivateAssets="all" /&gt;</code></pre><p>Keep <code>PrivateAssets="all"</code> when the analyser is a development-time concern and should not flow transitively to consumers.</p><h3>Build normally</h3><pre><code class="language-powershell">dotnet restore
dotnet build</code></pre><p>Warnings appear in the IDE and build output. Select a diagnostic or follow its help link to read the rule, its Standard context and corrected example.</p><h3>Adopt deliberately</h3><p>Start by fixing architectural classification and dependency-flow warnings, then naming, structure and formatting. Do not suppress a warning merely to make the build quiet: use the linked rule page to confirm the intended design first.</p></article></div></section></div>"""
    domain_path = "/Platform-Domains/Code-Analysis"
    write_page(
        "_Platform-Domains_Code-Analysis.json",
        page(
            domain_path,
            "Code-Analysis",
            2,
            "PublicContent",
            domain_body,
            "Executable guidance for applying The Standard to C# architecture, source layout and tests.",
            "cCoder.CodeAnalysis",
        ),
    )
    for role in ("Administrators", "Guests", "Users"):
        write_role(
            f"Platform-Domains_Code-Analysis-{role}.json",
            domain_path,
            role,
        )

    root_path = "Documentation/CodeAnalysis"
    write_page(
        "Documentation_CodeAnalysis.json",
        page(
            root_path,
            "CodeAnalysis",
            15,
            "Documentation",
            docs_shell(
                "Install the analyser, understand its diagnostic families, and follow each warning to practical guidance.",
                True,
            ),
            "Getting started and rule reference for the cCoder.CodeAnalysis package.",
            "cCoder.CodeAnalysis",
        ),
    )
    write_role("Documentation_CodeAnalysis-Guests.json", root_path, "Guests")

    grouped: dict[str, list[str]] = {}
    for code in codes:
        grouped.setdefault(prefix_for(code), []).append(code)

    for prefix_order, (prefix, prefix_codes) in enumerate(grouped.items(), 1):
        title, meaning = PREFIXES[prefix]
        prefix_path = f"{root_path}/{prefix}"
        prefix_body = docs_shell(
            f"{prefix} identifies {meaning}. Select a rule to see what The Standard requires and how to correct a violation.",
            True,
        )
        write_page(
            f"Documentation_CodeAnalysis_{prefix}.json",
            page(
                prefix_path,
                prefix,
                prefix_order,
                "Documentation",
                prefix_body,
                f"{prefix} diagnostic family: {meaning}.",
                f"{prefix}: {title}",
            ),
        )
        write_role(f"Documentation_CodeAnalysis_{prefix}-Guests.json", prefix_path, "Guests")

        for rule_order, code in enumerate(prefix_codes, 1):
            description = descriptions[code]
            bad, good = example_for(code, description)
            body = (
                CODE_STYLES
                + '<header class="docs-page-header"><h1>[page[title]]</h1>'
                f"<p>{html.escape(description)}</p></header>"
                '<div class="public-actions docs-rule-actions">'
                f'<a class="btn btn-primary" href="/Documentation/CodeAnalysis/{prefix}">'
                f'<span class="k-icon k-i-arrow-left"></span>Back to {prefix} rules</a>'
                f'<a class="btn btn-secondary" href="{html.escape(STANDARD_SOURCE_LINKS[prefix])}" '
                'target="_blank" rel="noreferrer">'
                '<span class="k-icon k-i-book"></span>Read The Standard source</a>'
                "</div>"
                "<h2>What The Standard says</h2>"
                f"<p>{html.escape(description)} The analyser reports this as a warning at the declaration or statement that needs attention.</p>"
                "<h2>Why it matters</h2>"
                f"<p>This rule belongs to the {html.escape(PREFIXES[prefix][0])} family. "
                "Following it keeps responsibilities and intent visible to readers, tooling and future maintainers.</p>"
                '<h2>Bad code example</h2><pre><code class="language-csharp">'
                f"{highlight_csharp(bad)}</code></pre>"
                '<h2>Good code example</h2><pre><code class="language-csharp">'
                f"{highlight_csharp(good)}</code></pre>"
            )
            rule_path = f"{prefix_path}/{code}"
            write_page(
                f"Documentation_CodeAnalysis_{prefix}_{code}.json",
                page(
                    rule_path,
                    code,
                    rule_order,
                    "Documentation",
                    body,
                    description,
                ),
            )
            write_role(
                f"Documentation_CodeAnalysis_{prefix}_{code}-Guests.json",
                rule_path,
                "Guests",
            )

    print(f"Generated 1 platform page, 1 documentation root, {len(grouped)} prefix pages and {len(codes)} rule pages.")


if __name__ == "__main__":
    main()
