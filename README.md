[![Logo](https://raw.githubusercontent.com/NeatooDotNet/Neatoo/main/Logo_411.png 'Logo')](http://neatoo.net)

Neatoo is a DDD Aggregate Framework for .NET C# to create bindable and serializable Aggregate Entity Graphs for Blazor and WPF.
With Neatoo you define the business logic once and reuse it in both the UI and the Application Service. 
Allowing you to do more with less code while maintaining consistency. 

Neatoo is for applications with complex business logic. 
A robust user experience is delivered by the bindable properties, property messages and authorization check methods.
The user knows exactly what they can do, what they need to do to save and that the save will succeed!

**Write complex Aggregate Entity Graphs with no DTOs and only one controller!**

# Why is Neatoo New?

Neatoo uses established DDD principles and implements them with two game changing technologies: Blazor WebAssembly and Roslyn Live Source Generation.

## Blazor and WebAssembly

WebAssembly means having our .NET libraries with their defined Entities and their behavior *in the browser*.
This was not possible with a Javascript SPA application with a C# backend.
Now there's no longer the need to define individual endpoints for each behavior.
The client already has the Entities with their behavior.
All that needs to be transferred is the Entity data.

## Roslyn Source Generators

Neatoo uniquely uses Roslyn Source Generators to create a readable and performant Factory *specific* to each Entity. 
The 3-Tier Factories transfers the Entity Data for you. 
Authorization is implemented within the Factory.
Before Source Generators frameworks had to rely on reflection.
The end result wasn't known until execution.
Now many runtime errors are compile errors and there is code you can see and step thru!

# Features

* Validation
* Authorization
* Bindable Properties
* Bindable Meta-Properties
* 3-Tier Factory
* Mapper Methods
* Interface Serialization
* Dependency Injection
* Async/Await
* Live Source Generation

[Discord](https://discord.gg/M3dVuZkG)
[Nuget](https://www.nuget.org/packages/Neatoo)

## Neatoo Lifecycle

This is a common lifecycle for a Neatoo Aggregate Entity Graph to and from the database using a 3-Tier infrastructure.

![Lifecycle](https://raw.githubusercontent.com/NeatooDotNet/Neatoo/main/AggregateLifecycle_960.gif)


### Full documentation at [Neatoo.net](http://neatoo.net)